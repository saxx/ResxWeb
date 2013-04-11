using System.Linq;
using System.Web.Mvc;
using System.Web.Security;
using ResxWeb.Models;
using ResxWeb.Models.ViewModels;
using ResxWeb.Models.ViewModels.Account;

namespace ResxWeb.Controllers
{
    public class AccountController : Controller
    {
        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var settings = Settings.Load();
                var matchingUser = settings.Users.FirstOrDefault(x => x.UserName == model.UserName && x.Password == model.Password);
                if (matchingUser != null)
                {
                    FormsAuthentication.RedirectFromLoginPage(matchingUser.UserName, false);
                    return Redirect("~/");
                }
                ModelState.AddModelError("username", "Invalid username or password.");
            }
            return View(model);
        }

        public ActionResult Logoff()
        {
            FormsAuthentication.SignOut();
            return Redirect("~/");
        }

    }
}
