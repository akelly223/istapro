using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using GestionScolaire.Data;
using GestionScolaire.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionScolaire.Controllers;

public class HomeController : Controller
{
    private readonly AppDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;

    public HomeController(AppDbContext context, UserManager<IdentityUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // Page d'accueil (accessible sans connexion).
    public IActionResult Index()
    {
        return View();
    }

    // Tableau de bord : réservé à l'administrateur connecté.
    [Authorize(Roles = AppRoles.Administrateur)]
    public IActionResult Dashboard()
    {
        ViewBag.NombreEtudiants = _context.Students.Count();
        ViewBag.NombreClasses = _context.ClassRooms.Count();
        ViewBag.NombreMatieres = _context.Subjects.Count();
        ViewBag.NombreNotes = _context.Grades.Count();

        return View();
    }

    // Espace du professeur : accès rapide à ses outils (notes, devoirs, présences).
    [Authorize(Roles = AppRoles.Professeur)]
    public IActionResult EspaceProfesseur()
    {
        ViewBag.NombreDevoirs = _context.Homeworks.Count();
        ViewBag.NombreRendusAujourdhui = _context.HomeworkSubmissions.Count(s => s.DateEnvoi.Date == System.DateTime.Today);

        return View();
    }

    // Espace de l'étudiant connecté : son profil, ses notes/moyenne et ses devoirs.
    [Authorize(Roles = AppRoles.Etudiant)]
    public async Task<IActionResult> MonEspace()
    {
        string? userId = _userManager.GetUserId(User);
        var etudiant = await _context.Students
            .Include(e => e.ClassRoom)
            .FirstOrDefaultAsync(e => e.UserId == userId);

        if (etudiant == null)
        {
            return Forbid();
        }

        var notes = await _context.Grades
            .Include(n => n.Subject)
            .Where(n => n.StudentId == etudiant.Id)
            .OrderByDescending(n => n.DateEvaluation)
            .ToListAsync();

        if (notes.Any())
        {
            double sommePonderee = notes.Sum(n => n.Valeur * n.Subject!.Coefficient);
            double sommeCoefficients = notes.Sum(n => n.Subject!.Coefficient);
            ViewBag.Moyenne = sommeCoefficients > 0 ? sommePonderee / sommeCoefficients : (double?)null;
        }

        int nombreDevoirsClasse = await _context.Homeworks.CountAsync(d => d.ClassRoomId == etudiant.ClassRoomId);
        int nombreDevoirsRendus = await _context.HomeworkSubmissions.CountAsync(s => s.StudentId == etudiant.Id);

        ViewBag.NombreDevoirsClasse = nombreDevoirsClasse;
        ViewBag.NombreDevoirsRendus = nombreDevoirsRendus;
        ViewBag.Notes = notes;

        return View(etudiant);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
