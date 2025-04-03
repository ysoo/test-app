using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;

namespace yarp.Controllers
{
    public class AccountController : Controller
    {
        private readonly ITokenAcquisition _tokenAcquisition;

        public AccountController(ITokenAcquisition tokenAcquisition)
        {
            _tokenAcquisition = tokenAcquisition;
        }

        public IActionResult Consent()
        {
            return Challenge(new AuthenticationProperties 
            { 
                RedirectUri = Url.Action("Index", "Home"),
                Items = 
                {
                    { "prompt", "consent" },
                    { "scope", "https://management.azure.com/user_impersonation" }
                }
            });
        }
    }
} 