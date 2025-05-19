using Azure.Core;
using Azure.Identity;
using B2C_AppRoles.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using Application = B2C_AppRoles.Models.Application;
using AppRole = B2C_AppRoles.Models.AppRole;

namespace B2C_AppRoles.MSGraphServices
{
    public class MSGraphApiServices : IMSGraphApiServices
    {
        private static readonly string GraphApiUrl = "https://graph.microsoft.com/v1.0/";
        private static readonly string Scope = "https://graph.microsoft.com/.default";
        private readonly AzureAdOptions _azureAdOptions;
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly string _tenantId;
        private readonly IConfiguration _configuration;
        public MSGraphApiServices(IOptions<AzureAdOptions> azureAdOptions, IConfiguration configuration)
        {
            _configuration = configuration;
            _azureAdOptions = azureAdOptions.Value;
            _clientId = _azureAdOptions.ClientId;
            _clientSecret = _azureAdOptions.ClientSecret;
            _tenantId = _azureAdOptions.TenantId;
           
        }
        public  async Task<IActionResult> GetApplicationOwnerAuthorizationAsync(string email, string appId)
        {
            try
            {
                var graphClient = GetGraphClientAsync();
                var owners = await graphClient.Applications[appId].Owners.GetAsync();

                if (owners != null && owners.Value.Any(owner => owner.Id.Equals(email, StringComparison.OrdinalIgnoreCase)))
                {
                    return new OkObjectResult("Authorized");
                }
                else
                {
                    return new UnauthorizedResult();
                }
            }
            catch (Exception ex)
            {
                // Log the exception as needed
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }

        public async Task<List<Users>> GetUsersAsync()
        {
            var graphClient = GetGraphClientAsync();
            var users = await graphClient.Users.GetAsync();

            var userList = new List<Users>();
            foreach (var userObj in users.Value.ToList())
            {
                var result = await graphClient.Users[userObj.Id].GetAsync((requestConfiguration) =>
                {
                    requestConfiguration.QueryParameters.Select = new string[] { "identities" };
                });
                var email = result.Identities.Where(e => e.SignInType == "emailAddress").Count() > 0 ? result.Identities.Where(e => e.SignInType == "emailAddress").FirstOrDefault().IssuerAssignedId : string.Empty;
                userList.Add(new Users { Id = userObj.Id, DispalyName = userObj.DisplayName, Email = email.ToString(), Type = "User" });
            }
            return userList;
        }

        public async Task<List<Groups>> GetGroupsAsync()
        {
            var graphClient = GetGraphClientAsync();
            var groups = await graphClient.Groups.GetAsync();

            var groupList = new List<Groups>();
            foreach (var userObj in groups.Value.ToList())
            {
                groupList.Add(new Groups { Id = userObj.Id, DispalyName = userObj.DisplayName, Email = string.Empty, Type = "Group" });
            }
            return groupList;
        }
        public async  Task<List<Application>> GetApplicationsAsync()
        {
            var graphClient = GetGraphClientAsync();
            var applications = await graphClient.Applications.GetAsync();
            var applicationList = new List<Application>();
            foreach (var app in applications.Value.ToList())
            {
                applicationList.Add(new Application { Id = app.Id, Name = app.DisplayName, appId=app.AppId });
            }
            return applicationList;
        }
        public async  Task<List<AppRole>> GetAppRolesAsync(string appId)


        {

            var appRole = new List<AppRole>();
            var graphClient = GetGraphClientAsync();
            var application = await graphClient.Applications
                .GetAsync(requestConfiguration =>
                {
                    requestConfiguration.QueryParameters.Filter = $"Id eq '{appId}'";
                    requestConfiguration.QueryParameters.Select = new[] { "appRoles","appId" };
                });
            var appRoles = application?.Value?.FirstOrDefault()?.AppRoles;
            var cleintId = application?.Value?.FirstOrDefault()?.AppId;
            if (appRoles != null)
            {
                foreach (var role in appRoles)
                {
                    appRole.Add(new AppRole { Id = role.Id.ToString(), Name = role.DisplayName, AppId = cleintId });
                }
            }
            return appRole;

        }
        public async  Task AssignUserToAppRole(string principalId, string resourceId, string appRoleId, string memberType = "user")
        {
            try
            {
                var graphClient = GetGraphClientAsync();
                var servicePrincipals = await graphClient.ServicePrincipals
                     .GetAsync(requestConfiguration =>
                     {
                         requestConfiguration.QueryParameters.Filter = $"appId eq '{resourceId}'";
                     });
                var servicePrincipal = servicePrincipals?.Value?.FirstOrDefault();
                var requestBody = new AppRoleAssignment
                {
                    PrincipalId = Guid.Parse(principalId), // user or group Id
                    ResourceId = Guid.Parse(servicePrincipal.Id), // service principal objectId
                    AppRoleId = Guid.Parse(appRoleId)
                };


                if (memberType.ToLower() == "user")
                {
                    var result = await graphClient.Users[principalId].AppRoleAssignments.PostAsync(requestBody);
                }
                else
                {
                    var result = await graphClient.Groups[principalId].AppRoleAssignments.PostAsync(requestBody);
                }
            }
            catch (Exception ex)
            {

            }
        }


        public async  Task RevokeMemberFromAppRole(string principalId, string resourceId, string appRoleId, string memberType = "user")
        {
            try
            {
                var graphClient = GetGraphClientAsync();
                var servicePrincipals = await graphClient.ServicePrincipals
                     .GetAsync(requestConfiguration =>
                     {
                         requestConfiguration.QueryParameters.Filter = $"appId eq '{resourceId}'";
                     });
                var servicePrincipal = servicePrincipals?.Value?.FirstOrDefault();

                if (memberType.ToLower() == "user")
                {
                    var appRoleAssignments = await graphClient.Users[principalId].AppRoleAssignments.GetAsync();
                    var assignmentToRevoke = appRoleAssignments.Value.FirstOrDefault(a =>
                        a.ResourceId == Guid.Parse(servicePrincipal.Id) &&
                        a.AppRoleId == Guid.Parse(appRoleId));

                    if (assignmentToRevoke != null)
                    {
                        await graphClient.Users[principalId].AppRoleAssignments[assignmentToRevoke.Id].DeleteAsync();
                    }
                }
                else
                {
                    var appRoleAssignments = await graphClient.Groups[principalId].AppRoleAssignments.GetAsync();
                    var assignmentToRevoke = appRoleAssignments.Value.FirstOrDefault(a =>
                        a.ResourceId == Guid.Parse(servicePrincipal.Id) &&
                        a.AppRoleId == Guid.Parse(appRoleId));

                    if (assignmentToRevoke != null)
                    {
                        await graphClient.Groups[principalId].AppRoleAssignments[assignmentToRevoke.Id].DeleteAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle exception (log or rethrow as needed)
            }
        }
        public async  Task<List<Users>> GetUserByAppRoleId(string roleId, string appId)
        {
            try
            {
                var graphClient = GetGraphClientAsync();
                 var servicePrincipalsList = await graphClient.ServicePrincipals.GetAsync();

                var servicePrincipals = await graphClient.ServicePrincipals
                    .GetAsync(requestConfiguration =>
                    {
                        requestConfiguration.QueryParameters.Filter = $"appId eq '{appId}'";
                    });

                var servicePrincipal = servicePrincipals?.Value?.FirstOrDefault();
                var roleAssignment = await graphClient.ServicePrincipals[servicePrincipal.Id]
                        .AppRoleAssignedTo.GetAsync();
                var appRoleId = Guid.Parse(roleId);
                var filter = $"appRoleId eq '{appRoleId}'";
                var assignedUsers = await graphClient.ServicePrincipals[servicePrincipal.Id]
                        .AppRoleAssignedTo.GetAsync();
                var assignedUsersResult = assignedUsers.Value.Where(e => e.AppRoleId.ToString() == roleId);
                var userList = new List<Users>();
                foreach (var userObj in assignedUsersResult)
                {
                    if (userObj.PrincipalType.ToLower() == "user")
                    {
                        var userId = userObj.PrincipalId.ToString();
                        var result = await graphClient.Users[userId].GetAsync((requestConfiguration) =>
                        {
                            requestConfiguration.QueryParameters.Select = new string[] { "identities" };
                        });
                        var email = result.Identities.Where(e => e.SignInType == "emailAddress").Count() > 0 ? result.Identities.Where(e => e.SignInType == "emailAddress").FirstOrDefault().IssuerAssignedId : string.Empty;

                        userList.Add(new Users { Id = userObj.PrincipalId.ToString(), DispalyName = userObj.PrincipalDisplayName, Email = email, Type = userObj.PrincipalType });

                    }
                    else
                    {
                        var groupId = userObj.PrincipalId.ToString();
                        var result = await graphClient.Groups[groupId].GetAsync();
                        userList.Add(new Users { Id = userObj.PrincipalId.ToString(), DispalyName = userObj.PrincipalDisplayName, Email = string.Empty, Type = userObj.PrincipalType });

                    }

                }
                return userList;
            }
            catch (Exception ex)
            {
            }
            return await GetUsersAsync();

        }
        public  GraphServiceClient GetGraphClientAsync()
        {
            var scopes = new[] { Scope };
            var appRole = new List<AppRole>();
            var options = new ClientSecretCredentialOptions
            {
                AuthorityHost = AzureAuthorityHosts.AzurePublicCloud,
            };

            var clientSecretCredential = new ClientSecretCredential(
                        _configuration["EntraId:TenantId"] , _configuration["EntraId:ClientId"], _configuration["EntraId:ClientSecret"] , options);
            var graphClient = new GraphServiceClient(clientSecretCredential, scopes);
            return graphClient;
        }

        public  string GetCachedAccessToken(string tenantId, int secondsRemaining = 60) 
        {
            string accessToken = Environment.GetEnvironmentVariable($"B2C_{tenantId}_AppRoles_AccessToken");
            if (accessToken != null)
            {
                DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                string b64 = accessToken.Split(".")[1];
                while (b64.Length % 4 != 0)
                    b64 += "=";
                JObject jwtClaims = JObject.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(b64)));
                DateTime expiryTime = epoch.AddSeconds(int.Parse(jwtClaims["exp"].ToString()));
                if (DateTime.UtcNow >= expiryTime.AddSeconds(-secondsRemaining))
                    accessToken = null; // invalidate
            }
            return accessToken;
        }
        public  void CacheAccessToken(string tenantId, string accesToken)
        {
            Environment.SetEnvironmentVariable($"B2C_{tenantId}_AppRoles_AccessToken", accesToken);
        }

        public async  Task<List<Users>> GetMembersByTypeAndFilterAsync(string memberType, string searchFeildValue)
        {
            var membersList = new List<Users>();
            try
            {
                var graphClient = GetGraphClientAsync();
                var issuer = _configuration["EntraId:Issuer"];
                if (memberType.ToLower() == "user")
                {
                    var users = await graphClient.Users
                        .GetAsync(requestConfiguration =>
                        {
                            //requestConfiguration.QueryParameters.Filter = $"startswith(givenName, '{firstNameStartsWith}')";
                            //requestConfiguration.QueryParameters.Count = true;
                            //requestConfiguration.Headers.Add("Consistency-Level", "eventual");
                            // requestConfiguration.QueryParameters.Filter = $"identities/any(i: i/issuer eq '{issuer}')";
                            // requestConfiguration.Headers.Add("Consistency-Level", "eventual");
                            requestConfiguration.QueryParameters.Filter = $"identities/any(c:c/issuerAssignedId eq '{searchFeildValue}' and c/issuer eq '{issuer}')";
                            requestConfiguration.QueryParameters.Select = new[] { "id", "displayName", "identities" };
                        });



                    foreach (var user in users.Value)
                    {
                        var emailFromIdentities = user.Identities?
                        .FirstOrDefault(id => id.SignInType == "emailAddress")?.IssuerAssignedId;
                        membersList.Add(new Users
                        {
                            Id = user.Id,
                            DispalyName = user.DisplayName + " - " + emailFromIdentities,
                            Email = emailFromIdentities,
                            Type = "User"
                        });
                    }
                }
                else if (memberType.ToLower() == "group")
                {
                    var groups = await graphClient.Groups
                        .GetAsync(requestConfiguration =>
                        {
                            requestConfiguration.QueryParameters.Filter = $"startswith(displayName, '{searchFeildValue}')";
                            requestConfiguration.QueryParameters.Select = new[] { "id", "displayName", "mail" };
                        });

                    foreach (var group in groups.Value)
                    {
                        membersList.Add(new Users
                        {
                            Id = group.Id,
                            DispalyName = group.DisplayName,
                            Email = group.Mail,
                            Type = "Group"
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle exception (log or rethrow as needed)  
            }

            return membersList;
        }

        public async  Task<bool> CreateAppRoleAsync(string appId, AppRoleCreationRequest appRoleRequest)
        {
            try
            {
                var graphClient = GetGraphClientAsync();

                // Retrieve the application to update its appRoles
                var application = await graphClient.Applications[appId]
                    .GetAsync(requestConfiguration =>
                    {
                        requestConfiguration.QueryParameters.Select = new[] { "appRoles" };
                    });

                if (application == null)
                {
                    return false; // Application not found
                }

                // Create a new app role
                var newAppRole = new Microsoft.Graph.Models.AppRole
                {
                    Id = Guid.NewGuid(),
                    DisplayName = appRoleRequest.RoleName,
                    Description = appRoleRequest.RoleDescription,
                    IsEnabled = appRoleRequest.IsEnabled,
                    Value = appRoleRequest.RoleName,
                    AllowedMemberTypes = new List<string> { "User" } // Default to "User", can be extended to "Group" if needed
                };

                // Add the new app role to the existing list
                var updatedAppRoles = application.AppRoles ?? new List<Microsoft.Graph.Models.AppRole>();
                updatedAppRoles.Add(newAppRole);

                // Update the application with the new app role
                var updatedApplication = new Microsoft.Graph.Models.Application
                {
                    AppRoles = updatedAppRoles
                };

                await graphClient.Applications[appId]
                    .PatchAsync(updatedApplication);

                return true; // Successfully created the app role
            }
            catch (Exception ex)
            {
                // Handle exception (log or rethrow as needed)
                return false;
            }
        }
        public async  Task<List<Groups>> GetGroupsOwnedByUserAsync(string userId)
        {
            var ownedGroups = new List<Groups>();
            try
            {
                var graphClient = GetGraphClientAsync();
  

            
                // Get the groups where the user is an owner
                var groups = await graphClient.Users[userId].OwnedObjects
                    .GetAsync(requestConfiguration =>
                    {
                        requestConfiguration.QueryParameters.Select = new[] { "id", "displayName", "mail" };
                    });

                if (groups?.Value != null)
                {
                    foreach (var directoryObject in groups.Value)
                    {
                        // Only include objects of type "group"
                        if (directoryObject is Group group)
                        {
                            ownedGroups.Add(new Groups
                            {
                                Id = group.Id,
                                DispalyName = group.DisplayName,
                                Email = group.Mail,
                                Type = "Group"
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle exception (log or rethrow as needed)
            }
            return ownedGroups;
        }
        public async Task<bool> AddUsersToGroupAsync(List<string> userIds, string groupId)
        {
            try
            {
                var graphClient = GetGraphClientAsync();

                var memberOdataBind = userIds
                    .Select(id => $"{GraphApiUrl}/directoryObjects/{id}")
                    .ToList();
                                    var group = new Group
                                    {
                                        AdditionalData = new Dictionary<string, object>
                        {
                            { "members@odata.bind", memberOdataBind }
                        }
                                    };

                await graphClient.Groups[groupId].PatchAsync(group);
     
                return true;
            }
            catch(Exception ex)
            {
                // Handle exception (log or rethrow as needed)
                return false;
            }
        }
        public async  Task<List<Users>> GetUsersByGroupIdAsync(string groupId)
        {
            var usersList = new List<Users>();
            try
            {
                var graphClient = GetGraphClientAsync();
                // Get group members
                var members = await graphClient.Groups[groupId].Members
                    .GetAsync(requestConfiguration =>
                    {
                        requestConfiguration.QueryParameters.Select = new[] { "id", "displayName", "userPrincipalName", "identities" };
                    });

                if (members?.Value != null)
                {
                    foreach (var member in members.Value)
                    {
                        // Only process user objects
                        if (member is User user)
                        {
                            string email = user.UserPrincipalName;
                            // Try to get email from identities if available
                            if (user.Identities != null)
                            {
                                var emailIdentity = user.Identities.FirstOrDefault(i => i.SignInType == "emailAddress");
                                if (emailIdentity != null)
                                {
                                    email = emailIdentity.IssuerAssignedId;
                                }
                            }
                            usersList.Add(new Users
                            {
                                Id = user.Id,
                                DispalyName = user.DisplayName,
                                Email = email,
                                Type = "User"
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle exception (log or rethrow as needed)
            }
            return usersList;
        }
        public async  Task<bool> RemoveUserFromGroupAsync(string userId, string groupId)
        {
            try
            {
                var graphClient = GetGraphClientAsync();
                // Remove the user from the group
                await graphClient.Groups[groupId].Members[userId].Ref.DeleteAsync();
                return true;
            }
            catch (Exception ex)
            {
                // Handle exception (log or rethrow as needed)
                return false;
            }
        }
    }
}
