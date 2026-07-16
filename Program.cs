using System.Globalization;
using GestionScolaire.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Connexion à la base de données SQLite (fichier GestionScolaire.db) via Entity Framework Core.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Mise en place d'ASP.NET Identity pour gérer les comptes (Administrateur, Professeur, Etudiant).
// Règles de mot de passe simplifiées : les comptes étudiants ont pour mot de passe
// leur date de naissance (8 chiffres), donc pas besoin de majuscule/symbole obligatoire.
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
    options.Password.RequiredLength = 6;
})
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

// On indique à Identity où se trouvent nos pages de connexion et d'accès refusé, puisqu'on
// utilise notre propre AccountController et non les pages Identity par défaut.
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

var app = builder.Build();

// Création automatique des rôles et des comptes de démonstration au premier démarrage.
// Il y a 3 rôles : Administrateur (gère tout), Professeur (notes, devoirs, présences)
// et Etudiant (un compte par étudiant, créé automatiquement par StudentsController).
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

    foreach (var role in new[] { AppRoles.Administrateur, AppRoles.Professeur, AppRoles.Etudiant })
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    const string adminEmail = "admin@ecole.com";
    const string adminPassword = "Admin123";
    var admin = await userManager.FindByEmailAsync(adminEmail);
    if (admin is null)
    {
        admin = new IdentityUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true };
        await userManager.CreateAsync(admin, adminPassword);
    }
    if (!await userManager.IsInRoleAsync(admin, AppRoles.Administrateur))
    {
        await userManager.AddToRoleAsync(admin, AppRoles.Administrateur);
    }

    const string profEmail = "prof@ecole.com";
    const string profPassword = "Prof123";
    var professeur = await userManager.FindByEmailAsync(profEmail);
    if (professeur is null)
    {
        professeur = new IdentityUser { UserName = profEmail, Email = profEmail, EmailConfirmed = true };
        await userManager.CreateAsync(professeur, profPassword);
    }
    if (!await userManager.IsInRoleAsync(professeur, AppRoles.Professeur))
    {
        await userManager.AddToRoleAsync(professeur, AppRoles.Professeur);
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// Force le format "16.5" (point) pour les nombres décimaux dans toute l'application,
// quelle que soit la langue configurée sur la machine (certains PC en français utilisent
// la virgule par défaut, ce qui ferait échouer la conversion des notes/coefficients).
var culture = new CultureInfo("en-US");
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(culture),
    SupportedCultures = new[] { culture },
    SupportedUICultures = new[] { culture }
});

app.UseHttpsRedirection();
app.UseRouting();

// UseAuthentication() doit être appelé avant UseAuthorization() :
// on identifie d'abord qui est l'utilisateur, puis on vérifie ce qu'il a le droit de faire.
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
