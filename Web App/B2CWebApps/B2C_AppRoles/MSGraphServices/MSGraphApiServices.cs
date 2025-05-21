using Azure.Core;
using Azure.Identity;
using B2C_AppRoles.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Newtonsoft.Json.Linq;
using System.Net;
using Application = B2C_AppRoles.Models.Application;
using AppRole = B2C_AppRoles.Models.AppRole;

namespace B2C_AppRoles.MSGraphServices
{
    public class MSGraphApiServices : IMSGraphApiServices
    {
        private static readonly string GraphApiUrl = "https://graph.microsoft.com/v1.0/";
        private static readonly string Scope = "https://graph.microsoft.com/.default";        
        private readonly IConfiguration _configuration;

        public MSGraphApiServices(IOptions<AzureAdOptions> azureAdOptions, IConfiguration configuration)
        {
            _configuration = configuration;            
        }

        public async Task<IActionResult> GetApplicationOwnerAuthorizationAsync(string email, string appId)
        {
            try
            {
                var graphClient = GetGraphClientAsync();
                var owners = await graphClient.Applications[appId].Owners.GetAsync();

                if (owners?.Value?.Any(owner => owner.Id.Equals(email, StringComparison.OrdinalIgnoreCase)) == true)
                    return new OkObjectResult("Authorized");
                return new UnauthorizedResult();
            }
            catch
            {
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }

        public async Task<List<Users>> GetUsersAsync()
        {
            var graphClient = GetGraphClientAsync();
            var users = await graphClient.Users.GetAsync();
            var userList = new List<Users>(users.Value.Count);

            foreach (var userObj in users.Value)
            {
                var result = await graphClient.Users[userObj.Id].GetAsync(rc =>
                {
                    rc.QueryParameters.Select = new[] { "identities" };
                });
                var email = result.Identities?.FirstOrDefault(e => e.SignInType == "emailAddress")?.IssuerAssignedId ?? string.Empty;
                userList.Add(new Users
                {
                    Id = userObj.Id,
                    DispalyName = userObj.DisplayName,
                    Email = email,
                    Type = "User"
                });
            }
            return userList;
        }

        public async Task<List<Groups>> GetGroupsAsync()
        {
            var graphClient = GetGraphClientAsync();
            var groups = await graphClient.Groups.GetAsync();
            var groupList = new List<Groups>(groups.Value.Count);

            foreach (var groupObj in groups.Value)
            {
                groupList.Add(new Groups
                {
                    Id = groupObj.Id,
                    DispalyName = groupObj.DisplayName,
                    Email = string.Empty,
                    Type = "Group"
                });
            }
            return groupList;
        }

        public async Task<List<Application>> GetApplicationsAsync()
        {
            var graphClient = GetGraphClientAsync();
            var applications = await graphClient.Applications.GetAsync();
            var applicationList = new List<Application>(applications.Value.Count);

            foreach (var app in applications.Value)
            {
                applicationList.Add(new Application
                {
                    Id = app.Id,
                    Name = app.DisplayName,
                    appId = app.AppId
                });
            }
            return applicationList;
        }

        public async Task<List<AppRole>> GetAppRolesAsync(string appId)
        {
            var appRole = new List<AppRole>();
            var graphClient = GetGraphClientAsync();
            var application = await graphClient.Applications
                .GetAsync(rc =>
                {
                    rc.QueryParameters.Filter = $"Id eq '{appId}'";
                    rc.QueryParameters.Select = new[] { "appRoles", "appId" };
                });
            var appRoles = application?.Value?.FirstOrDefault()?.AppRoles;
            var clientId = application?.Value?.FirstOrDefault()?.AppId;
            if (appRoles != null)
            {
                foreach (var role in appRoles)
                {
                    appRole.Add(new AppRole
                    {
                        Id = role.Id.ToString(),
                        Name = role.DisplayName,
                        AppId = clientId
                    });
                }
            }
            return appRole;
        }

        public async Task AssignUserToAppRole(string principalId, string resourceId, string appRoleId, string memberType = "user")
        {
            try
            {
                var graphClient = GetGraphClientAsync();
                var servicePrincipals = await graphClient.ServicePrincipals
                    .GetAsync(rc =>
                    {
                        rc.QueryParameters.Filter = $"appId eq '{resourceId}'";
                    });
                var servicePrincipal = servicePrincipals?.Value?.FirstOrDefault();
                if (servicePrincipal == null) return;

                var requestBody = new AppRoleAssignment
                {
                    PrincipalId = Guid.Parse(principalId),
                    ResourceId = Guid.Parse(servicePrincipal.Id),
                    AppRoleId = Guid.Parse(appRoleId)
                };

                if (memberType.Equals("user", StringComparison.OrdinalIgnoreCase))
                    await graphClient.Users[principalId].AppRoleAssignments.PostAsync(requestBody);
                else
                    await graphClient.Groups[principalId].AppRoleAssignments.PostAsync(requestBody);
            }
            catch
            {
                // Log or handle as needed
            }
        }

        public async Task RevokeMemberFromAppRole(string principalId, string resourceId, string appRoleId, string memberType = "user")
        {
            try
            {
                var graphClient = GetGraphClientAsync();
                var servicePrincipals = await graphClient.ServicePrincipals
                    .GetAsync(rc =>
                    {
                        rc.QueryParameters.Filter = $"appId eq '{resourceId}'";
                    });
                var servicePrincipal = servicePrincipals?.Value?.FirstOrDefault();
                if (servicePrincipal == null) return;

                var assignments = memberType.Equals("user", StringComparison.OrdinalIgnoreCase)
                    ? await graphClient.Users[principalId].AppRoleAssignments.GetAsync()
                    : await graphClient.Groups[principalId].AppRoleAssignments.GetAsync();

                var assignmentToRevoke = assignments.Value.FirstOrDefault(a =>
                    a.ResourceId == Guid.Parse(servicePrincipal.Id) &&
                    a.AppRoleId == Guid.Parse(appRoleId));

                if (assignmentToRevoke != null)
                {
                    if (memberType.Equals("user", StringComparison.OrdinalIgnoreCase))
                        await graphClient.Users[principalId].AppRoleAssignments[assignmentToRevoke.Id].DeleteAsync();
                    else
                        await graphClient.Groups[principalId].AppRoleAssignments[assignmentToRevoke.Id].DeleteAsync();
                }
            }
            catch
            {
                // Log or handle as needed
            }
        }

        public async Task<List<Users>> GetUserByAppRoleId(string roleId, string appId)
        {
            try
            {
                var graphClient = GetGraphClientAsync();
                var servicePrincipals = await graphClient.ServicePrincipals
                    .GetAsync(rc =>
                    {
                        rc.QueryParameters.Filter = $"appId eq '{appId}'";
                    });
                var servicePrincipal = servicePrincipals?.Value?.FirstOrDefault();
                if (servicePrincipal == null) return new List<Users>();

                var assignedUsers = await graphClient.ServicePrincipals[servicePrincipal.Id]
                    .AppRoleAssignedTo.GetAsync();

                var assignedUsersResult = assignedUsers.Value
                    .Where(e => e.AppRoleId.ToString() == roleId);

                var userList = new List<Users>();
                foreach (var userObj in assignedUsersResult)
                {
                    if (userObj.PrincipalType.Equals("user", StringComparison.OrdinalIgnoreCase))
                    {
                        var userId = userObj.PrincipalId.ToString();
                        var result = await graphClient.Users[userId].GetAsync(rc =>
                        {
                            rc.QueryParameters.Select = new[] { "identities" };
                        });
                        var email = result.Identities?.FirstOrDefault(e => e.SignInType == "emailAddress")?.IssuerAssignedId ?? string.Empty;
                        userList.Add(new Users
                        {
                            Id = userObj.PrincipalId.ToString(),
                            DispalyName = userObj.PrincipalDisplayName,
                            Email = email,
                            Type = userObj.PrincipalType
                        });
                    }
                    else
                    {
                        userList.Add(new Users
                        {
                            Id = userObj.PrincipalId.ToString(),
                            DispalyName = userObj.PrincipalDisplayName,
                            Email = string.Empty,
                            Type = userObj.PrincipalType
                        });
                    }
                }
                return userList;
            }
            catch
            {
                return await GetUsersAsync();
            }
        }

        public GraphServiceClient GetGraphClientAsync()
        {
            var scopes = new[] { Scope };
            var options = new ClientSecretCredentialOptions
            {
                AuthorityHost = AzureAuthorityHosts.AzurePublicCloud,
            };

            var clientSecretCredential = new ClientSecretCredential(
                _configuration["EntraId:TenantId"],
                _configuration["EntraId:ClientId"],
                _configuration["EntraId:ClientSecret"],
                options);

            return new GraphServiceClient(clientSecretCredential, scopes);
        }

        public string GetCachedAccessToken(string tenantId, int secondsRemaining = 60)
        {
            string accessToken = Environment.GetEnvironmentVariable($"B2C_{tenantId}_AppRoles_AccessToken");
            if (accessToken != null)
            {
                DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                string b64 = accessToken.Split(".")[1];
                while (b64.Length % 4 != 0)
                    b64 += "=";
                JObject jwtClaims = JObject.Parse(System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(b64)));
                DateTime expiryTime = epoch.AddSeconds(int.Parse(jwtClaims["exp"].ToString()));
                if (DateTime.UtcNow >= expiryTime.AddSeconds(-secondsRemaining))
                    accessToken = null;
            }
            return accessToken;
        }

