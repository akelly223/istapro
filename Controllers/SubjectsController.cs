using System.Linq;
using System.Threading.Tasks;
using GestionScolaire.Data;
using GestionScolaire.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionScolaire.Controllers
{
    [Authorize]
    public class SubjectsController : Controller
    {
        private readonly AppDbContext _context;

        public SubjectsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Subjects
        public async Task<IActionResult> Index()
        {
            var matieres = await _context.Subjects
                .OrderBy(m => m.Nom)
                .ToListAsync();

            return View(matieres);
        }

        // GET: /Subjects/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Subjects/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Subject subject)
        {
            if (!ModelState.IsValid)
            {
                return View(subject);
            }

            _context.Subjects.Add(subject);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Matière ajoutée avec succès.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Subjects/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var matiere = await _context.Subjects.FindAsync(id);
            if (matiere == null)
            {
                return NotFound();
            }

            return View(matiere);
        }

        // POST: /Subjects/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Subject subject)
        {
            if (id != subject.Id)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(subject);
            }

            _context.Subjects.Update(subject);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Matière modifiée avec succès.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Subjects/Delete/5
        // On indique combien de notes existantes seront supprimées par cascade avec la matière.
        public async Task<IActionResult> Delete(int id)
        {
            var matiere = await _context.Subjects.FirstOrDefaultAsync(m => m.Id == id);
            if (matiere == null)
            {
                return NotFound();
            }

            ViewBag.NombreNotes = await _context.Grades.CountAsync(g => g.SubjectId == id);
            return View(matiere);
        }

        // POST: /Subjects/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var matiere = await _context.Subjects.FindAsync(id);
            if (matiere != null)
            {
                _context.Subjects.Remove(matiere);
                await _context.SaveChangesAsync();
                TempData["Message"] = "Matière supprimée avec succès.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
