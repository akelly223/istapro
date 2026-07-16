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
    // [Authorize] sur le contrôleur entier : toutes les actions ci-dessous
    // nécessitent d'être connecté (voir étape 2 - authentification).
    [Authorize]
    public class StudentsController : Controller
    {
        private readonly AppDbContext _context;

        public StudentsController(AppDbContext context)
        {
            _context = context;
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
        public IActionResult Create()
        {
            ViewBag.Classes = new SelectList(_context.ClassRooms.OrderBy(c => c.Nom), "Id", "Nom");
            return View();
        }

        // POST: /Students/Create
        [HttpPost]
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

            TempData["Message"] = "Étudiant ajouté avec succès.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Students/Edit/5
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

            _context.Students.Update(student);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Étudiant modifié avec succès.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Students/Delete/5
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
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var etudiant = await _context.Students.FindAsync(id);
            if (etudiant != null)
            {
                _context.Students.Remove(etudiant);
                await _context.SaveChangesAsync();
                TempData["Message"] = "Étudiant supprimé avec succès.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
