# Fiche d'explication — Kelly — Extension : Espaces par rôle (Administrateur / Professeur / Étudiant)

Cette fiche documente l'extension ajoutée après l'étape 6 : au lieu d'un seul compte administrateur, l'application gère désormais **3 rôles** avec des espaces différents, plus deux nouveaux modules (Devoirs et Présences).

## 1. Objectif

Le professeur voulait voir « ce que chaque type d'utilisateur peut voir et faire ». On a donc ajouté :
- Un rôle **Professeur** : gère les notes, assigne des devoirs, consulte les rendus, fait l'appel (présences). Consulte étudiants/classes/matières en lecture seule.
- Un rôle **Étudiant** : un compte par étudiant, créé automatiquement. Voit son profil complet, ses notes, sa moyenne, et peut envoyer ses devoirs.
- Le rôle **Administrateur** existant garde tous les droits.

## 2. Fichiers créés

| Fichier | Rôle |
|---|---|
| `Data/AppRoles.cs` | Constantes des noms de rôles (évite les fautes de frappe) |
| `Models/Homework.cs` | Un devoir assigné à une classe pour une matière |
| `Models/HomeworkSubmission.cs` | Le rendu (fichier) envoyé par un étudiant pour un devoir |
| `Models/Attendance.cs` | Présence/absence d'un étudiant à une date |
| `Controllers/HomeworksController.cs` | Gestion des devoirs (Prof/Admin) + espace étudiant (rendre un devoir) |
| `Controllers/AttendancesController.cs` | Prise de présence (Prof/Admin) |
| `Views/Homeworks/*`, `Views/Attendances/*` | Vues associées |
| `Views/Home/MonEspace.cshtml` | Espace de l'étudiant connecté |
| `Views/Home/EspaceProfesseur.cshtml` | Espace du professeur connecté |
| `Views/Shared/_SidebarNav.cshtml` | Menu latéral factorisé (un seul fichier pour les 2 sidebars desktop/mobile) |
| `Views/Account/AccessDenied.cshtml` | Page affichée quand un rôle n'a pas les droits |

## 3. Fichiers modifiés

| Fichier | Modification |
|---|---|
| `Program.cs` | Seed des 3 rôles, compte Professeur de démo, culture Identity (mot de passe sans majuscule/minuscule obligatoire) |
| `Models/Student.cs` | Ajout de `UserId` (lien vers le compte Identity de l'étudiant) |
| `Controllers/StudentsController.cs` | Création/suppression/synchronisation automatique du compte étudiant |
| `Controllers/AccountController.cs` | Redirection après connexion selon le rôle ; action `AccessDenied` |
| `Controllers/ClassRoomsController.cs`, `SubjectsController.cs`, `GradesController.cs` | Restriction des droits par rôle |
| `Views/Students/Index.cshtml`, `ClassRooms/Index.cshtml`, `Subjects/Index.cshtml` | Boutons masqués si non-administrateur |
| `Views/Shared/_Layout.cshtml` | Sidebar factorisée, libellé de rôle dynamique |
| `Data/AppDbContext.cs` | 3 nouveaux `DbSet` |

## 4. Comment un étudiant obtient son compte

Dans `StudentsController.Create` :
```csharp
string motDePasse = student.DateNaissance.ToString("ddMMyyyy");
var compte = new IdentityUser { UserName = student.Email, Email = student.Email, EmailConfirmed = true };
var resultat = await _userManager.CreateAsync(compte, motDePasse);

if (resultat.Succeeded)
{
    await _userManager.AddToRoleAsync(compte, AppRoles.Etudiant);
    student.UserId = compte.Id;
    await _context.SaveChangesAsync();
}
```
- Le mot de passe est la date de naissance au format `jjMMaaaa` (ex : `14052008` pour le 14 mai 2008).
- **Piège rencontré** : Identity exige par défaut qu'un mot de passe contienne au moins une minuscule (`RequireLowercase = true` par défaut). Une date de naissance ne contient que des chiffres, donc la création échouait systématiquement avec l'erreur *"Passwords must have at least one lowercase"*. Il a fallu ajouter `options.Password.RequireLowercase = false;` dans `Program.cs`, en plus des règles déjà assouplies pour l'administrateur (pas de majuscule, pas de symbole obligatoire).
- `student.UserId = compte.Id` : on relie la fiche étudiant à son compte de connexion. C'est ce lien qui permet, plus tard, de retrouver "quel étudiant correspond à la personne connectée" (voir `HomeController.MonEspace`).

Quand on modifie ou supprime un étudiant, ce lien est utilisé pour garder le compte synchronisé (email mis à jour, compte supprimé en même temps que la fiche).

## 5. Comment la redirection après connexion fonctionne

Dans `AccountController.Login`, après une connexion réussie :
```csharp
var utilisateur = await _userManager.FindByEmailAsync(model.Email);
if (await _userManager.IsInRoleAsync(utilisateur, AppRoles.Etudiant))
{
    return RedirectToAction("MonEspace", "Home");
}
if (await _userManager.IsInRoleAsync(utilisateur, AppRoles.Professeur))
{
    return RedirectToAction("EspaceProfesseur", "Home");
}
return RedirectToAction("Dashboard", "Home");
```
`IsInRoleAsync` vérifie à quel(s) rôle(s) appartient le compte, et on redirige vers l'espace correspondant.

## 6. Comment la sidebar change selon le rôle

`Views/Shared/_SidebarNav.cshtml` (un seul fichier, utilisé deux fois via `<partial name="_SidebarNav" />` pour la version desktop et la version mobile — évite de dupliquer le menu) :
```csharp
bool estAdmin = User.IsInRole(AppRoles.Administrateur);
bool estProf = User.IsInRole(AppRoles.Professeur);
bool estEtudiant = User.IsInRole(AppRoles.Etudiant);
```
`User.IsInRole(...)` est disponible dans n'importe quelle vue (comme `User.Identity.IsAuthenticated` vu à l'étape 2), car il vient du cookie d'authentification. Chaque bloc `@if` du menu n'affiche que ce que le rôle courant a le droit de voir.

