# Fiche d'explication — Kelly — Étape 2 : Authentification

## 1. Objectif de l'étape

Mettre en place la connexion/déconnexion de l'administrateur avec ASP.NET Identity, protéger le tableau de bord, et adapter le layout pour qu'il change d'apparence selon que l'utilisateur est connecté ou non.

## 2. Fichiers créés

| Fichier | Rôle |
|---|---|
| `Models/LoginViewModel.cs` | Transporte les données du formulaire de connexion |
| `Controllers/AccountController.cs` | Gère `Login` (GET/POST) et `Logout` |
| `Views/Account/Login.cshtml` | Formulaire de connexion moderne |
| `Views/Home/Dashboard.cshtml` | Tableau de bord protégé avec statistiques |

## 3. Fichiers modifiés

| Fichier | Modification |
|---|---|
| `Program.cs` | `ConfigureApplicationCookie` + création automatique du compte admin |
| `Controllers/HomeController.cs` | Ajout de l'action `Dashboard` protégée par `[Authorize]` |
| `Views/Shared/_Layout.cshtml` | Sidebar/navbar conditionnelles selon la connexion, logout en formulaire POST |
| `wwwroot/css/custom.css` | Styles des messages de validation |
| `Views/Home/Index.cshtml` | Mise à jour du message d'information |

## 4. Explication ligne par ligne

### 4.1 `Models/LoginViewModel.cs`

```csharp
public class LoginViewModel
{
    [Required(ErrorMessage = "L'email est obligatoire.")]
    [EmailAddress(ErrorMessage = "Le format de l'email n'est pas valide.")]
    [Display(Name = "Adresse email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Le mot de passe est obligatoire.")]
    [DataType(DataType.Password)]
    [Display(Name = "Mot de passe")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Se souvenir de moi")]
    public bool RememberMe { get; set; }
}
```

C'est un **ViewModel**, pas un modèle de base de données : il n'y a pas de `DbSet<LoginViewModel>` dans `AppDbContext`. Il sert uniquement à recevoir proprement les données tapées dans le formulaire HTML, avec de la validation automatique :
- `[Required]` : le champ ne peut pas être vide. Le message s'affiche via `asp-validation-for` dans la vue.
- `[EmailAddress]` : vérifie que le texte ressemble à un email (contient un `@`, etc.).
- `[DataType(DataType.Password)]` : indique à Razor de générer un `<input type="password">` (les caractères tapés sont masqués).
- `[Display(Name = "...")]` : le texte utilisé pour le `<label>` généré par `asp-for`.

### 4.2 `Controllers/AccountController.cs`