        public void CacheAccessToken(string tenantId, string accesToken)
        {
            Environment.SetEnvironmentVariable($"B2C_{tenantId}_AppRoles_AccessToken", accesToken);
        }

        public async Task<List<Users>> GetMembersByTypeAndFilterAsync(string memberType, string searchFieldValue)
        {
            var membersList = new List<Users>();
            try
            {
                var graphClient = GetGraphClientAsync();
                var issuer = _configuration["EntraId:Issuer"];
                if (memberType.Equals("user", StringComparison.OrdinalIgnoreCase))
                {
                    var users = await graphClient.Users
                        .GetAsync(rc =>
                        {
                            rc.QueryParameters.Filter = $"identities/any(c:c/issuerAssignedId eq '{searchFieldValue}' and c/issuer eq '{issuer}')";
                            rc.QueryParameters.Select = new[] { "id", "displayName", "identities" };
                        });

                    foreach (var user in users.Value)
                    {
                        var emailFromIdentities = user.Identities?.FirstOrDefault(id => id.SignInType == "emailAddress")?.IssuerAssignedId;
                        membersList.Add(new Users
                        {
                            Id = user.Id,
                            DispalyName = $"{user.DisplayName} - {emailFromIdentities}",
                            Email = emailFromIdentities,
                            Type = "User"
                        });
                    }
                }
                else if (memberType.Equals("group", StringComparison.OrdinalIgnoreCase))
                {
                    var groups = await graphClient.Groups
                        .GetAsync(rc =>
                        {
                            rc.QueryParameters.Filter = $"startswith(displayName, '{searchFieldValue}')";
                            rc.QueryParameters.Select = new[] { "id", "displayName", "mail" };
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
            catch
            {
                // Log or handle as needed
            }
            return membersList;
        }

        public async Task<bool> CreateAppRoleAsync(string appId, AppRoleCreationRequest appRoleRequest)
        {
            try
            {
                var graphClient = GetGraphClientAsync();
                var application = await graphClient.Applications[appId]
                    .GetAsync(rc =>
                    {
                        rc.QueryParameters.Select = new[] { "appRoles" };
                    });

                if (application == null)
                    return false;

                var newAppRole = new Microsoft.Graph.Models.AppRole
                {
                    Id = Guid.NewGuid(),
                    DisplayName = appRoleRequest.RoleName,
                    Description = appRoleRequest.RoleDescription,
                    IsEnabled = appRoleRequest.IsEnabled,
                    Value = appRoleRequest.RoleName,
                    AllowedMemberTypes = new List<string> { "User" }
                };

                var updatedAppRoles = application.AppRoles ?? new List<Microsoft.Graph.Models.AppRole>();
                updatedAppRoles.Add(newAppRole);

                var updatedApplication = new Microsoft.Graph.Models.Application
                {
                    AppRoles = updatedAppRoles
                };

                await graphClient.Applications[appId].PatchAsync(updatedApplication);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<Groups>> GetGroupsOwnedByUserAsync(string userId)
        {
            var ownedGroups = new List<Groups>();
            try
            {
                var graphClient = GetGraphClientAsync();
                var groups = await graphClient.Users[userId].OwnedObjects
                    .GetAsync(rc =>
                    {
                        rc.QueryParameters.Select = new[] { "id", "displayName", "mail" };
                    });

                if (groups?.Value != null)
                {
                    foreach (var directoryObject in groups.Value)
                    {
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
            catch
            {
                // Log or handle as needed
            }
            return ownedGroups;
        }

        public async Task<bool> AddUsersToGroupAsync(List<string> userIds, string groupId)
        {
            try
            {
                var graphClient = GetGraphClientAsync();
                var memberOdataBind = userIds
                    .Select(id => $"{GraphApiUrl}directoryObjects/{id}")
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
            catch
            {
                return false;
            }
        }

        public async Task<List<Users>> GetUsersByGroupIdAsync(string groupId)
        {
            var usersList = new List<Users>();
            try
            {
                var graphClient = GetGraphClientAsync();
                var members = await graphClient.Groups[groupId].Members
                    .GetAsync(rc =>
                    {
                        rc.QueryParameters.Select = new[] { "id", "displayName", "userPrincipalName", "identities" };
                    });

                if (members?.Value != null)
                {
                    foreach (var member in members.Value)
                    {
                        if (member is User user)
                        {
                            string email = user.UserPrincipalName;
                            var emailIdentity = user.Identities?.FirstOrDefault(i => i.SignInType == "emailAddress");
                            if (emailIdentity != null)
                                email = emailIdentity.IssuerAssignedId;

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
            catch
            {
                // Log or handle as needed
            }
            return usersList;
        }

        public async Task<bool> RemoveUserFromGroupAsync(string userId, string groupId)
        {
            try
            {
                var graphClient = GetGraphClientAsync();
                await graphClient.Groups[groupId].Members[userId].Ref.DeleteAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}