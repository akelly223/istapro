using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GestionScolaire.Data;
using GestionScolaire.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace GestionScolaire.Controllers
{
    // Deux publics très différents dans ce même contrôleur :
    // - Administrateur/Professeur : assignent les devoirs et consultent les rendus.
    // - Etudiant : consulte les devoirs de sa classe et envoie son propre rendu.
    [Authorize]
    public class HomeworksController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IWebHostEnvironment _environment;

        public HomeworksController(AppDbContext context, UserManager<IdentityUser> userManager, IWebHostEnvironment environment)
        {
            _context = context;
            _userManager = userManager;
            _environment = environment;
        }

        // ================= Espace Administrateur / Professeur =================

        // GET: /Homeworks
        [Authorize(Roles = AppRoles.Administrateur + "," + AppRoles.Professeur)]
        public async Task<IActionResult> Index()
        {
            var devoirs = await _context.Homeworks
                .Include(d => d.Subject)
                .Include(d => d.ClassRoom)
                .Include(d => d.Soumissions)
                .OrderByDescending(d => d.DateLimite)
                .ToListAsync();

            return View(devoirs);
        }

        // GET: /Homeworks/Create
        [Authorize(Roles = AppRoles.Administrateur + "," + AppRoles.Professeur)]
        public async Task<IActionResult> Create()
        {
            await ChargerListesDeroulantes();
            return View();
        }

        // POST: /Homeworks/Create
        [HttpPost]
        [Authorize(Roles = AppRoles.Administrateur + "," + AppRoles.Professeur)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Homework devoir)
        {
            if (!ModelState.IsValid)
            {
                await ChargerListesDeroulantes(devoir.SubjectId, devoir.ClassRoomId);
                return View(devoir);
            }

            _context.Homeworks.Add(devoir);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Devoir assigné avec succès.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Homeworks/Edit/5
        [Authorize(Roles = AppRoles.Administrateur + "," + AppRoles.Professeur)]
        public async Task<IActionResult> Edit(int id)
        {
            var devoir = await _context.Homeworks.FindAsync(id);
            if (devoir == null)
            {
                return NotFound();
            }

            await ChargerListesDeroulantes(devoir.SubjectId, devoir.ClassRoomId);
            return View(devoir);
        }

        // POST: /Homeworks/Edit/5
        [HttpPost]
        [Authorize(Roles = AppRoles.Administrateur + "," + AppRoles.Professeur)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Homework devoir)
        {
            if (id != devoir.Id)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                await ChargerListesDeroulantes(devoir.SubjectId, devoir.ClassRoomId);
                return View(devoir);
            }

            var devoirExistant = await _context.Homeworks.FindAsync(id);
            if (devoirExistant == null)
            {
                return NotFound();
            }

            devoirExistant.Titre = devoir.Titre;
            devoirExistant.Description = devoir.Description;
            devoirExistant.SubjectId = devoir.SubjectId;
            devoirExistant.ClassRoomId = devoir.ClassRoomId;
            devoirExistant.DateLimite = devoir.DateLimite;

            await _context.SaveChangesAsync();

            TempData["Message"] = "Devoir modifié avec succès.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Homeworks/Delete/5
        [Authorize(Roles = AppRoles.Administrateur + "," + AppRoles.Professeur)]
        public async Task<IActionResult> Delete(int id)
        {
            var devoir = await _context.Homeworks
                .Include(d => d.Subject)
                .Include(d => d.ClassRoom)
                .Include(d => d.Soumissions)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (devoir == null)
            {
                return NotFound();
            }

            return View(devoir);
        }

        // POST: /Homeworks/Delete/5
        // Supprime aussi les fichiers physiques des rendus déjà envoyés pour ce devoir.
        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = AppRoles.Administrateur + "," + AppRoles.Professeur)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var devoir = await _context.Homeworks
                .Include(d => d.Soumissions)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (devoir != null)
            {
                if (devoir.Soumissions != null)
                {
                    foreach (var soumission in devoir.Soumissions)
                    {
                        SupprimerFichierPhysique(soumission.NomFichier);
                    }
                }

                _context.Homeworks.Remove(devoir);
                await _context.SaveChangesAsync();
                TempData["Message"] = "Devoir supprimé avec succès.";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: /Homeworks/Rendus/5
        // Liste les étudiants qui ont déjà envoyé leur devoir (avec lien de téléchargement)
        // et ceux qui n'ont pas encore rendu.
        [Authorize(Roles = AppRoles.Administrateur + "," + AppRoles.Professeur)]
        public async Task<IActionResult> Rendus(int id)
        {
            var devoir = await _context.Homeworks
                .Include(d => d.Subject)
                .Include(d => d.ClassRoom)
                .Include(d => d.Soumissions!).ThenInclude(s => s.Student)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (devoir == null)
            {
                return NotFound();
            }

            var idsEtudiantsAyantRendu = devoir.Soumissions?.Select(s => s.StudentId).ToList() ?? new List<int>();

            ViewBag.EtudiantsSansRendu = await _context.Students
                .Where(e => e.ClassRoomId == devoir.ClassRoomId && !idsEtudiantsAyantRendu.Contains(e.Id))
                .OrderBy(e => e.Nom)
                .ToListAsync();

            return View(devoir);
        }

        // ================= Espace Étudiant =================

        // GET: /Homeworks/MesDevoirs
        // Liste les devoirs assignés à la classe de l'étudiant connecté, avec son statut d'envoi.
        [Authorize(Roles = AppRoles.Etudiant)]
        public async Task<IActionResult> MesDevoirs()
        {
            var etudiant = await ObtenirEtudiantConnecteAsync();
            if (etudiant == null)
            {
                return Forbid();
            }

            var devoirs = await _context.Homeworks
                .Include(d => d.Subject)
                .Where(d => d.ClassRoomId == etudiant.ClassRoomId)
                .OrderBy(d => d.DateLimite)
                .ToListAsync();

            var mesSoumissions = await _context.HomeworkSubmissions
                .Where(s => s.StudentId == etudiant.Id)
                .ToListAsync();

            ViewBag.MesSoumissions = mesSoumissions.ToDictionary(s => s.HomeworkId);

            return View(devoirs);
        }

        // GET: /Homeworks/Soumettre/5
        [Authorize(Roles = AppRoles.Etudiant)]
        public async Task<IActionResult> Soumettre(int id)
        {
            var etudiant = await ObtenirEtudiantConnecteAsync();
            if (etudiant == null)
            {
                return Forbid();
            }

            var devoir = await _context.Homeworks
                .Include(d => d.Subject)
                .FirstOrDefaultAsync(d => d.Id == id && d.ClassRoomId == etudiant.ClassRoomId);

            if (devoir == null)
            {
                return NotFound();
            }

            ViewBag.SoumissionExistante = await _context.HomeworkSubmissions
                .FirstOrDefaultAsync(s => s.HomeworkId == id && s.StudentId == etudiant.Id);

            return View(devoir);
        }

        // POST: /Homeworks/Soumettre/5
        // Enregistre le fichier envoyé par l'étudiant. Si un rendu existait déjà pour ce devoir,
        // l'ancien fichier est remplacé (l'étudiant peut donc corriger son envoi).
        [HttpPost]
        [Authorize(Roles = AppRoles.Etudiant)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Soumettre(int id, IFormFile fichier)
        {
            var etudiant = await ObtenirEtudiantConnecteAsync();
            if (etudiant == null)
            {
                return Forbid();
            }

            var devoir = await _context.Homeworks
                .Include(d => d.Subject)
                .FirstOrDefaultAsync(d => d.Id == id && d.ClassRoomId == etudiant.ClassRoomId);

            if (devoir == null)
            {
                return NotFound();
            }

            if (fichier == null || fichier.Length == 0)
            {
                ModelState.AddModelError(string.Empty, "Merci de choisir un fichier avant d'envoyer.");
                ViewBag.SoumissionExistante = await _context.HomeworkSubmissions
                    .FirstOrDefaultAsync(s => s.HomeworkId == id && s.StudentId == etudiant.Id);
                return View(devoir);
            }

            string dossierUploads = Path.Combine(_environment.WebRootPath, "uploads", "devoirs");
            Directory.CreateDirectory(dossierUploads);

            string nomFichierUnique = $"{Guid.NewGuid()}{Path.GetExtension(fichier.FileName)}";
            string cheminComplet = Path.Combine(dossierUploads, nomFichierUnique);

            using (var flux = new FileStream(cheminComplet, FileMode.Create))
            {
                await fichier.CopyToAsync(flux);
            }

            var soumissionExistante = await _context.HomeworkSubmissions
                .FirstOrDefaultAsync(s => s.HomeworkId == id && s.StudentId == etudiant.Id);

            if (soumissionExistante != null)
            {
                SupprimerFichierPhysique(soumissionExistante.NomFichier);
                soumissionExistante.NomFichier = nomFichierUnique;
                soumissionExistante.NomFichierOriginal = fichier.FileName;
                soumissionExistante.DateEnvoi = DateTime.Now;
            }
            else
            {
                _context.HomeworkSubmissions.Add(new HomeworkSubmission
                {
                    HomeworkId = id,
                    StudentId = etudiant.Id,
                    NomFichier = nomFichierUnique,
                    NomFichierOriginal = fichier.FileName
                });
            }

            await _context.SaveChangesAsync();

            TempData["Message"] = "Devoir envoyé avec succès.";
            return RedirectToAction(nameof(MesDevoirs));
        }

        // GET: /Homeworks/Telecharger/5
        // Accessible à l'administrateur et au professeur pour tous les rendus,
        // et à l'étudiant uniquement pour son propre rendu.
        [Authorize]
        public async Task<IActionResult> Telecharger(int id)
        {
            var soumission = await _context.HomeworkSubmissions.FindAsync(id);
            if (soumission == null)
            {
                return NotFound();
            }

            if (User.IsInRole(AppRoles.Etudiant))
            {
                var etudiant = await ObtenirEtudiantConnecteAsync();
                if (etudiant == null || soumission.StudentId != etudiant.Id)
                {
                    return Forbid();
                }
            }

            string chemin = Path.Combine(_environment.WebRootPath, "uploads", "devoirs", soumission.NomFichier);
            if (!System.IO.File.Exists(chemin))
            {
                return NotFound();
            }

            byte[] octets = await System.IO.File.ReadAllBytesAsync(chemin);
            return File(octets, "application/octet-stream", soumission.NomFichierOriginal);
        }

        // ================= Méthodes privées =================

        private async Task ChargerListesDeroulantes(int? subjectId = null, int? classRoomId = null)
        {
            ViewBag.Matieres = new SelectList(await _context.Subjects.OrderBy(m => m.Nom).ToListAsync(), "Id", "Nom", subjectId);
            ViewBag.Classes = new SelectList(await _context.ClassRooms.OrderBy(c => c.Nom).ToListAsync(), "Id", "Nom", classRoomId);
        }

        // Retrouve la fiche Student liée au compte actuellement connecté.
        private async Task<Student?> ObtenirEtudiantConnecteAsync()
        {
            string? userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                return null;
            }

            return await _context.Students.FirstOrDefaultAsync(e => e.UserId == userId);
        }

        private void SupprimerFichierPhysique(string nomFichier)
        {
            string chemin = Path.Combine(_environment.WebRootPath, "uploads", "devoirs", nomFichier);
            if (System.IO.File.Exists(chemin))
            {
                System.IO.File.Delete(chemin);
            }
        }
    }
}