```csharp
private readonly SignInManager<IdentityUser> _signInManager;

public AccountController(SignInManager<IdentityUser> signInManager)
{
    _signInManager = signInManager;
}
```
`SignInManager<IdentityUser>` est fourni automatiquement par ASP.NET Identity (on l'a enregistré dans `Program.cs` avec `AddIdentity`). Le contrôleur le reçoit par **injection de dépendances** : on n'a jamais à écrire `new SignInManager(...)` nous-mêmes.

```csharp
[HttpGet]
public IActionResult Login(string? returnUrl = null)
{
    ViewData["ReturnUrl"] = returnUrl;
    return View(new LoginViewModel());
}
```
Quand on **affiche** la page de connexion (GET), on transmet un `LoginViewModel` vide à la vue. Le `returnUrl` retient la page que l'utilisateur voulait visiter avant d'être redirigé vers le login (ex: il a cliqué sur "Étudiants" sans être connecté).

```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
{
    ViewData["ReturnUrl"] = returnUrl;

    if (!ModelState.IsValid)
    {
        return View(model);
    }

    var result = await _signInManager.PasswordSignInAsync(
        model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);

    if (result.Succeeded)
    {
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }
        return RedirectToAction("Dashboard", "Home");
    }

    ModelState.AddModelError(string.Empty, "Email ou mot de passe incorrect.");
    return View(model);
}
```
- `[ValidateAntiForgeryToken]` : protège contre les attaques CSRF (un autre site qui essaierait de soumettre ce formulaire à notre place). Le token est généré automatiquement par la balise `<form>` de Razor et vérifié ici.
- `ModelState.IsValid` : vérifie que toutes les règles `[Required]`, `[EmailAddress]`, etc. sont respectées **avant** même de toucher à la base de données.
- `PasswordSignInAsync(...)` : c'est **la** ligne clé. Identity va chercher l'utilisateur par email, vérifie que le mot de passe (haché) correspond, et si tout est bon, **pose un cookie de connexion** dans le navigateur. C'est ce cookie qui permettra à `[Authorize]` de reconnaître l'utilisateur sur les pages suivantes.
- `Url.IsLocalUrl(returnUrl)` : vérification de sécurité importante. Sans ça, quelqu'un pourrait construire un lien du type `/Account/Login?returnUrl=https://site-pirate.com` et rediriger l'utilisateur vers un site externe après connexion.
- Si la connexion échoue, `ModelState.AddModelError(string.Empty, ...)` ajoute une erreur "générale" (pas liée à un champ précis) : c'est ce qui s'affiche dans `asp-validation-summary`. On ne dit jamais "l'email n'existe pas" ou "le mot de passe est faux" séparément, pour ne pas donner d'indice à quelqu'un qui essaierait de deviner un compte.

```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Logout()
{
    await _signInManager.SignOutAsync();
    return RedirectToAction("Index", "Home");
}
```
`SignOutAsync()` supprime le cookie d'authentification. **C'est en POST, pas en GET** : se déconnecter est une action qui modifie l'état de la session, ce n'est donc pas une simple consultation. Un lien `<a>` classique aurait fait un GET ; on utilise donc un `<form method="post">` avec un bouton, comme on le voit dans `_Layout.cshtml`.

### 4.3 `Program.cs` — le compte administrateur

```csharp
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/Login";
});
```
Par défaut, Identity redirige vers `/Identity/Account/Login` (une page fournie par le paquet "Identity UI" qu'on n'utilise pas ici, puisqu'on a écrit notre propre `AccountController`). Cette ligne dit à Identity : « en cas de besoin de connexion, envoie plutôt vers **notre** page ».

```csharp
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
```
Il n'y a pas de page d'inscription dans cette application (un seul administrateur). Ce bloc s'exécute **une seule fois, à chaque démarrage de l'application** : il vérifie si le compte `admin@ecole.com` existe déjà, et le crée sinon.
- `app.Services.CreateScope()` : nécessaire car `UserManager` est un service "scoped" (conçu pour vivre le temps d'une requête HTTP). Comme on est en dehors d'une requête (au démarrage), on doit créer un scope manuellement pour pouvoir l'utiliser.
- `UserManager<IdentityUser>` : gère la création/modification des comptes (contrairement à `SignInManager` qui gère la connexion).
- **Identifiants de connexion à l'application** : `admin@ecole.com` / `Admin123`.

### 4.4 `Controllers/HomeController.cs` — le Dashboard protégé

```csharp
[Authorize]
public IActionResult Dashboard()
{
    ViewBag.NombreEtudiants = _context.Students.Count();
    ViewBag.NombreClasses = _context.ClassRooms.Count();
    ViewBag.NombreMatieres = _context.Subjects.Count();
    ViewBag.NombreNotes = _context.Grades.Count();
    return View();
}
```
`[Authorize]` est un attribut fourni par ASP.NET. Il s'exécute **avant** le code de l'action : si l'utilisateur n'a pas de cookie d'authentification valide, il est automatiquement redirigé vers `LoginPath` (configuré plus haut) avec un `?ReturnUrl=/Home/Dashboard`, sans jamais exécuter une seule ligne de `Dashboard()`. C'est ce qu'on a vérifié dans le test : en étant déconnecté, `/Home/Dashboard` redirige bien vers `/Account/Login?ReturnUrl=%2FHome%2FDashboard`.

### 4.5 `Views/Shared/_Layout.cshtml` — l'affichage conditionnel

```csharp
@{
    bool estConnecte = User.Identity?.IsAuthenticated ?? false;
}
```
`User` est disponible dans **toutes** les vues Razor automatiquement (il vient du `HttpContext` de la requête en cours). `User.Identity.IsAuthenticated` renvoie `true` si le cookie de connexion est valide. On l'utilise ensuite avec `@if (estConnecte) { ... } else { ... }` pour :
- afficher la sidebar complète + le menu utilisateur si connecté,
- afficher juste un bouton "Se connecter" sinon.

## 5. Qu'est-ce qu'un Claim et un Cookie, concrètement ?

- **Cookie** : petit fichier stocké par le navigateur, envoyé automatiquement à chaque requête vers notre site. ASP.NET Identity y stocke un identifiant chiffré/signé qui prouve « cet utilisateur s'est authentifié avec succès », sans jamais renvoyer le mot de passe.
- **Claims** : à l'intérieur de ce cookie, Identity encode des "affirmations" sur l'utilisateur (son `Id`, son `Email`, etc.) sous forme de paires clé/valeur. C'est `User.Identity` (et `User.Claims`) qui permet de les relire côté serveur, par exemple pour afficher "Bienvenue, Administrateur".

Dans ce projet, on n'a pas eu besoin d'ajouter de Claims personnalisés (un seul rôle : administrateur), mais c'est le mécanisme qui, plus tard, permettrait par exemple de distinguer un rôle "professeur" d'un rôle "administrateur".

## 6. Captures attendues

1. La page `/Account/Login` (formulaire de connexion).
2. Le tableau de bord après connexion (sidebar visible, cartes de statistiques à 0).
3. La page d'accueil après déconnexion (sidebar masquée, bouton "Se connecter").
4. Une tentative d'accès direct à `/Home/Dashboard` sans être connecté, montrant la redirection vers `/Account/Login?ReturnUrl=...`.

## 7. Étapes de test

1. `dotnet build` → aucune erreur.
2. `dotnet run` → vérifier dans les logs que le compte admin est créé (`INSERT INTO "AspNetUsers"` visible au premier démarrage seulement).
3. Se connecter avec `admin@ecole.com` / `Admin123` → doit rediriger vers `/Home/Dashboard`.
4. Vérifier que la sidebar et le menu "Administrateur" apparaissent uniquement après connexion.
5. Se déconnecter → doit revenir à `/` avec la sidebar masquée.
6. Essayer d'aller directement sur `/Home/Dashboard` sans être connecté → doit rediriger vers la page de connexion.
7. Essayer de se connecter avec un mauvais mot de passe → doit afficher "Email ou mot de passe incorrect." sans préciser lequel des deux est faux.

## 8. Commits Git à faire

```bash
git add Models/LoginViewModel.cs Controllers/AccountController.cs Views/Account/
git commit -m "feat(auth): ajout du formulaire de connexion et du contrôleur de compte"

git add Program.cs
git commit -m "feat(auth): configuration du cookie Identity et création du compte administrateur"

git add Controllers/HomeController.cs Views/Home/Dashboard.cshtml
git commit -m "feat(dashboard): tableau de bord protégé avec statistiques"

git add Views/Shared/_Layout.cshtml wwwroot/css/custom.css Views/Home/Index.cshtml
git commit -m "feat(layout): affichage conditionnel de la sidebar selon l'état de connexion"
```

## 9. Points à savoir répondre au professeur

- **« Où est stocké le mot de passe de l'admin ? »** → Jamais en clair. `CreateAsync` le hache (fonction à sens unique) avant de l'enregistrer dans `AspNetUsers.PasswordHash`. `PasswordSignInAsync` recalcule le hachage du mot de passe saisi et compare les deux hachages, sans jamais déchiffrer quoi que ce soit.
- **« Pourquoi le logout est en POST et pas en GET ? »** → Parce qu'une requête GET ne devrait jamais modifier l'état du serveur (bonne pratique HTTP), et parce qu'un lien GET est plus facile à déclencher involontairement (ex: un `<img src="/Account/Logout">` malveillant sur une autre page).
- **« Que se passe-t-il si je visite une page protégée sans être connecté ? »** → Le middleware d'authentification intercepte la requête avant le contrôleur, et redirige vers `LoginPath` avec un `ReturnUrl`, pour revenir automatiquement à la bonne page après connexion.
- **« Pourquoi utiliser `ViewBag` dans le Dashboard plutôt qu'un ViewModel ? »** → Par simplicité pour ce projet (4 nombres à afficher). Un vrai projet avec plus de données utiliserait un ViewModel dédié, mais `ViewBag` reste parfaitement valable pour un cas aussi simple.
