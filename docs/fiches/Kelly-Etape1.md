# Fiche d'explication — Kelly — Étape 1 : Architecture du projet

Cette fiche te sert à défendre ton code devant le professeur. Elle explique **quoi**, **pourquoi** et **comment**, ligne par ligne, pour tout ce que tu as créé à cette étape.

## 1. Objectif de l'étape

Poser les fondations du projet pour que Koita, Dougouré et Cécile puissent ensuite travailler chacun sur leur partie sans jamais se marcher dessus (voir `docs/01-conception-UML.md`, section 6). Concrètement :
- créer le projet ASP.NET Core MVC,
- connecter la base de données (Entity Framework Core + SQLite),
- créer les 4 modèles **en version minimale**,
- préparer ASP.NET Identity (sans encore construire les pages de connexion — ça, c'est l'étape 2),
- construire le layout général (navbar + sidebar) avec un design moderne.

## 2. Fichiers créés

| Fichier | Rôle |
|---|---|
| `GestionScolaire.slnx` | Fichier solution (regroupe le projet) |
| `GestionScolaire.csproj` | Fichier projet (dépendances NuGet, version .NET) |
| `Models/ClassRoom.cs` | Modèle minimal d'une classe |
| `Models/Student.cs` | Modèle minimal d'un étudiant |
| `Models/Subject.cs` | Modèle minimal d'une matière |
| `Models/Grade.cs` | Modèle minimal d'une note |
| `Data/AppDbContext.cs` | Le pont entre le C# et la base de données |
| `wwwroot/css/custom.css` | Styles personnalisés (sidebar, cartes, couleurs) |
| `.gitignore` | Empêche Git de suivre les fichiers `bin/`, `obj/`, la base `.db` |

## 3. Fichiers modifiés

| Fichier | Modification |
|---|---|
| `Program.cs` | Ajout d'Entity Framework Core, d'Identity, de `UseAuthentication()` |
| `appsettings.json` | Ajout de la chaîne de connexion vers la base SQLite |
| `Views/Shared/_Layout.cshtml` | Refonte complète : navbar + sidebar moderne |
| `Views/Home/Index.cshtml` | Page d'accueil avec bannière et cartes des modules |
| `Controllers/HomeController.cs` | Suppression de l'action `Privacy` (inutile) |

## 4. Pourquoi SQLite plutôt que SQL Server LocalDB ?

Le template ASP.NET par défaut propose souvent SQL Server LocalDB. On a choisi **SQLite** à la place :
- c'est un simple fichier (`GestionScolaire.db`), donc **aucune installation de serveur** n'est nécessaire ;
- les 4 machines de l'équipe (Koita, Dougouré, Cécile, toi) auront exactement le même comportement ;
- Entity Framework Core fonctionne exactement pareil derrière (mêmes commandes de migration).

## 5. Explication ligne par ligne

### 5.1 `Models/Student.cs`

```csharp
namespace GestionScolaire.Models
{
    public class Student
    {
        public int Id { get; set; }

        public string Nom { get; set; } = string.Empty;

        public string Prenom { get; set; } = string.Empty;

        // Clé étrangère : un étudiant appartient à une seule classe
        public int ClassRoomId { get; set; }

        // Propriété de navigation vers la classe de l'étudiant
        public ClassRoom? ClassRoom { get; set; }
    }
}
```

- `public int Id { get; set; }` : chaque étudiant a un identifiant unique. Entity Framework reconnaît automatiquement une propriété nommée `Id` comme **clé primaire**, sans avoir besoin de l'écrire ailleurs.
- `Nom` / `Prenom` : deux chaînes de caractères, initialisées à `string.Empty` pour éviter les valeurs `null`.
- `ClassRoomId` : c'est la **clé étrangère**. Sa convention de nommage (`NomDeLaClasse` + `Id`) permet à Entity Framework de comprendre tout seul qu'elle pointe vers `ClassRoom.Id`.
- `ClassRoom? ClassRoom` : la **propriété de navigation**. Elle permet d'écrire `etudiant.ClassRoom.Nom` en C# au lieu de faire une jointure SQL à la main. Le `?` indique qu'elle peut être vide (`null`) tant qu'on ne l'a pas explicitement chargée.

