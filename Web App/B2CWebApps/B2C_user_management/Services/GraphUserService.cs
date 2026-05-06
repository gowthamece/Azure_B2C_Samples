using Azure.Identity;
using B2C_user_management.Models;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using System.Text.Json;

namespace B2C_user_management.Services;

/// <summary>
/// Service for interacting with Azure AD B2C via Microsoft Graph API
/// </summary>
public interface IGraphUserService
{
    /// <summary>
    /// Checks if users exist in Azure AD B2C using batch requests
    /// </summary>
    /// <param name="userRecords">List of user records to check</param>
    /// <returns>List of user check results</returns>
    Task<List<UserCheckResult>> CheckUsersExistAsync(List<UserRecord> userRecords);
}

public class GraphUserService : IGraphUserService
{
    private readonly GraphServiceClient _graphClient;
    private readonly AzureB2CSettings _settings;
    private readonly ILogger<GraphUserService> _logger;
    private const int BatchSize = 20; // Microsoft Graph batch limit

    public GraphUserService(IOptions<AzureB2CSettings> settings, ILogger<GraphUserService> logger)
    {
        _settings = settings.Value;
        _logger = logger;

        var clientSecretCredential = new ClientSecretCredential(
            _settings.TenantId,
            _settings.ClientId,
            _settings.ClientSecret);

        _graphClient = new GraphServiceClient(clientSecretCredential, new[] { "https://graph.microsoft.com/.default" });
    }

    public async Task<List<UserCheckResult>> CheckUsersExistAsync(List<UserRecord> userRecords)
    {
        var results = new List<UserCheckResult>();
        
        // First, fetch all users from B2C to build a lookup dictionary
        // This is more efficient for bulk checks than individual queries
        _logger.LogInformation("Fetching all users from Azure AD B2C for comparison...");
        
        var allB2CUsers = await GetAllB2CUsersAsync();
        
        _logger.LogInformation("Fetched {Count} users from Azure AD B2C", allB2CUsers.Count);

        // Build lookup dictionaries for fast case-insensitive email matching
        var identityEmailLookup = new Dictionary<string, User>(StringComparer.OrdinalIgnoreCase);
        var mailLookup = new Dictionary<string, User>(StringComparer.OrdinalIgnoreCase);

        foreach (var user in allB2CUsers)
        {
            // Check identities for local account emails
            if (user.Identities != null)
            {
                foreach (var identity in user.Identities)
                {
                    if (!string.IsNullOrEmpty(identity.IssuerAssignedId) && 
                        identity.Issuer == _settings.B2CDomain &&
                        (identity.SignInType == "emailAddress" || identity.SignInType == "userName"))
                    {
                        identityEmailLookup.TryAdd(identity.IssuerAssignedId, user);
                    }
                }
            }

            // Also check mail property
            if (!string.IsNullOrEmpty(user.Mail))
            {
                mailLookup.TryAdd(user.Mail, user);
            }

            // Check otherMails
            if (user.OtherMails != null)
            {
                foreach (var otherMail in user.OtherMails)
                {
                    if (!string.IsNullOrEmpty(otherMail))
                    {
                        mailLookup.TryAdd(otherMail, user);
                    }
                }
            }
        }

        // Now check each user record against the lookup
        foreach (var userRecord in userRecords)
        {
            var result = new UserCheckResult
            {
                Email = userRecord.Email,
                DisplayName = userRecord.DisplayName,
                FirstName = userRecord.FirstName,
                LastName = userRecord.LastName,
                IsExist = "N"
            };

            // Check in identity lookup first (for B2C local accounts)
            if (identityEmailLookup.TryGetValue(userRecord.Email, out var foundUserByIdentity))
            {
                result.IsExist = "Y";
                result.B2CObjectId = foundUserByIdentity.Id;
                result.DisplayName = foundUserByIdentity.DisplayName ?? userRecord.DisplayName;
            }
            // Then check mail lookup
            else if (mailLookup.TryGetValue(userRecord.Email, out var foundUserByMail))
            {
                result.IsExist = "Y";
                result.B2CObjectId = foundUserByMail.Id;
                result.DisplayName = foundUserByMail.DisplayName ?? userRecord.DisplayName;
            }

            results.Add(result);
        }

        return results;
    }

    private async Task<List<User>> GetAllB2CUsersAsync()
    {
        var allUsers = new List<User>();

        try
        {
            var response = await _graphClient.Users.GetAsync(config =>
            {
                config.QueryParameters.Select = new[] { "id", "displayName", "givenName", "surname", "mail", "otherMails", "identities" };
                config.QueryParameters.Top = 999;
            });

            if (response?.Value != null)
            {
                allUsers.AddRange(response.Value);
            }

            // Handle pagination
            var pageIterator = Microsoft.Graph.PageIterator<User, UserCollectionResponse>.CreatePageIterator(
                _graphClient,
                response,
                (user) =>
                {
                    allUsers.Add(user);
                    return true;
                });

            await pageIterator.IterateAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching users from Azure AD B2C");
            throw;
        }

        return allUsers;
    }

    private static string EscapeODataString(string value)
    {
        // Escape single quotes for OData filter
        return value?.Replace("'", "''") ?? string.Empty;
    }
}
