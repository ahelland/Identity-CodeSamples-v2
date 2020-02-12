using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using aad_b2c_custom_policies_dotnet_core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;

namespace B2CNETCoreWebApp.Controllers
{
    public class HomeController : Controller
    {
        public HomeController(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public IActionResult Index()
        {
            return View();
        }

        [Authorize(Policy = "Partner")]
        public IActionResult Partner()
        {
            ViewData["Message"] = "Howdy partner!";
            return View();
        }

        [Authorize(Policy = "Customer")]
        public IActionResult Customer()
        {
            ViewData["Message"] = "Welcome dear customer. How may we help you today.";
            return View();
        }

        [Authorize(Policy = "Employee")]
        public IActionResult Employee()
        {
            ViewData["Message"] = "Get back to work!";
            return View();
        }       

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        //Separate SignIn handler for magic links sent by email
        public IActionResult SignInLink(string id_token_hint)
        {
            var magic_link_auth = new AuthenticationProperties { RedirectUri = "/" };
            magic_link_auth.Items.Add("id_token_hint", id_token_hint);

            string magic_link_policy = Configuration.GetSection("AzureAdB2C")["MagicLinkPolicyId"];

            return this.Challenge(magic_link_auth, magic_link_policy);
        }
    }
}