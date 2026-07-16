using System;
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
    // Prise de présence : réservée à l'administrateur et au professeur.
    [Authorize(Roles = AppRoles.Administrateur + "," + AppRoles.Professeur)]
    public class AttendancesController : Controller
    {
        private readonly AppDbContext _context;

        public AttendancesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Attendances?classRoomId=1&date=2026-05-20
        // Étape 1 : choisir une classe et une date.
        // Étape 2 (si les deux sont choisis) : cocher les étudiants présents et enregistrer.
        public async Task<IActionResult> Index(int? classRoomId, DateTime? date)
        {
            ViewBag.Classes = new SelectList(await _context.ClassRooms.OrderBy(c => c.Nom).ToListAsync(), "Id", "Nom", classRoomId);
            ViewBag.ClassRoomId = classRoomId;
            ViewBag.Date = (date ?? DateTime.Today).ToString("yyyy-MM-dd");

            if (!classRoomId.HasValue)
            {
                return View(new List<Student>());
            }

            DateTime dateChoisie = (date ?? DateTime.Today).Date;

            var etudiants = await _context.Students
                .Where(e => e.ClassRoomId == classRoomId.Value)
                .OrderBy(e => e.Nom)
                .ToListAsync();

            var presencesExistantes = await _context.Attendances
                .Where(p => p.DateSeance == dateChoisie && etudiants.Select(e => e.Id).Contains(p.StudentId))
                .ToListAsync();

            ViewBag.PresencesExistantes = presencesExistantes.ToDictionary(p => p.StudentId, p => p.EstPresent);

            return View(etudiants);
        }

        // POST: /Attendances/Enregistrer
        // "presents" contient les Id des étudiants cochés comme présents ; tous les autres
        // étudiants de la classe sont enregistrés comme absents pour cette date.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Enregistrer(int classRoomId, DateTime date, List<int>? presents)
        {
            presents ??= new List<int>();
            DateTime dateChoisie = date.Date;

            var etudiants = await _context.Students
                .Where(e => e.ClassRoomId == classRoomId)
                .ToListAsync();

            var presencesExistantes = await _context.Attendances
                .Where(p => p.DateSeance == dateChoisie && etudiants.Select(e => e.Id).Contains(p.StudentId))
                .ToListAsync();

            foreach (var etudiant in etudiants)
            {
                bool estPresent = presents.Contains(etudiant.Id);
                var presenceExistante = presencesExistantes.FirstOrDefault(p => p.StudentId == etudiant.Id);

                if (presenceExistante != null)
                {
                    presenceExistante.EstPresent = estPresent;
                }
                else
                {
                    _context.Attendances.Add(new Attendance
                    {
                        StudentId = etudiant.Id,
                        DateSeance = dateChoisie,
                        EstPresent = estPresent
                    });
                }
            }

            await _context.SaveChangesAsync();

            TempData["Message"] = "Présences enregistrées avec succès.";
            return RedirectToAction(nameof(Index), new { classRoomId, date = dateChoisie.ToString("yyyy-MM-dd") });
        }
    }
}
