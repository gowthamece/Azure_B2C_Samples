using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace WeatherAppMVC.Controllers
{
    /// <summary>
    /// Controller to handle custom account-related actions such as signing out.
    /// </summary>
    [AllowAnonymous]
    [Route("[controller]/[action]")]
    public class CustomAccountController : Controller
    {
        /// <summary>
        /// Signs the user out of the application.
        /// </summary>
        /// <param name="scheme">The authentication scheme to use. Defaults to OpenIdConnect if not provided.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains an IActionResult.</returns>
        [HttpGet("{scheme?}")]
        public async Task<IActionResult> SignOutAsync([FromRoute] string scheme)
        {
            // Set the default authentication scheme if none is provided
            scheme ??= OpenIdConnectDefaults.AuthenticationScheme;

            // Authenticate the user using the provided scheme
            var authenticateResult = await HttpContext.AuthenticateAsync(scheme);

            // Retrieve the ID token from the authentication result
            var idToken = authenticateResult.Properties.GetTokenValue("id_token");

            // Define the callback URL to redirect to after sign-out
            var callbackUrl = Url.Content("https://localhost:44373/");

            // Create authentication properties with the callback URL
            var properties = new AuthenticationProperties { RedirectUri = callbackUrl };

            // Add the ID token hint to the properties
            properties.Items.Add("id_token_hint", idToken);

            // Sign the user out using the specified schemes
            return SignOut(properties, CookieAuthenticationDefaults.AuthenticationScheme, scheme);
        }

        /// <summary>
        /// Signs the user out of the remote authentication provider.
        /// </summary>
        /// <param name="scheme">The authentication scheme to use. Defaults to OpenIdConnect if not provided.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains an IActionResult.</returns>
        [HttpGet("{scheme?}")]
        public async Task<IActionResult> RemoteSignOut([FromRoute] string scheme)
        {
            // Set the default authentication scheme if none is provided
            scheme ??= OpenIdConnectDefaults.AuthenticationScheme;

            // Define the redirect URL to redirect to after sign-out
            var redirectUrl = Url.Content("~/");

            // Create authentication properties with the redirect URL
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };

            // Sign the user out using the specified schemes
            return SignOut(properties, CookieAuthenticationDefaults.AuthenticationScheme, scheme);
        }
    }
}
