using Azure.Identity;
using B2C_AppRoles.Models;
using B2C_AppRoles.MSGraphServices;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using Azure.Security.KeyVault;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));

if (builder.Environment.IsProduction() || builder.Environment.IsStaging())
{
    Uri kvUri = new(builder.Configuration["AzureEndPoints:keyVaultName"] ?? "https://kv-entraid-dev-eastus-01.vault.azure.net/");
    var options = new DefaultAzureCredentialOptions
    {
        ExcludeEnvironmentCredential = false
    };
    builder.Configuration.AddAzureKeyVault(kvUri, new DefaultAzureCredential());
}

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("TeamManager", p =>
    {
        p.RequireClaim("http://schemas.microsoft.com/ws/2008/06/identity/claims/role", "TeamManager");
    });
});
builder.Services.AddControllersWithViews(options =>
{

});
builder.Services.AddRazorPages()
    .AddMicrosoftIdentityUI();
builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<IMSGraphApiServices, MSGraphApiServices>();
var app = builder.Build();


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
