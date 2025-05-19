using NuGet.Protocol;

namespace B2C_AppRoles.Models
{
    public class AppRoleCreationRequest
    {
        public string ApplicationId { get; set; }
        public string RoleName { get; set; }
        public string RoleDescription { get; set; }
        
        public string RoleValue { get; set; }
        public bool IsEnabled { get; set; }

    }
}
