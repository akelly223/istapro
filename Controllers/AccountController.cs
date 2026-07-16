using System.Threading.Tasks;
using GestionScolaire.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace GestionScolaire.Controllers
{
    public class AccountController : Controller
    {
        // SignInManager gère la connexion/déconnexion (vérifie le mot de passe, pose le cookie...).
        private readonly SignInManager<IdentityUser> _signInManager;

        public AccountController(SignInManager<IdentityUser> signInManager)
        {
            _signInManager = signInManager;
        }

        // Affiche le formulaire de connexion.
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel());
        }

        // Traite le formulaire de connexion soumis par l'administrateur.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // PasswordSignInAsync vérifie l'email/mot de passe et, si c'est correct,
            // crée le cookie d'authentification (l'utilisateur est alors "connecté").
            var result = await _signInManager.PasswordSignInAsync(
                model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                return RedirectToAction("Dashboard", "Home");
            }

            ModelState.AddModelError(string.Empty, "Email ou mot de passe incorrect.");
            return View(model);
        }

        // Déconnecte l'administrateur : supprime le cookie d'authentification.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }
    }
}
