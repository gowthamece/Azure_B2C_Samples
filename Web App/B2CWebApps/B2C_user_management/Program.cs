using B2C_user_management.Components;
using B2C_user_management.Models;
using B2C_user_management.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Configure Azure B2C settings
builder.Services.Configure<AzureB2CSettings>(builder.Configuration.GetSection("AzureB2C"));

// Register HttpClient for the migration service
builder.Services.AddHttpClient<IUserMigrationService, UserMigrationService>();

// Register services
builder.Services.AddScoped<IExcelService, ExcelService>();
builder.Services.AddScoped<IGraphUserService, GraphUserService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