## 7. Comment les droits sont vérifiés côté serveur (pas que dans la vue)

Cacher un bouton dans une vue ne suffit pas : il faut aussi bloquer l'action côté serveur (sinon quelqu'un pourrait taper l'URL directement). C'est le rôle de `[Authorize(Roles = "...")]` :
```csharp
[Authorize(Roles = AppRoles.Administrateur + "," + AppRoles.Professeur)]
public class StudentsController : Controller
{
    ...
    [Authorize(Roles = AppRoles.Administrateur)]
    public IActionResult Create() { ... }
```
- L'attribut sur la **classe** s'applique à toutes les actions par défaut (ici : Administrateur ou Professeur peuvent consulter `Index`).
- Un attribut sur une **action précise** vient le remplacer pour cette action (ici : `Create` est réservé à l'Administrateur seul).
- Si un Professeur essaie quand même d'aller sur `/Students/Create`, il est redirigé vers `/Account/AccessDenied` (configuré dans `Program.cs` via `options.AccessDeniedPath`).

## 8. Les devoirs et l'envoi de fichier

### 8.1 Assigner un devoir (Professeur/Administrateur)
Rien de nouveau conceptuellement : un CRUD classique (`Homework`), avec deux menus déroulants (matière, classe) comme pour les notes.

### 8.2 Envoyer un fichier (étudiant)
```csharp
[HttpPost]
[Authorize(Roles = AppRoles.Etudiant)]
public async Task<IActionResult> Soumettre(int id, IFormFile fichier)
{
    ...
    string nomFichierUnique = $"{Guid.NewGuid()}{Path.GetExtension(fichier.FileName)}";
    string cheminComplet = Path.Combine(dossierUploads, nomFichierUnique);

    using (var flux = new FileStream(cheminComplet, FileMode.Create))
    {
        await fichier.CopyToAsync(flux);
    }
    ...
}
```
- `IFormFile` est le type ASP.NET Core qui représente un fichier envoyé dans un formulaire. Pour que le fichier arrive jusqu'au serveur, le formulaire HTML doit avoir `enctype="multipart/form-data"` (sinon seul le texte serait envoyé, pas le fichier).
- On ne garde **jamais** le nom du fichier tel que l'étudiant l'a choisi comme nom de fichier sur le serveur (`Guid.NewGuid()` à la place) : si deux étudiants envoient tous les deux `devoir.pdf`, on ne veut pas que le second écrase le fichier du premier. Le nom d'origine (`NomFichierOriginal`) est quand même gardé en base pour l'afficher au professeur.
- Le fichier est stocké dans `wwwroot/uploads/devoirs/`. Si un étudiant renvoie un nouveau fichier pour le même devoir, l'ancien est supprimé du disque avant d'enregistrer le nouveau.

### 8.3 Téléchargement sécurisé
```csharp
[Authorize]
public async Task<IActionResult> Telecharger(int id)
{
    var soumission = await _context.HomeworkSubmissions.FindAsync(id);
    ...
    if (User.IsInRole(AppRoles.Etudiant))
    {
        var etudiant = await ObtenirEtudiantConnecteAsync();
        if (etudiant == null || soumission.StudentId != etudiant.Id)
        {
            return Forbid();
        }
    }
    ...
}
```
Un étudiant ne peut télécharger **que son propre rendu** (vérification `soumission.StudentId != etudiant.Id`), alors qu'un professeur/administrateur peut télécharger n'importe quel rendu pour corriger. C'est un contrôle d'accès **au niveau de la donnée**, pas seulement du rôle : deux étudiants ont le même rôle "Etudiant", mais l'un ne doit pas pouvoir voir le devoir de l'autre simplement en changeant l'`id` dans l'URL.

## 9. Les présences

`AttendancesController.Index` affiche la liste des étudiants d'une classe pour une date choisie, avec une case à cocher "Présent" pré-remplie selon ce qui existe déjà en base. `Enregistrer` fait un **upsert** (met à jour si la ligne existe déjà pour cet étudiant à cette date, la crée sinon) plutôt que de dupliquer des lignes à chaque nouvel appel pour le même jour.

## 10. Captures attendues

1. Connexion en tant qu'administrateur → sidebar complète (Dashboard, Étudiants, Classes, Matières, Notes, Devoirs, Présences).
2. Connexion en tant que professeur (`prof@ecole.com` / `Prof123`) → "Espace Professeur" avec cartes d'accès rapide ; liste des étudiants en lecture seule (pas de bouton Ajouter/Modifier/Supprimer).
3. Tentative d'accès à `/Students/Create` en tant que professeur → page "Accès refusé".
4. Ajout d'un étudiant → message affichant les identifiants générés.
5. Connexion avec le compte étudiant généré → "Mon espace" avec profil, moyenne, notes.
6. Envoi d'un devoir depuis l'espace étudiant → statut passe à "Envoyé".
7. Le professeur consulte "Rendus" du devoir → voit le fichier et peut le télécharger.
8. Prise de présence par le professeur → cases cochées/décochées, message de confirmation.

## 11. Étapes de test

1. `dotnet ef database update` (2 migrations ajoutées : `AjoutLienCompteEtudiant`, `AjoutDevoirsEtPresences`).
2. Se connecter en administrateur, créer un étudiant, vérifier le message d'identifiants générés.
3. Se déconnecter, se connecter avec ces identifiants → doit arriver sur "Mon espace".
4. Se déconnecter, se connecter en `prof@ecole.com` / `Prof123` → doit arriver sur "Espace Professeur".
5. En tant que professeur, essayer `/Students/Create` directement dans l'URL → doit afficher "Accès refusé", pas planter.
6. Assigner un devoir à la classe de l'étudiant testé, se reconnecter en étudiant, l'envoyer, vérifier le statut "Envoyé".
7. Se reconnecter en professeur, aller sur "Rendus" du devoir, télécharger le fichier envoyé.
8. Faire l'appel pour une classe à une date, décocher un étudiant, enregistrer, revenir sur la même classe/date → la case doit rester décochée (preuve que ça a bien été sauvegardé, pas juste affiché).

## 12. Points à savoir répondre au professeur

- **« Pourquoi un compte Identity par étudiant plutôt qu'un seul compte partagé ? »** → Pour que chaque étudiant ne voie que ses propres notes et devoirs. Un seul compte partagé ne permettrait pas de distinguer qui est connecté.
- **« Que se passe-t-il si deux étudiants ont la même date de naissance ? »** → Ils auraient le même mot de passe initial, mais des comptes différents (identifiés par leur email, unique). Ce n'est pas un problème de sécurité grave pour un projet scolaire, mais dans un vrai système on inviterait chaque étudiant à changer son mot de passe à la première connexion.
- **« Pourquoi vérifier `soumission.StudentId != etudiant.Id` dans `Telecharger` alors qu'on a déjà `[Authorize(Roles = "Etudiant")]` ? »** → Parce que `[Authorize(Roles=...)]` vérifie seulement **le rôle**, pas **à qui appartient la donnée**. Tous les étudiants partagent le même rôle "Etudiant" ; sans cette vérification supplémentaire, n'importe quel étudiant pourrait télécharger le devoir de n'importe quel autre juste en devinant l'`id` dans l'URL.
- **« Pourquoi factoriser la sidebar dans `_SidebarNav.cshtml` ? »** → Pour ne pas dupliquer la même logique de rôles à deux endroits (version desktop et version mobile) : un seul fichier, modifié une seule fois si le menu doit changer.