C'est un modèle **minimal** : Koita ajoutera `DateNaissance`, `Email` et les attributs de validation (`[Required]`, etc.) à l'étape 3.

### 5.2 `Data/AppDbContext.cs`

```csharp
using GestionScolaire.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace GestionScolaire.Data
{
    public class AppDbContext : IdentityDbContext<IdentityUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<ClassRoom> ClassRooms { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<Subject> Subjects { get; set; }
        public DbSet<Grade> Grades { get; set; }
    }
}
```

- `AppDbContext` hérite de `IdentityDbContext<IdentityUser>`. C'est ce qui nous donne **gratuitement** toutes les tables nécessaires à la connexion (`AspNetUsers`, `AspNetRoles`, etc.) sans avoir à les écrire nous-mêmes.
- Le constructeur `AppDbContext(DbContextOptions<AppDbContext> options) : base(options)` reçoit sa configuration (quelle base de données utiliser, quelle chaîne de connexion) depuis `Program.cs`. C'est de l'**injection de dépendances** basique : ASP.NET crée cet objet pour nous et le distribue à qui en a besoin (par exemple aux contrôleurs).
- Chaque ligne `DbSet<...>` correspond à **une table**. `DbSet<Student> Students` veut dire : « il existe une table `Students`, et je peux la manipuler comme une collection C# (`.Add()`, `.Where()`, `.ToList()`...) ».

### 5.3 `Program.cs` (parties ajoutées)

```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
```
On enregistre `AppDbContext` dans le conteneur de services de l'application, en lui disant d'utiliser SQLite avec la chaîne de connexion lue dans `appsettings.json`.

```csharp
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 6;
})
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();
```
On active ASP.NET Identity. On assouplit les règles de mot de passe (pas besoin de majuscule/chiffre/symbole) parce qu'il n'y a qu'un seul compte administrateur à créer nous-mêmes pour la démo — pas besoin d'une politique de sécurité digne d'une banque. `.AddEntityFrameworkStores<AppDbContext>()` dit à Identity de stocker les utilisateurs dans **notre** base de données, via **notre** `AppDbContext`.

```csharp
app.UseAuthentication();
app.UseAuthorization();
```
**L'ordre est important.** `UseAuthentication()` détermine **qui** fait la requête (en lisant le cookie de connexion). `UseAuthorization()` vérifie ensuite **ce que cette personne a le droit de faire**. Si on inversait l'ordre, l'application vérifierait des droits pour un utilisateur qu'elle ne connaît pas encore.

### 5.4 `Views/Shared/_Layout.cshtml`

Points clés à savoir expliquer :
- **`asp-controller` / `asp-action`** : ce sont des *Tag Helpers* Razor. Ils génèrent automatiquement la bonne URL (`/Students/Index`, `/Grades/Index`, ...) au lieu d'écrire l'URL en dur. Résultat : même si les routes changent, les liens restent corrects.
- Le sidebar contient déjà **tous** les liens (Étudiants, Classes, Matières, Notes) alors que ces pages n'existent pas encore : c'est voulu (voir conception UML section 6.2), pour que toi seule aies à toucher ce fichier.
- **Deux versions de la sidebar** existent dans le fichier : une visible uniquement sur grand écran (`d-none d-lg-flex`), une en `offcanvas` (tiroir coulissant) pour mobile, ouverte par un bouton hamburger. C'est Bootstrap qui gère l'affichage/masquage automatiquement selon la taille d'écran.
- `@RenderBody()` : c'est l'endroit où Razor insère le contenu de la vue affichée (ex. `Views/Home/Index.cshtml`). Toutes les vues du site partagent ce même layout.

