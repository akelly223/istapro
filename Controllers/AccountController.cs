using System.Threading.Tasks;
using GestionScolaire.Data;
using GestionScolaire.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace GestionScolaire.Controllers
{
    public class AccountController : Controller
    {
        // SignInManager gère la connexion/déconnexion (vérifie le mot de passe, pose le cookie...).
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;

        public AccountController(SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

        // Affiche le formulaire de connexion.
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel());
        }

        // Traite le formulaire de connexion, quel que soit le rôle du compte (Administrateur, Professeur, Etudiant).
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

                // Sans page précise demandée au départ, chaque rôle est envoyé vers son propre espace.
                var utilisateur = await _userManager.FindByEmailAsync(model.Email);
                if (utilisateur != null)
                {
                    if (await _userManager.IsInRoleAsync(utilisateur, AppRoles.Etudiant))
                    {
                        return RedirectToAction("MonEspace", "Home");
                    }
                    if (await _userManager.IsInRoleAsync(utilisateur, AppRoles.Professeur))
                    {
                        return RedirectToAction("EspaceProfesseur", "Home");
                    }
                }

                return RedirectToAction("Dashboard", "Home");
            }

            ModelState.AddModelError(string.Empty, "Email ou mot de passe incorrect.");
            return View(model);
        }

        // Déconnecte l'utilisateur : supprime le cookie d'authentification.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        // Affichée quand un utilisateur connecté essaie d'accéder à une page réservée à un autre rôle.
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
