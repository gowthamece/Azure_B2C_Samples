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

// Add Key Vault configuration if in production or staging
if (builder.Environment.IsProduction() || builder.Environment.IsStaging())
{
    Uri kvUri = new(builder.Configuration["AzureEndPoints:keyVaultName"] ?? "https://kv-entraid-dev-eastus-01.vault.azure.net/");
    var options = new DefaultAzureCredentialOptions
    {
        ExcludeEnvironmentCredential = false
    };
    builder.Configuration.AddAzureKeyVault(kvUri, new DefaultAzureCredential());
}

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

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.UseStaticFiles();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Map Razor Pages for authentication
app.MapRazorPages();

app.Run();
