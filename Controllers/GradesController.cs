using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GestionScolaire.Data;
using GestionScolaire.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace GestionScolaire.Controllers
{
    // L'administrateur et le professeur peuvent tous les deux gérer les notes.
    // Les étudiants consultent les leurs uniquement depuis leur propre espace (Home/MonEspace).
    [Authorize(Roles = AppRoles.Administrateur + "," + AppRoles.Professeur)]
    public class GradesController : Controller
    {
        private readonly AppDbContext _context;

        public GradesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Grades
        // GET: /Grades?studentId=3
        // Liste toutes les notes, ou seulement celles d'un étudiant si studentId est fourni.
        // C'est ce même filtre qui sert à la fois pour "Liste des notes" et
        // "Consultation des notes d'un étudiant".
        public async Task<IActionResult> Index(int? studentId)
        {
            var requete = _context.Grades
                .Include(n => n.Student)
                .Include(n => n.Subject)
                .AsQueryable();

            if (studentId.HasValue)
            {
                requete = requete.Where(n => n.StudentId == studentId.Value);
            }

            var notes = await requete
                .OrderByDescending(n => n.DateEvaluation)
                .ToListAsync();

            ViewBag.Etudiants = new SelectList(await ListeEtudiantsPourMenu(), "Id", "NomComplet", studentId);
            ViewBag.StudentIdFiltre = studentId;

            // Si on regarde les notes d'un seul étudiant, on calcule sa moyenne pondérée
            // (chaque note compte selon le coefficient de sa matière).
            if (studentId.HasValue && notes.Any())
            {
                double sommePonderee = notes.Sum(n => n.Valeur * n.Subject!.Coefficient);
                double sommeCoefficients = notes.Sum(n => n.Subject!.Coefficient);
                ViewBag.Moyenne = sommeCoefficients > 0 ? sommePonderee / sommeCoefficients : (double?)null;
            }

            return View(notes);
        }

        // GET: /Grades/Create
        public async Task<IActionResult> Create()
        {
            await ChargerListesDeroulantes();
            return View();
        }

        // POST: /Grades/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Grade grade)
        {
            if (!ModelState.IsValid)
            {
                await ChargerListesDeroulantes(grade.StudentId, grade.SubjectId);
                return View(grade);
            }

            _context.Grades.Add(grade);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Note ajoutée avec succès.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Grades/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var note = await _context.Grades.FindAsync(id);
            if (note == null)
            {
                return NotFound();
            }

            await ChargerListesDeroulantes(note.StudentId, note.SubjectId);
            return View(note);
        }

        // POST: /Grades/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Grade grade)
        {
            if (id != grade.Id)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                await ChargerListesDeroulantes(grade.StudentId, grade.SubjectId);
                return View(grade);
            }

            _context.Grades.Update(grade);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Note modifiée avec succès.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Grades/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var note = await _context.Grades
                .Include(n => n.Student)
                .Include(n => n.Subject)
                .FirstOrDefaultAsync(n => n.Id == id);

            if (note == null)
            {
                return NotFound();
            }

            return View(note);
        }

        // POST: /Grades/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var note = await _context.Grades.FindAsync(id);
            if (note != null)
            {
                _context.Grades.Remove(note);
                await _context.SaveChangesAsync();
                TempData["Message"] = "Note supprimée avec succès.";
            }

            return RedirectToAction(nameof(Index));
        }

        // Prépare les listes déroulantes (étudiants et matières) utilisées par Create et Edit.
        private async Task ChargerListesDeroulantes(int? studentId = null, int? subjectId = null)
        {
            ViewBag.Etudiants = new SelectList(await ListeEtudiantsPourMenu(), "Id", "NomComplet", studentId);

            ViewBag.Matieres = new SelectList(
                await _context.Subjects.OrderBy(m => m.Nom).ToListAsync(),
                "Id", "Nom", subjectId);
        }

        // Construit la liste "Nom Prénom" utilisée dans les menus déroulants,
        // sans avoir besoin d'ajouter de propriété au modèle Student.
        private async Task<List<object>> ListeEtudiantsPourMenu()
        {
            return (await _context.Students
                .OrderBy(e => e.Nom)
                .Select(e => new { e.Id, NomComplet = e.Nom + " " + e.Prenom })
                .ToListAsync())
                .Cast<object>()
                .ToList();
        }
    }
}
