namespace BlazorFluentUI.Models
{
    public class AssignMemberRequest
    {
        public List<string> MemberIds { get; set; } = new();
        public string RoleId { get; set; } = string.Empty;
        public string AppId { get; set; } = string.Empty;
        public string GroupId { get; set; } = string.Empty;
        public string MemberType { get; set; } = string.Empty;
    }
}
