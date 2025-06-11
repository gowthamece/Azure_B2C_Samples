using B2C_AppRoles.Models;
using B2C_AppRoles.MSGraphServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;

namespace B2C_AppRoles.Controllers
{
    [Authorize]
    public class GroupController : Controller
    {
        private readonly IMSGraphApiServices _msGraphApiServices;

        public GroupController(IMSGraphApiServices msGraphApiServices)
        {
            _msGraphApiServices = msGraphApiServices;
        }

        public async Task<IActionResult> Index()
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier");
            var userId = userIdClaim.Value;
            var groupResult = await _msGraphApiServices.GetGroupsOwnedByUserAsync(userId);
            return View(groupResult);
        }
        public async Task<IActionResult> Get()
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier");
            if (userIdClaim == null || string.IsNullOrEmpty(userIdClaim.Value))
            {
                return BadRequest("User identifier claim not found.");
            }
            var userId = userIdClaim.Value;
            var groupResult = await _msGraphApiServices.GetGroupsOwnedByUserAsync(userId);
            return Ok(groupResult.Count);
        }
        [HttpGet]
        public async Task<IActionResult> AssignMemberToGroupPartial(string groupId, string groupName)
        {
            if (string.IsNullOrEmpty(groupId) || string.IsNullOrEmpty(groupName))
            {
                return BadRequest("GroupId and GroupName are required.");
            }

            var model = new AssignMemberToGroupViewModel
            {
                GroupId = groupId,
                GroupName = groupName
            };

            try
            {
                return PartialView("_AssignMemberToGroup", model);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                return StatusCode(500, $"Error rendering partial view: {ex.Message}");
            }
        }

        public async Task<IActionResult> AssignUserToGroup([FromBody] AssignMemeberRequest request)
        {
            if (request?.MemberIds == null || request.MemberIds.Count == 0)
            {
                return BadRequest("UserIds cannot be null or empty.");
            }
            foreach (var userId in request.MemberIds)
            {
                await _msGraphApiServices.AddUsersToGroupAsync(request.MemberIds, request.GroupId);
            }
            return Ok("Member assigned successfully.");
        }

        [HttpPost]
        public async Task<IActionResult> GetUsersByGroupId([FromForm] string groupId, [FromForm] string groupName)
        {
            if (string.IsNullOrEmpty(groupId))
            {
                return BadRequest("GroupId is required.");
            }

            try
            {
                ViewBag.groupName = groupName;
                ViewBag.groupId = groupId;
                var users = await _msGraphApiServices.GetUsersByGroupIdAsync(groupId);
                return PartialView("_ManageMembers", users);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving users: {ex.Message}");
            }
        }

        [HttpPost]
        public async Task<IActionResult> RemoveUserFromGroup([FromBody] AssignMemeberRequest request)
        {
            if (request?.MemberIds == null || request.MemberIds.Count == 0)
            {
                return BadRequest("UserIds cannot be null or empty.");
            }
            try
            {
                foreach (var userId in request.MemberIds)
                {
                    await _msGraphApiServices.RemoveUserFromGroupAsync(userId, request.GroupId);
                }
                return Ok("User removed from group successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error removing user from group: {ex.Message}");
            }
        }
    }
}
