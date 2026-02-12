namespace ADB2C_UserMigration.Models;

/// <summary>
/// Request model for migrating a single user to Azure AD B2C
/// </summary>
public class UserMigrationRequest
{
    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? DisplayName { get; set; }
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// Request model for batch user migration
/// </summary>
public class BatchUserMigrationRequest
{
    public List<UserMigrationRequest> Users { get; set; } = new();
    public bool ForceChangePasswordNextSignIn { get; set; } = true;
}

/// <summary>
/// Result of a single user migration attempt
/// </summary>
public class UserMigrationResult
{
    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? DisplayName { get; set; }
    public bool Success { get; set; }
    public string? B2CObjectId { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Response model for batch user migration
/// </summary>
public class BatchUserMigrationResponse
{
    public int TotalUsers { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<UserMigrationResult> Results { get; set; } = new();
}

/// <summary>
/// Request model for revoking/deleting users from Azure AD B2C
/// </summary>
public class RevokeUsersRequest
{
    public List<string> UserObjectIds { get; set; } = new();
}

/// <summary>
/// Result of a single user revoke/delete attempt
/// </summary>
public class RevokeUserResult
{
    public string B2CObjectId { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Response model for batch user revoke
/// </summary>
public class RevokeUsersResponse
{
    public int TotalUsers { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<RevokeUserResult> Results { get; set; } = new();
}
