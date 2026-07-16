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

// Mise en place d'ASP.NET Identity pour gérer le compte administrateur (connexion, mot de passe, cookie).
// Règles de mot de passe simplifiées car il n'y a qu'un seul compte administrateur pour ce projet.
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 6;
})
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

// On indique à Identity où se trouve notre page de connexion, puisqu'on utilise
// notre propre AccountController et non les pages Identity par défaut.
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/Login";
});

var app = builder.Build();

// Création automatique du compte administrateur au premier démarrage, s'il n'existe pas déjà.
// C'est le seul compte de l'application : il n'y a pas de page d'inscription.
using (var scope = app.Services.CreateScope())
{
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

    const string adminEmail = "admin@ecole.com";
    const string adminPassword = "Admin123";

    if (await userManager.FindByEmailAsync(adminEmail) is null)
    {
        var admin = new IdentityUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true
        };
        await userManager.CreateAsync(admin, adminPassword);
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
