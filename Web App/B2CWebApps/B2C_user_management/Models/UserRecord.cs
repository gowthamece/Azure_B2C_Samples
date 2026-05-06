namespace B2C_user_management.Models;

/// <summary>
/// Represents a user record from the uploaded Excel file
/// </summary>
public class UserRecord
{
    public string Email { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
}
