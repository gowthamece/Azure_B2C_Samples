using B2C_AppRoles.Models;
using B2C_AppRoles.MSGraphServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Claims;
using System.Threading.Tasks;

namespace B2C_AppRoles.Controllers
{
    
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IMSGraphApiServices _msGraphApiServices;

        public HomeController(ILogger<HomeController> logger, IMSGraphApiServices msGraphApiServices)
        {
            _logger = logger;
            _msGraphApiServices = msGraphApiServices;
        }
        [Authorize]
        public async Task<IActionResult> Index()
        {
            // Get user object id claim
            var userId = User.Claims.FirstOrDefault(c => c.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                ViewData["GroupCount"] = 0;
                ViewData["Assigned Role"] = 0;
                return View();
            }

            // Get group count (groups the user is a member of)
            var groupCountTask = await _msGraphApiServices.GetGroupsOwnedByUserAsync(userId);
            // Get assigned roles count (roles assigned to the user)
            var applicationTask = await _msGraphApiServices.GetApplicationsAsync();

            //Task.WaitAll(groupCountTask, assignedRolesTask);

            var groupCount = groupCountTask?.Count ?? 0;
            var applicationCount = applicationTask?.Count ?? 0;

            ViewData["GroupCount"] = groupCount;
            ViewData["AppCount"] = applicationCount;
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [Authorize(Policy = "TeamManager")]
        public IActionResult TeamManager()
        {
            var email = User.Claims.FirstOrDefault(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.Value;

            var result = _msGraphApiServices.GetApplicationsAsync();
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