### 5.5 `wwwroot/css/custom.css`

On utilise des **variables CSS** (`--accent`, `--sidebar-bg`, ...) définies une seule fois dans `:root`, puis réutilisées partout (`background-color: var(--accent)`). Avantage : si le professeur n'aime pas la couleur, on change **une seule ligne** pour changer tout le thème.

## 6. Commandes EF utilisées

```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

- `migrations add InitialCreate` : Entity Framework compare nos classes C# (`Student`, `ClassRoom`, ...) à l'état actuel de la base (vide au départ), et génère un fichier C# (`Migrations/..._InitialCreate.cs`) qui décrit les instructions SQL nécessaires (`CREATE TABLE ...`) pour que la base corresponde à nos modèles.
- `database update` : exécute réellement ces instructions sur la base `GestionScolaire.db`, qui est créée à ce moment-là.

**Important pour la suite** : chaque fois qu'un membre de l'équipe ajoute un champ à son modèle (Koita, Dougouré, Cécile), il devra relancer ces deux commandes pour mettre à jour la base.

## 7. Captures attendues

Prends une capture de :
1. La page d'accueil (`http://localhost:5010/`) montrant la bannière violette, la sidebar sombre et les 4 cartes de modules.
2. Le terminal après `dotnet ef database update` montrant `Done.`
3. (Optionnel) La sidebar en mode mobile (fenêtre réduite), montrant le bouton hamburger et le tiroir qui s'ouvre.

## 8. Étapes de test

1. `dotnet build` → doit se terminer par `La génération a réussi` sans erreur.
2. `dotnet ef database update` → doit créer/mettre à jour `GestionScolaire.db`.
3. `dotnet run` → noter l'URL affichée (`http://localhost:5010` en Development).
4. Ouvrir cette URL dans un navigateur : la page d'accueil doit s'afficher avec le style moderne (sidebar sombre, bannière violette, cartes).
5. Vérifier que cliquer sur "Étudiants", "Classes", "Matières" ou "Notes" dans la sidebar renvoie une erreur 404 **normale** à ce stade (ces pages seront construites aux étapes suivantes par leurs responsables).

## 9. Commits Git à faire

En respectant la convention définie dans la conception (`type(portée): description`) :

```bash
git init
git add .gitignore
git commit -m "chore: initialisation du dépôt et du .gitignore"

git add GestionScolaire.slnx GestionScolaire.csproj Program.cs appsettings.json
git commit -m "feat(architecture): création du projet ASP.NET MVC et configuration EF Core + Identity"

git add Models/ Data/
git commit -m "feat(architecture): ajout des modèles minimaux et de AppDbContext"

git add Migrations/
git commit -m "feat(architecture): première migration EF Core (InitialCreate)"

git add Views/Shared/_Layout.cshtml wwwroot/css/custom.css
git commit -m "feat(layout): navbar, sidebar moderne et styles personnalisés"

git add Views/Home/ Controllers/HomeController.cs
git commit -m "feat(accueil): page d'accueil avec présentation des modules"
```

## 10. Points à savoir répondre au professeur

- **« Pourquoi `IdentityDbContext` et pas `DbContext` ? »** → Parce qu'on utilise ASP.NET Identity pour la connexion, et qu'Identity a besoin de ses propres tables (`AspNetUsers`, etc.). `IdentityDbContext` les ajoute automatiquement à notre contexte.
- **« Pourquoi les modèles sont-ils si vides ? »** → C'est volontaire : chaque membre de l'équipe complète son propre modèle à son étape, pour éviter les conflits Git sur les mêmes fichiers.
- **« Comment la base de données est-elle créée ? »** → Via les migrations Entity Framework (`dotnet ef migrations add` + `dotnet ef database update`), pas en écrivant du SQL à la main.
- **« Pourquoi `UseAuthentication()` avant `UseAuthorization()` ? »** → Il faut savoir *qui* fait la requête avant de vérifier *ce qu'il a le droit de faire*.
