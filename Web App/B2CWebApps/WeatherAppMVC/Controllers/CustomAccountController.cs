using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace WeatherAppMVC.Controllers
{
    [AllowAnonymous]
    [Route("[controller]/[action]")]
    public class CustomAccountController : Controller
    {
        [HttpGet("{scheme?}")]
        public async Task<IActionResult> SignOutAsync([FromRoute] string scheme)
        {
            scheme ??= OpenIdConnectDefaults.AuthenticationScheme;
            var authenticateResult = await HttpContext.AuthenticateAsync(scheme);
            var idToken = authenticateResult.Properties.GetTokenValue("id_token");
           // var callbackUrl = Url.Page("/Account/SignedOut", pageHandler: null, values: null, protocol: Request.Scheme);
            var callbackUrl = Url.Content("https://localhost:44373/");
            var properties = new AuthenticationProperties { RedirectUri = callbackUrl };
            properties.Items.Add("id_token_hint", idToken);
            return SignOut(properties, CookieAuthenticationDefaults.AuthenticationScheme, scheme);

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
