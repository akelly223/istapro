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
    public class ClassRoomsController : Controller
    {
        private readonly AppDbContext _context;

        public ClassRoomsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /ClassRooms
        // Affiche la liste des classes avec le nombre d'étudiants de chacune.
        public async Task<IActionResult> Index()
        {
            var classes = await _context.ClassRooms
                .Include(c => c.Students)
                .OrderBy(c => c.Nom)
                .ToListAsync();

            return View(classes);
        }

        // GET: /ClassRooms/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: /ClassRooms/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ClassRoom classRoom)
        {
            if (!ModelState.IsValid)
            {
                return View(classRoom);
            }

            _context.ClassRooms.Add(classRoom);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Classe ajoutée avec succès.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /ClassRooms/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var classe = await _context.ClassRooms.FindAsync(id);
            if (classe == null)
            {
                return NotFound();
            }

            return View(classe);
        }

        // POST: /ClassRooms/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ClassRoom classRoom)
        {
            if (id != classRoom.Id)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(classRoom);
            }

            _context.ClassRooms.Update(classRoom);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Classe modifiée avec succès.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /ClassRooms/Delete/5
        // On affiche le nombre d'étudiants concernés, car la suppression de la classe
        // supprime aussi ses étudiants (et leurs notes) par cascade.
        public async Task<IActionResult> Delete(int id)
        {
            var classe = await _context.ClassRooms
                .Include(c => c.Students)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (classe == null)
            {
                return NotFound();
            }

            return View(classe);
        }

        // POST: /ClassRooms/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var classe = await _context.ClassRooms.FindAsync(id);
            if (classe != null)
            {
                _context.ClassRooms.Remove(classe);
                await _context.SaveChangesAsync();
                TempData["Message"] = "Classe supprimée avec succès.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
