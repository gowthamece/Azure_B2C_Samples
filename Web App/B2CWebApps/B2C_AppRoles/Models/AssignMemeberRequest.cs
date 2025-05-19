namespace B2C_AppRoles.Models
{
    public class AssignMemeberRequest
    {
        public List<string> MemberIds { get; set; }
        public string RoleId { get; set; }
        public string AppId { get; set; }
        public string GroupId { get; set; }
        public string MemeberType { get; set; }
    }
}
