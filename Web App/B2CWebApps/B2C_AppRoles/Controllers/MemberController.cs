using B2C_AppRoles.MSGraphServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace B2C_AppRoles.Controllers
{
    [Authorize]
    public class MemberController : Controller
    {
        private readonly IMSGraphApiServices _msGraphApiServices;

        public MemberController(IMSGraphApiServices msGraphApiServices)
        {
            _msGraphApiServices = msGraphApiServices;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> GetUserByName(string searchTerm)
        {
            var users = await _msGraphApiServices.GetMembersByTypeAndFilterAsync("user", searchTerm);
            //  return PartialView("_UserList", users);
            return Json(users);
        }

        public async Task<IActionResult> GetGroupByName(string searchTerm)
        {
            var users = await _msGraphApiServices.GetMembersByTypeAndFilterAsync("group", searchTerm);
            //  return PartialView("_UserList", users);
            return Json(users);
        }
    }
}
