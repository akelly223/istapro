using GestionScolaire.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace GestionScolaire.Data
{
    // AppDbContext fait le lien entre nos classes C# et les tables de la base de données.
    // On hérite de IdentityDbContext pour obtenir gratuitement les tables de connexion
    // (AspNetUsers, AspNetRoles, ...) fournies par ASP.NET Identity.
    public class AppDbContext : IdentityDbContext<IdentityUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // Chaque DbSet correspond à une table dans la base de données.
        public DbSet<ClassRoom> ClassRooms { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<Subject> Subjects { get; set; }
        public DbSet<Grade> Grades { get; set; }
    }
}
