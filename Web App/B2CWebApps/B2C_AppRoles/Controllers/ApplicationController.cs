using B2C_AppRoles.MSGraphServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;

namespace B2C_AppRoles.Controllers
{
    [Authorize]
    public class ApplicationController : Controller
    {
        private readonly IMSGraphApiServices _msGraphApiServices;

        public ApplicationController(IMSGraphApiServices msGraphApiServices)
        {
            _msGraphApiServices = msGraphApiServices;
        }

        public async Task<IActionResult> Index()
        {
            var email = User.Claims.FirstOrDefault(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier").Value.ToString();

            var applicationList = await _msGraphApiServices.GetApplicationsAsync();
            return View(applicationList);
        }

        public async Task<IActionResult> CheckApplicationAccess(string appId)
        {
            var emailClaim = User.Claims.FirstOrDefault(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");
            if (emailClaim == null || string.IsNullOrEmpty(emailClaim.Value))
            {
                return BadRequest("User email claim is missing or invalid.");
            }

            var email = emailClaim.Value;

            var applicationStatusResult = await _msGraphApiServices.GetApplicationOwnerAuthorizationAsync(email, appId);
            var applicationStatus = (applicationStatusResult as ObjectResult)?.StatusCode ?? (int)HttpStatusCode.InternalServerError;

            if (applicationStatus == (int)HttpStatusCode.OK)
            {
                return Ok();
            }
            else if (applicationStatus == (int)HttpStatusCode.Forbidden)
            {
                return Forbid();
            }
            else if (applicationStatus == (int)HttpStatusCode.NotFound)
            {
                return NotFound();
            }
            else
            {
                return StatusCode(applicationStatus);
            }
        }
    }
}
