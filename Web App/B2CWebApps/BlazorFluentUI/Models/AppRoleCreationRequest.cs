namespace BlazorFluentUI.Models
{
    public class AppRoleCreationRequest
    {
        public string ApplicationId { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public string RoleDescription { get; set; } = string.Empty;
        public string RoleValue { get; set; } = string.Empty;
        public bool IsEnabled { get; set; } = true;
    }
}
