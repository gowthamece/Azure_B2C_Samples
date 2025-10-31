using BlazorFluentUI.Components;
using BlazorFluentUI.Services;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using Azure.Identity;
using Azure.Security.KeyVault;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add authentication
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));

builder.Services.Configure<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme, options =>
{
    var postLogoutRedirectUri = builder.Configuration["AzureAd:PostLogoutRedirectUri"];

    options.Events = new OpenIdConnectEvents
    {
        OnRedirectToIdentityProviderForSignOut = context =>
        {
            if (!string.IsNullOrEmpty(postLogoutRedirectUri))
            {
                // Construct the logout URI with the post-logout redirect URI - This will overcome the issue in guest user sign-out with common endpoint

                var logoutUri = $"https://login.microsoftonline.com/common/oauth2/v2.0/logout" +
                    $"?post_logout_redirect_uri={Uri.EscapeDataString($"https://{context.Request.Host}/{postLogoutRedirectUri}")}";

                context.Response.Redirect(logoutUri);
                context.HandleResponse();
                return Task.CompletedTask;

            }

            return Task.CompletedTask;
        }
    };
});


// Add Key Vault configuration if in production or staging
//if (builder.Environment.IsProduction() || builder.Environment.IsStaging())
//{
//    Uri kvUri = new(builder.Configuration["AzureEndPoints:keyVaultName"] ?? "https://kv-entraid-dev-eastus-01.vault.azure.net/");
//    var options = new DefaultAzureCredentialOptions
//    {
//        ExcludeEnvironmentCredential = false
//    };
//    builder.Configuration.AddAzureKeyVault(kvUri, new DefaultAzureCredential());
//}

// Add authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("TeamManager", p =>
    {
        p.RequireClaim("http://schemas.microsoft.com/ws/2008/06/identity/claims/role", "TeamManager");
    });
});

// Add Razor Pages with Microsoft Identity UI
builder.Services.AddRazorPages()
    .AddMicrosoftIdentityUI();

// Add HTTP services
builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();

// Add FluentUI components
builder.Services.AddFluentUIComponents();

// Add custom services
builder.Services.AddScoped<IMSGraphApiServices, MSGraphApiServices>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
//app.UsePathBase("/AppRoleManagement");
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.UseStaticFiles();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Map Razor Pages for authentication
app.MapRazorPages();

// Map controllers for Microsoft Identity
app.MapControllers();

app.Run();
