using B2C_AppRoles.Models;
using B2C_AppRoles.MSGraphServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace B2C_AppRoles.Controllers
{
    [Authorize]
    public class RoleController : Controller
    {
        private readonly IMSGraphApiServices _msGraphApiServices;

        public RoleController(IMSGraphApiServices msGraphApiServices)
        {
            _msGraphApiServices = msGraphApiServices;
        }

        public async Task<IActionResult> Index(string Id)
        {
            if (string.IsNullOrEmpty(Id))
            {
                return BadRequest("Application ID cannot be null or empty.");
            }

            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");
            userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier");
            if (userIdClaim == null || string.IsNullOrEmpty(userIdClaim.Value))
            {
                return Unauthorized("User ID claim is missing or invalid.");
            }
            var userId = userIdClaim.Value;

            var accessResponse = await _msGraphApiServices.GetApplicationOwnerAuthorizationAsync(userId, Id);

            if (accessResponse is UnauthorizedResult)
            {
                return PartialView("_Unauthorized");
            }

            var roleList = await _msGraphApiServices.GetAppRolesAsync(Id);
            ViewBag.AppID = Id;
            return View(roleList);
        }

        [HttpPost]
        public async Task<IActionResult> AssignRole([FromForm] string id, [FromForm] string name, [FromForm] string appId)
        {
            var users = await _msGraphApiServices.GetUserByAppRoleId(id, appId);
            ViewBag.AppId = appId;
            ViewBag.RoleId = id;
            return PartialView("_UserList", users);
        }

        [HttpPost]
        public async Task<IActionResult> AssignMember([FromBody] AssignMemeberRequest request)
        {
            if (request?.MemberIds == null || request.MemberIds.Count == 0)
            {
                return BadRequest("UserIds cannot be null or empty.");
            }

            foreach (var userId in request.MemberIds)
            {
                await _msGraphApiServices.AssignUserToAppRole(userId, request.AppId, request.RoleId, request.MemeberType);
            }

            return Ok("Member assigned successfully.");
        }

        [HttpPost]
        public async Task<IActionResult> RevokeMember([FromBody] AssignMemeberRequest request)
        {
            foreach (var userId in request.MemberIds)
            {
                await _msGraphApiServices.RevokeMemberFromAppRole(userId, request.AppId, request.RoleId, request.MemeberType);
            }

            return Ok("Member revoked successfully.");
        }

        public IActionResult Create(string appId)
        {
            var appIdTemp = HttpContext.Request.Query["appId"].ToString();
            if (string.IsNullOrEmpty(appId))
            {
                return BadRequest("Application ID cannot be null or empty.");
            }

            ViewData["AppId"] = appId;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> PostAppRole([FromForm] AppRoleCreationRequest appRole)
        {
            if (string.IsNullOrEmpty(appRole.RoleValue) || string.IsNullOrEmpty(appRole.ApplicationId))
            {
                return BadRequest("Display name and Application ID cannot be null or empty.");
            }

            var result = await _msGraphApiServices.CreateAppRoleAsync(appRole.ApplicationId, appRole);

            if (result == null)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, "Failed to create app role.");
            }

            return Ok("App role created successfully.");
        }
    }

}
