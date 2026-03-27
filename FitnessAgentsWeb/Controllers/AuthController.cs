using FitnessAgentsWeb.Core.Configuration;
using FitnessAgentsWeb.Models.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace FitnessAgentsWeb.Controllers
{
    public class AuthController : Controller
    {
        private readonly IAppConfigurationManager _configManager;
        private readonly IConfiguration _configuration;

        public AuthController(IAppConfigurationManager configManager, IConfiguration configuration)
        {
            _configManager = configManager;
            _configuration = configuration;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Login(string? error = null)
        {
            if (!await _configManager.IsAppConfiguredAsync())
            {
                return RedirectToAction("Index", "Setup");
            }

            if (User.Identity is not null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Overview");
            }

            var externalAuth = _configuration.GetSection("ExternalAuth");
            var viewModel = new LoginViewModel
            {
                SsoEnabled = externalAuth.GetValue<bool>("Enabled") && !string.IsNullOrWhiteSpace(externalAuth["Authority"]),
                SsoDisplayName = externalAuth["DisplayName"] ?? "SSO",
                SsoIcon = externalAuth["Icon"] ?? "fa-solid fa-arrow-right-to-bracket"
            };

            if (error == "sso_failed")
            {
                viewModel.ErrorMessage = "SSO authentication failed. Please try again or use local credentials.";
            }

            return View(viewModel);
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Login(string email, string password, [FromServices] Core.Interfaces.IStorageRepository storageRepo)
        {
            // Note: 'email' field in form is now used as 'username' or 'userId'
            string username = email?.Trim().ToLowerInvariant() ?? "";
            
            // 1. Check if this is the Admin Login
            bool isAdmin = await _configManager.ValidateAdminLoginAsync(username, password);
            if (isAdmin)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, username),
                    new Claim(ClaimTypes.Role, "Admin")
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));
                return RedirectToAction("Index", "Overview");
            }

            // 2. Check if this is a Standard User Login
            var users = await storageRepo.GetAllUserProfilesAsync();
            if (users.TryGetValue(username, out var profile))
            {
                if (profile.IsActive && PasswordHasher.VerifyPassword(password, profile.PasswordHash))
                {
                    var role = profile.IsAdmin ? "Admin" : "User";
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, username),
                        new Claim(ClaimTypes.Role, role)
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));
                    return RedirectToAction("Index", "Overview", new { userId = username });
                }
            }

            return View(new LoginViewModel { ErrorMessage = "Invalid credentials or account disabled." });
        }

        /// <summary>
        /// Initiates the OIDC SSO login flow with the configured external provider.
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ExternalLogin()
        {
            var properties = new AuthenticationProperties
            {
                RedirectUri = Url.Action(nameof(ExternalLoginCallback))
            };
            return Challenge(properties, OpenIdConnectDefaults.AuthenticationScheme);
        }

        /// <summary>
        /// Handles the callback after external OIDC SSO authentication completes.
        /// </summary>
        [HttpGet]
        [Authorize]
        public IActionResult ExternalLoginCallback()
        {
            return RedirectToAction("Index", "Overview");
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // If the user signed in via OIDC, also sign out from the external provider
            if (User.Identity?.AuthenticationType == OpenIdConnectDefaults.AuthenticationScheme ||
                User.Identity?.AuthenticationType == "AuthenticationTypes.Federation")
            {
                return SignOut(
                    new AuthenticationProperties { RedirectUri = Url.Action("Login") },
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    OpenIdConnectDefaults.AuthenticationScheme);
            }

            return RedirectToAction("Login");
        }
    }
}
