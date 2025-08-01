using BlazorFluentUI.Models;
using Microsoft.Graph;

namespace BlazorFluentUI.Services
{
    public interface IMSGraphApiServices
    {        
        Task<bool> GetApplicationOwnerAuthorizationAsync(string email, string appId);
        Task<List<Users>> GetUsersAsync();
        Task<List<Groups>> GetGroupsAsync();
        Task<List<Application>> GetApplicationsAsync();
        Task<List<AppRole>> GetAppRolesAsync(string appId);
        Task<bool> AssignUserToAppRole(string principalId, string resourceId, string appRoleId, string memberType = "user");
        Task<bool> RevokeMemberFromAppRole(string principalId, string resourceId, string appRoleId, string memberType = "user");
        Task<List<Users>> GetUserByAppRoleId(string roleId, string appId);
        GraphServiceClient GetGraphClientAsync();
        string GetCachedAccessToken(string tenantId, int secondsRemaining = 60);
        void CacheAccessToken(string tenantId, string accesToken);
        Task<List<Users>> GetMembersByTypeAndFilterAsync(string memberType, string firstNameStartsWith);
        Task<bool> CreateAppRoleAsync(string appId, AppRoleCreationRequest appRoleRequest);
        Task<List<Groups>> GetGroupsOwnedByUserAsync(string userId);
        Task<List<Application>> GetApplicationsOwnedByUserAsync(string userId);
        Task<bool> AddUsersToGroupAsync(List<string> userIds, string groupId);
        Task<List<Users>> GetUsersByGroupIdAsync(string groupId);
        Task<bool> RemoveUserFromGroupAsync(string userId, string groupId);
    }
}
