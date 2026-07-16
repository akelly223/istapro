using System.Linq;
using System.Threading.Tasks;
using GestionScolaire.Data;
using GestionScolaire.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace GestionScolaire.Controllers
{
    // L'administrateur peut tout faire ; le professeur peut seulement consulter la liste
    // (voir les attributs [Authorize] sur Create/Edit/Delete plus bas).
    [Authorize(Roles = AppRoles.Administrateur + "," + AppRoles.Professeur)]
    public class StudentsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public StudentsController(AppDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Students
        // Affiche la liste de tous les étudiants, avec le nom de leur classe.
        public async Task<IActionResult> Index()
        {
            var etudiants = await _context.Students
                .Include(e => e.ClassRoom)
                .OrderBy(e => e.Nom)
                .ToListAsync();

            return View(etudiants);
        }

        // GET: /Students/Create
        [Authorize(Roles = AppRoles.Administrateur)]
        public IActionResult Create()
        {
            ViewBag.Classes = new SelectList(_context.ClassRooms.OrderBy(c => c.Nom), "Id", "Nom");
            return View();
        }

        // POST: /Students/Create
        // En plus d'enregistrer l'étudiant, on crée automatiquement son compte de connexion
        // (rôle Etudiant), avec pour mot de passe sa date de naissance au format jjMMaaaa.
        [HttpPost]
        [Authorize(Roles = AppRoles.Administrateur)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Student student)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Classes = new SelectList(_context.ClassRooms.OrderBy(c => c.Nom), "Id", "Nom", student.ClassRoomId);
                return View(student);
            }

            _context.Students.Add(student);
            await _context.SaveChangesAsync();

            string motDePasse = student.DateNaissance.ToString("ddMMyyyy");
            var compte = new IdentityUser { UserName = student.Email, Email = student.Email, EmailConfirmed = true };
            var resultat = await _userManager.CreateAsync(compte, motDePasse);

            if (resultat.Succeeded)
            {
                await _userManager.AddToRoleAsync(compte, AppRoles.Etudiant);
                student.UserId = compte.Id;
                await _context.SaveChangesAsync();

                TempData["Message"] = $"Étudiant ajouté avec succès. Identifiants de connexion : {student.Email} / {motDePasse}";
            }
            else
            {
                TempData["Message"] = "Étudiant ajouté, mais le compte de connexion n'a pas pu être créé (cet email est peut-être déjà utilisé par un autre compte).";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: /Students/Edit/5
        [Authorize(Roles = AppRoles.Administrateur)]
        public async Task<IActionResult> Edit(int id)
        {
            var etudiant = await _context.Students.FindAsync(id);
            if (etudiant == null)
            {
                return NotFound();
            }

            ViewBag.Classes = new SelectList(_context.ClassRooms.OrderBy(c => c.Nom), "Id", "Nom", etudiant.ClassRoomId);
            return View(etudiant);
        }

        // POST: /Students/Edit/5
        [HttpPost]
        [Authorize(Roles = AppRoles.Administrateur)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Student student)
        {
            if (id != student.Id)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Classes = new SelectList(_context.ClassRooms.OrderBy(c => c.Nom), "Id", "Nom", student.ClassRoomId);
                return View(student);
            }

            // On récupère l'étudiant existant (avec son UserId) plutôt que d'écraser toute la ligne :
            // le formulaire ne connaît pas le UserId, donc un Update(student) direct l'aurait remis à null.
            var etudiantExistant = await _context.Students.FindAsync(id);
            if (etudiantExistant == null)
            {
                return NotFound();
            }

            bool emailAChange = etudiantExistant.Email != student.Email;

            etudiantExistant.Nom = student.Nom;
            etudiantExistant.Prenom = student.Prenom;
            etudiantExistant.DateNaissance = student.DateNaissance;
            etudiantExistant.Email = student.Email;
            etudiantExistant.ClassRoomId = student.ClassRoomId;

            await _context.SaveChangesAsync();

            // Si l'email a changé, on met aussi à jour le compte de connexion de l'étudiant.
            if (emailAChange && etudiantExistant.UserId != null)
            {
                var compte = await _userManager.FindByIdAsync(etudiantExistant.UserId);
                if (compte != null)
                {
                    compte.Email = student.Email;
                    compte.UserName = student.Email;
                    await _userManager.UpdateAsync(compte);
                }
            }

            TempData["Message"] = "Étudiant modifié avec succès.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Students/Delete/5
        [Authorize(Roles = AppRoles.Administrateur)]
        public async Task<IActionResult> Delete(int id)
        {
            var etudiant = await _context.Students
                .Include(e => e.ClassRoom)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (etudiant == null)
            {
                return NotFound();
            }

            return View(etudiant);
        }

        // POST: /Students/Delete/5
        // Supprime aussi le compte de connexion de l'étudiant, s'il en avait un.
        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = AppRoles.Administrateur)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var etudiant = await _context.Students.FindAsync(id);
            if (etudiant != null)
            {
                string? userId = etudiant.UserId;

                _context.Students.Remove(etudiant);
                await _context.SaveChangesAsync();

                if (userId != null)
                {
                    var compte = await _userManager.FindByIdAsync(userId);
                    if (compte != null)
                    {
                        await _userManager.DeleteAsync(compte);
                    }
                }

                TempData["Message"] = "Étudiant supprimé avec succès.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
