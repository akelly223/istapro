using System.Diagnostics;
using System.Linq;
using GestionScolaire.Data;
using GestionScolaire.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionScolaire.Controllers;

public class HomeController : Controller
{
    private readonly AppDbContext _context;

    public HomeController(AppDbContext context)
    {
        _context = context;
    }

    // Page d'accueil (accessible sans connexion).
    public IActionResult Index()
    {
        return View();
    }

    // Tableau de bord : réservé à l'administrateur connecté.
    // [Authorize] redirige automatiquement vers /Account/Login si l'utilisateur n'est pas connecté.
    [Authorize]
    public IActionResult Dashboard()
    {
        ViewBag.NombreEtudiants = _context.Students.Count();
        ViewBag.NombreClasses = _context.ClassRooms.Count();
        ViewBag.NombreMatieres = _context.Subjects.Count();
        ViewBag.NombreNotes = _context.Grades.Count();

        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
