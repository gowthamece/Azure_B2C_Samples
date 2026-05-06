using ADB2C_UserMigration.Models;
using Azure.Identity;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using System.Net;
using System.Text.Json;

namespace ADB2C_UserMigration.Functions;

public class MigrateUsers
{
    private readonly ILogger<MigrateUsers> _logger;
    private readonly GraphServiceClient _graphClient;
    private readonly AzureB2CSettings _settings;

    public MigrateUsers(ILogger<MigrateUsers> logger)
    {
        _logger = logger;
        
        // Load settings from environment variables
        _settings = new AzureB2CSettings
        {
            TenantId = Environment.GetEnvironmentVariable("AzureB2C_TenantId") ?? "",
            ClientId = Environment.GetEnvironmentVariable("AzureB2C_ClientId") ?? "",
            ClientSecret = Environment.GetEnvironmentVariable("AzureB2C_ClientSecret") ?? "",
            TenantName = Environment.GetEnvironmentVariable("AzureB2C_TenantName") ?? ""
        };

        var clientSecretCredential = new ClientSecretCredential(
            _settings.TenantId,
            _settings.ClientId,
            _settings.ClientSecret);

        _graphClient = new GraphServiceClient(clientSecretCredential, new[] { "https://graph.microsoft.com/.default" });
    }

    [Function("MigrateUsers")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "migrate-users")] HttpRequestData req)
    {
        _logger.LogInformation("User migration function triggered");

        try
        {
            // Parse the request body
            var requestBody = await req.ReadAsStringAsync();
            
            if (string.IsNullOrEmpty(requestBody))
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Request body is empty");
            }

            var migrationRequest = JsonSerializer.Deserialize<BatchUserMigrationRequest>(requestBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (migrationRequest == null || migrationRequest.Users == null || !migrationRequest.Users.Any())
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "No users provided in the request");
            }

            _logger.LogInformation("Processing migration for {Count} users", migrationRequest.Users.Count);

            var response = new BatchUserMigrationResponse
            {
                TotalUsers = migrationRequest.Users.Count,
                Results = new List<UserMigrationResult>()
            };

            // Process each user
            foreach (var userRequest in migrationRequest.Users)
            {
                var result = await CreateUserInB2CAsync(userRequest, migrationRequest.ForceChangePasswordNextSignIn);
                response.Results.Add(result);

                if (result.Success)
                    response.SuccessCount++;
                else
                    response.FailureCount++;
            }

            _logger.LogInformation("Migration completed. Success: {Success}, Failed: {Failed}", 
                response.SuccessCount, response.FailureCount);

            var httpResponse = req.CreateResponse(HttpStatusCode.OK);
            await httpResponse.WriteAsJsonAsync(response);
            return httpResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing user migration request");
            return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, $"Error: {ex.Message}");
        }
    }

    private async Task<UserMigrationResult> CreateUserInB2CAsync(UserMigrationRequest userRequest, bool forceChangePassword)
    {
        // Build the display name early for use in result
        var displayName = userRequest.DisplayName;
        if (string.IsNullOrWhiteSpace(displayName))
        {
            displayName = $"{userRequest.FirstName} {userRequest.LastName}".Trim();
            if (string.IsNullOrWhiteSpace(displayName))
            {
                displayName = userRequest.Email;
            }
        }

        var result = new UserMigrationResult
        {
            Email = userRequest.Email,
            FirstName = userRequest.FirstName,
            LastName = userRequest.LastName,
            DisplayName = displayName
        };

        try
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(userRequest.Email))
            {
                result.Success = false;
                result.ErrorMessage = "Email is required";
                return result;
            }

            if (string.IsNullOrWhiteSpace(userRequest.Password))
            {
                result.Success = false;
                result.ErrorMessage = "Password is required";
                return result;
            }

            // Create user object with B2C local account identity
            var user = new User
            {
                DisplayName = displayName,
                GivenName = userRequest.FirstName,
                Surname = userRequest.LastName,
                Identities = new List<ObjectIdentity>
                {
                    new ObjectIdentity
                    {
                        SignInType = "emailAddress",
                        Issuer = _settings.B2CDomain,
                        IssuerAssignedId = userRequest.Email
                    }
                },
                PasswordProfile = new PasswordProfile
                {
                    Password = userRequest.Password,
                    ForceChangePasswordNextSignIn = forceChangePassword
                },
                PasswordPolicies = "DisablePasswordExpiration"
            };

            // Create user in B2C
            var createdUser = await _graphClient.Users.PostAsync(user);

            if (createdUser != null)
            {
                result.Success = true;
                result.B2CObjectId = createdUser.Id;
                _logger.LogInformation("Successfully created user: {Email} with Object ID: {ObjectId}", 
                    userRequest.Email, createdUser.Id);
            }
            else
            {
                result.Success = false;
                result.ErrorMessage = "User creation returned null";
            }
        }
        catch (Microsoft.Graph.Models.ODataErrors.ODataError ex)
        {
            result.Success = false;
            result.ErrorMessage = $"Graph API Error: {ex.Error?.Message ?? ex.Message}";
            _logger.LogError(ex, "Graph API error creating user {Email}", userRequest.Email);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            _logger.LogError(ex, "Error creating user {Email}", userRequest.Email);
        }

        return result;
    }

    private async Task<HttpResponseData> CreateErrorResponse(HttpRequestData req, HttpStatusCode statusCode, string message)
    {
        var response = req.CreateResponse(statusCode);
        await response.WriteAsJsonAsync(new { error = message });
        return response;
    }
}
