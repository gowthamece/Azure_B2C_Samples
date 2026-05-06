using B2C_user_management.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace B2C_user_management.Services;

/// <summary>
/// Service for calling the Azure Function to migrate users
/// </summary>
public interface IUserMigrationService
{
    /// <summary>
    /// Migrates users to Azure AD B2C via the Azure Function
    /// </summary>
    Task<BatchUserMigrationResponse> MigrateUsersAsync(List<UserMigrationRecord> users, bool forceChangePassword);
    
    /// <summary>
    /// Revokes/deletes migrated users from Azure AD B2C
    /// </summary>
    Task<RevokeUsersResponse> RevokeUsersAsync(List<string> userObjectIds);
}

public class UserMigrationService : IUserMigrationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<UserMigrationService> _logger;
    private readonly string _functionUrl;
    private readonly string _functionKey;

    public UserMigrationService(HttpClient httpClient, IConfiguration configuration, ILogger<UserMigrationService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _functionUrl = configuration["AzureFunction:BaseUrl"] ?? "http://localhost:7071";
        _functionKey = configuration["AzureFunction:FunctionKey"] ?? "";
    }

    public async Task<BatchUserMigrationResponse> MigrateUsersAsync(List<UserMigrationRecord> users, bool forceChangePassword)
    {
        try
        {
            var request = new BatchUserMigrationRequest
            {
                Users = users,
                ForceChangePasswordNextSignIn = forceChangePassword
            };

            var url = $"{_functionUrl}/api/migrate-users";
            
            // Add function key if provided
            if (!string.IsNullOrEmpty(_functionKey))
            {
                url += $"?code={_functionKey}";
            }

            _logger.LogInformation("Calling migration function at {Url} with {Count} users", url, users.Count);

            var response = await _httpClient.PostAsJsonAsync(url, request);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Migration function returned error: {StatusCode} - {Content}", response.StatusCode, errorContent);
                
                return new BatchUserMigrationResponse
                {
                    TotalUsers = users.Count,
                    FailureCount = users.Count,
                    Results = users.Select(u => new UserMigrationResult
                    {
                        Email = u.Email,
                        Success = false,
                        ErrorMessage = $"API Error: {response.StatusCode} - {errorContent}"
                    }).ToList()
                };
            }

            var result = await response.Content.ReadFromJsonAsync<BatchUserMigrationResponse>(new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return result ?? new BatchUserMigrationResponse
            {
                TotalUsers = users.Count,
                FailureCount = users.Count,
                Results = users.Select(u => new UserMigrationResult
                {
                    Email = u.Email,
                    Success = false,
                    ErrorMessage = "Empty response from migration function"
                }).ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling migration function");
            
            return new BatchUserMigrationResponse
            {
                TotalUsers = users.Count,
                FailureCount = users.Count,
                Results = users.Select(u => new UserMigrationResult
                {
                    Email = u.Email,
                    Success = false,
                    ErrorMessage = ex.Message
                }).ToList()
            };
        }
    }

    public async Task<RevokeUsersResponse> RevokeUsersAsync(List<string> userObjectIds)
    {
        try
        {
            var request = new RevokeUsersRequest
            {
                UserObjectIds = userObjectIds
            };

            var url = $"{_functionUrl}/api/revoke-users";
            
            // Add function key if provided
            if (!string.IsNullOrEmpty(_functionKey))
            {
                url += $"?code={_functionKey}";
            }

            _logger.LogInformation("Calling revoke function at {Url} with {Count} users", url, userObjectIds.Count);

            var response = await _httpClient.PostAsJsonAsync(url, request);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Revoke function returned error: {StatusCode} - {Content}", response.StatusCode, errorContent);
                
                return new RevokeUsersResponse
                {
                    TotalUsers = userObjectIds.Count,
                    FailureCount = userObjectIds.Count,
                    Results = userObjectIds.Select(id => new RevokeUserResult
                    {
                        B2CObjectId = id,
                        Success = false,
                        ErrorMessage = $"API Error: {response.StatusCode} - {errorContent}"
                    }).ToList()
                };
            }

            var result = await response.Content.ReadFromJsonAsync<RevokeUsersResponse>(new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return result ?? new RevokeUsersResponse
            {
                TotalUsers = userObjectIds.Count,
                FailureCount = userObjectIds.Count,
                Results = userObjectIds.Select(id => new RevokeUserResult
                {
                    B2CObjectId = id,
                    Success = false,
                    ErrorMessage = "Empty response from revoke function"
                }).ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling revoke function");
            
            return new RevokeUsersResponse
            {
                TotalUsers = userObjectIds.Count,
                FailureCount = userObjectIds.Count,
                Results = userObjectIds.Select(id => new RevokeUserResult
                {
                    B2CObjectId = id,
                    Success = false,
                    ErrorMessage = ex.Message
                }).ToList()
            };
        }
    }
}
