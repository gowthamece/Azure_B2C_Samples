using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;

namespace WeatherApp
{
    public class CustomAccountController : Controller
    {
        [HttpGet("{scheme?}")]
        public async Task<IActionResult> SignOutAsync([FromRoute] string scheme)
        {
            scheme ??= OpenIdConnectDefaults.AuthenticationScheme;
            var idToken= await HttpContext.GetTokenAsync("id_token");
            var redirectUrl = Url.Content("~/");
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
            properties.Items.Add("id_token_hint", idToken);
            return SignOut(properties,CookieAuthenticationDefaults.AuthenticationScheme, scheme);
            
        }

        [HttpGet("{scheme?}")]
        public async Task<IActionResult> RemoteSignOut([FromRoute] string scheme)
        {
            scheme ??= OpenIdConnectDefaults.AuthenticationScheme;
            var redirectUrl = Url.Content("~/");
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
            return SignOut(properties, CookieAuthenticationDefaults.AuthenticationScheme, scheme);
        }

        }
}
