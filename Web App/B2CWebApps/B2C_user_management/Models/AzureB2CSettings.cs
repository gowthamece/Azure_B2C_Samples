namespace B2C_user_management.Models;

/// <summary>
/// Azure AD B2C configuration settings
/// </summary>
public class AzureB2CSettings
{
    public string TenantId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string TenantName { get; set; } = string.Empty;
    
    /// <summary>
    /// The B2C domain (e.g., "yourtenant.onmicrosoft.com")
    /// </summary>
    public string B2CDomain => $"{TenantName}.onmicrosoft.com";
}
