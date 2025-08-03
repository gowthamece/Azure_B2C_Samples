using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Identity.Web;

namespace BlazorFluentUI.Pages
{
    public class SignOutModel : PageModel
    {
        public IActionResult OnGet()
        {
            // Sign out from both the local cookies and Azure AD
            var returnUrl = Url.Content("~/");
            
            return SignOut(
                new AuthenticationProperties 
                { 
                    RedirectUri = returnUrl 
                },
                CookieAuthenticationDefaults.AuthenticationScheme,
                OpenIdConnectDefaults.AuthenticationScheme
            );
        }
    }
}
