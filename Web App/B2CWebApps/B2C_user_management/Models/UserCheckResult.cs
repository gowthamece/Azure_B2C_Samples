namespace B2C_user_management.Models;

/// <summary>
/// Represents the result of checking a user's existence in Azure AD B2C
/// </summary>
public class UserCheckResult
{
    public string Email { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string IsExist { get; set; } = "N";
    public string? B2CObjectId { get; set; }
    public string? ErrorMessage { get; set; }
}
