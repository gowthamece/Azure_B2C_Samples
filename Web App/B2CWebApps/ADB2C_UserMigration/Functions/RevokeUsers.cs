using ADB2C_UserMigration.Models;
using Azure.Identity;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using System.Net;
using System.Text.Json;

namespace ADB2C_UserMigration.Functions;

public class RevokeUsers
{
    private readonly ILogger<RevokeUsers> _logger;
    private readonly GraphServiceClient _graphClient;
    private readonly AzureB2CSettings _settings;

    public RevokeUsers(ILogger<RevokeUsers> logger)
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

    [Function("RevokeUsers")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "revoke-users")] HttpRequestData req)
    {
        _logger.LogInformation("User revoke/delete function triggered");

        try
        {
            // Parse the request body
            var requestBody = await req.ReadAsStringAsync();
            
            if (string.IsNullOrEmpty(requestBody))
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Request body is empty");
            }

            var revokeRequest = JsonSerializer.Deserialize<RevokeUsersRequest>(requestBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (revokeRequest == null || revokeRequest.UserObjectIds == null || !revokeRequest.UserObjectIds.Any())
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "No user Object IDs provided in the request");
            }

            _logger.LogInformation("Processing revoke for {Count} users", revokeRequest.UserObjectIds.Count);

            var response = new RevokeUsersResponse
            {
                TotalUsers = revokeRequest.UserObjectIds.Count,
                Results = new List<RevokeUserResult>()
            };

            // Process each user deletion
            foreach (var objectId in revokeRequest.UserObjectIds)
            {
                var result = await DeleteUserFromB2CAsync(objectId);
                response.Results.Add(result);

                if (result.Success)
                    response.SuccessCount++;
                else
                    response.FailureCount++;
            }

            _logger.LogInformation("Revoke completed. Success: {Success}, Failed: {Failed}", 
                response.SuccessCount, response.FailureCount);

            var httpResponse = req.CreateResponse(HttpStatusCode.OK);
            await httpResponse.WriteAsJsonAsync(response);
            return httpResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing user revoke request");
            return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, $"Error: {ex.Message}");
        }
    }

    private async Task<RevokeUserResult> DeleteUserFromB2CAsync(string objectId)
    {
        var result = new RevokeUserResult
        {
            B2CObjectId = objectId
        };

        try
        {
            if (string.IsNullOrWhiteSpace(objectId))
            {
                result.Success = false;
                result.ErrorMessage = "Object ID is empty";
                return result;
            }

            // Delete user from B2C
            await _graphClient.Users[objectId].DeleteAsync();

            result.Success = true;
            _logger.LogInformation("Successfully deleted user with Object ID: {ObjectId}", objectId);
        }
        catch (Microsoft.Graph.Models.ODataErrors.ODataError ex)
        {
            result.Success = false;
            result.ErrorMessage = $"Graph API Error: {ex.Error?.Message ?? ex.Message}";
            _logger.LogError(ex, "Graph API error deleting user {ObjectId}", objectId);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            _logger.LogError(ex, "Error deleting user {ObjectId}", objectId);
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
