using B2C_AppRoles.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph;

namespace B2C_AppRoles.MSGraphServices
{
    public interface IMSGraphApiServices
    {        
        Task<IActionResult> GetApplicationOwnerAuthorizationAsync(string email, string appId);
        Task<List<Users>> GetUsersAsync();
        Task<List<Groups>> GetGroupsAsync();
        Task<List<Application>> GetApplicationsAsync();
        Task<List<AppRole>> GetAppRolesAsync(string appId);
        Task AssignUserToAppRole(string principalId, string resourceId, string appRoleId, string memberType = "user");
        Task RevokeMemberFromAppRole(string principalId, string resourceId, string appRoleId, string memberType = "user");
        Task<List<Users>> GetUserByAppRoleId(string roleId, string appId);
        GraphServiceClient GetGraphClientAsync();
        string GetCachedAccessToken(string tenantId, int secondsRemaining = 60);
        void CacheAccessToken(string tenantId, string accesToken);
         Task<List<Users>> GetMembersByTypeAndFilterAsync(string memberType, string firstNameStartsWith);
        Task<bool> CreateAppRoleAsync(string appId, AppRoleCreationRequest appRoleRequest);
        Task<List<Groups>> GetGroupsOwnedByUserAsync(string userId);
        Task<bool> AddUsersToGroupAsync(List<string> userIds, string groupId);
        Task<List<Users>> GetUsersByGroupIdAsync(string groupId);
        Task<bool> RemoveUserFromGroupAsync(string userId, string groupId);

    }
}
