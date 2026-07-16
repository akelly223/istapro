# Fiche d'explication — Koita — Étape 3 : Gestion des étudiants

Cette fiche te sert à défendre ton code devant le professeur. Elle explique **quoi**, **pourquoi** et **comment**, pour tout ce que tu as créé à cette étape.

## 1. Objectif de l'étape

Réaliser le CRUD (Create, Read, Update, Delete) complet des étudiants : liste, ajout, modification, suppression, avec un formulaire moderne et de la validation.

## 2. Fichiers modifiés

| Fichier | Rôle |
|---|---|
| `Models/Student.cs` | Modèle complété avec `DateNaissance`, `Email` et les validations |

## 3. Fichiers créés

| Fichier | Rôle |
|---|---|
| `Controllers/StudentsController.cs` | Toute la logique CRUD |
| `Views/Students/Index.cshtml` | Liste des étudiants (tableau) |
| `Views/Students/Create.cshtml` | Formulaire d'ajout |
| `Views/Students/Edit.cshtml` | Formulaire de modification |
| `Views/Students/Delete.cshtml` | Page de confirmation de suppression |
| `Migrations/..._AjoutDetailsEtudiant.cs` | Migration EF pour les nouveaux champs |

Tu n'as touché **aucun autre fichier** (pas `AppDbContext`, pas `_Layout.cshtml`) : c'était déjà prêt depuis l'étape 1, exactement pour que tu puisses travailler sans conflit Git avec les autres.

## 4. Le modèle `Student.cs` complété

```csharp
public class Student
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Le nom est obligatoire.")]
    [StringLength(50, ErrorMessage = "Le nom ne doit pas dépasser 50 caractères.")]
    [Display(Name = "Nom")]
    public string Nom { get; set; } = string.Empty;

    [Required(ErrorMessage = "Le prénom est obligatoire.")]
    [StringLength(50, ErrorMessage = "Le prénom ne doit pas dépasser 50 caractères.")]
    [Display(Name = "Prénom")]
    public string Prenom { get; set; } = string.Empty;

    [Required(ErrorMessage = "La date de naissance est obligatoire.")]
    [DataType(DataType.Date)]
    [Display(Name = "Date de naissance")]
    public DateTime DateNaissance { get; set; }

    [Required(ErrorMessage = "L'email est obligatoire.")]
    [EmailAddress(ErrorMessage = "Le format de l'email n'est pas valide.")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "La classe est obligatoire.")]
    [Display(Name = "Classe")]
    public int ClassRoomId { get; set; }

    public ClassRoom? ClassRoom { get; set; }
}
```

**Les attributs de validation (Data Annotations)** :
- `[Required]` : champ obligatoire, ne peut pas être vide.
- `[StringLength(50)]` : limite la longueur du texte (protège aussi la base de données).
- `[EmailAddress]` : vérifie le format `quelquechose@quelquechose.xxx`.
- `[DataType(DataType.Date)]` : indique à Razor de générer un `<input type="date">` plutôt qu'un champ texte classique.
- `[Display(Name = "...")]` : le texte affiché dans les `<label>` générés automatiquement par `asp-for`.

Ces validations s'appliquent **deux fois** : côté navigateur (grâce à `_ValidationScriptsPartial`, en JavaScript, pour un retour immédiat sans recharger la page) et côté serveur (`ModelState.IsValid` dans le contrôleur, **indispensable** car un utilisateur malveillant pourrait désactiver le JavaScript et envoyer n'importe quoi).

## 5. La migration EF Core

```bash
dotnet ef migrations add AjoutDetailsEtudiant
dotnet ef database update
```

Comme le modèle `Student` existait déjà (créé en version minimale par Kelly à l'étape 1) et que tu as **ajouté** deux propriétés (`DateNaissance`, `Email`), EF Core génère une migration qui modifie la table existante :

```csharp
migrationBuilder.AddColumn<DateTime>(name: "DateNaissance", table: "Students", ...);
migrationBuilder.AddColumn<string>(name: "Email", table: "Students", ...);
```

C'est la différence avec la migration `InitialCreate` de l'étape 1 : celle-là créait les tables, celle-ci les **modifie**. Chaque membre de l'équipe doit lancer ces deux commandes après avoir récupéré ton code (`git pull`), sinon sa base de données locale n'aura pas ces nouvelles colonnes et l'application plantera.

## 6. Le contrôleur `StudentsController.cs`

### 6.1 L'attribut `[Authorize]` sur le contrôleur

```csharp
[Authorize]
public class StudentsController : Controller
```

Placé sur la **classe** (et non sur chaque action), il protège automatiquement toutes les actions du contrôleur : `Index`, `Create`, `Edit`, `Delete`. Un visiteur non connecté est redirigé vers la page de connexion (mise en place par Kelly à l'étape 2).

### 6.2 Liste des étudiants (Index)

```csharp
public async Task<IActionResult> Index()
{
    var etudiants = await _context.Students
        .Include(e => e.ClassRoom)
        .OrderBy(e => e.Nom)
        .ToListAsync();

    return View(etudiants);
}
```
- `.Include(e => e.ClassRoom)` : **très important**. Par défaut, Entity Framework ne charge pas automatiquement les objets liés (ici la classe de l'étudiant) — c'est ce qu'on appelle le *lazy vs eager loading*. Sans `.Include`, `etudiant.ClassRoom` serait toujours `null` dans la vue, et on ne pourrait pas afficher le nom de la classe.
- `.OrderBy(e => e.Nom)` : trie les résultats par nom, directement en SQL (plus efficace que trier en C# après coup).
- `async`/`await`/`ToListAsync()` : la requête à la base de données se fait de façon asynchrone. Pendant que le serveur attend la réponse de la base, il peut traiter d'autres requêtes d'autres visiteurs — c'est plus performant qu'une méthode bloquante.

### 6.3 Créer un étudiant (Create)

```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Create(Student student)
{
    if (!ModelState.IsValid)
    {
        ViewBag.Classes = new SelectList(_context.ClassRooms.OrderBy(c => c.Nom), "Id", "Nom", student.ClassRoomId);
        return View(student);
    }

    _context.Students.Add(student);
    await _context.SaveChangesAsync();

    TempData["Message"] = "Étudiant ajouté avec succès.";
    return RedirectToAction(nameof(Index));
}
```
- Si la validation échoue, on **recharge la liste des classes** pour le menu déroulant avant de renvoyer la vue avec les erreurs — sinon le `<select>` serait vide et l'utilisateur ne pourrait pas corriger son choix de classe.
- `_context.Students.Add(student)` : ajoute l'étudiant en mémoire (rien n'est encore écrit dans la base).
- `await _context.SaveChangesAsync()` : **c'est cette ligne qui écrit réellement** dans la base de données (génère un `INSERT INTO Students ...`).
- `TempData["Message"]` : contrairement à `ViewBag`/`ViewData` qui ne survivent qu'à une seule requête, `TempData` survit à une **redirection**. C'est indispensable ici car `RedirectToAction` fait faire un nouvel aller-retour au navigateur (`Redirect` puis `GET /Students`) : sans `TempData`, le message de succès serait perdu en route.
- **Pattern Post/Redirect/Get** : après un POST réussi, on redirige toujours vers une action GET (`Index`) plutôt que de renvoyer directement une vue. Ça évite que l'utilisateur recrée un étudiant en double en rafraîchissant la page (F5) après validation.

### 6.4 Modifier un étudiant (Edit)

```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Edit(int id, Student student)
{
    if (id != student.Id)
    {
        return NotFound();
    }
    ...
    _context.Students.Update(student);
    await _context.SaveChangesAsync();
    ...
}
```
- La vérification `id != student.Id` protège contre une manipulation d'URL (ex: quelqu'un modifie l'URL `/Students/Edit/3` mais le formulaire caché contient `Id = 7`) — normalement impossible via l'interface, mais c'est une bonne pratique de sécurité de base.
- `_context.Students.Update(student)` : marque **tout l'objet** comme modifié (contrairement à changer un seul champ). EF Core génère un `UPDATE` avec toutes les colonnes.

### 6.5 Supprimer un étudiant (Delete)

Il y a **deux actions** pour la suppression :
```csharp
public async Task<IActionResult> Delete(int id)          // GET : affiche la page de confirmation
[HttpPost, ActionName("Delete")]
public async Task<IActionResult> DeleteConfirmed(int id) // POST : supprime réellement
```
On ne supprime **jamais** directement sur un simple lien/clic (GET) : il faut toujours une étape de confirmation en POST. `[ActionName("Delete")]` permet d'avoir deux méthodes C# avec des noms différents (`Delete` et `DeleteConfirmed`, obligatoire car C# n'autorise pas deux méthodes avec la même signature) tout en gardant la même URL `/Students/Delete/5` pour les deux.

## 7. Les vues

- **`Index.cshtml`** : `@model IEnumerable<Student>` — la vue reçoit une **liste** d'étudiants (pas un seul). Le tableau utilise la classe `table-modern` définie par Kelly dans `custom.css`, donc tu n'as pas eu besoin d'écrire le moindre style toi-même.
- **`Create.cshtml` / `Edit.cshtml`** : `asp-for="NomDuChamp"` génère automatiquement l'attribut `name`, l'`id`, et relie le champ à la validation. `asp-items="ViewBag.Classes"` transforme la `SelectList` envoyée par le contrôleur en `<option>` HTML.
- **`Delete.cshtml`** : pas de formulaire de saisie, juste un résumé en lecture seule + un bouton de confirmation.

## 8. Captures attendues

1. La liste des étudiants avec au moins une ligne.
2. Le formulaire d'ajout rempli avant validation.
3. Le message de succès vert après ajout ("Étudiant ajouté avec succès.").
4. Le formulaire de modification pré-rempli avec les données existantes.
5. La page de confirmation de suppression.

## 9. Étapes de test

1. `dotnet ef migrations add AjoutDetailsEtudiant` puis `dotnet ef database update`.
2. `dotnet build` → aucune erreur.
3. Se connecter en tant qu'administrateur.
4. Aller sur "Étudiants" dans la sidebar → la liste s'affiche (vide au départ).
5. Cliquer "Ajouter un étudiant", remplir le formulaire, valider → retour à la liste avec le message de succès et la nouvelle ligne.
6. Essayer de soumettre le formulaire avec un champ vide → un message d'erreur doit s'afficher **sans recharger la page** (validation JavaScript), puis un deuxième message si on désactive le JavaScript (validation serveur).
7. Cliquer sur l'icône crayon → modifier un champ → enregistrer → vérifier que la liste reflète le changement.
8. Cliquer sur l'icône poubelle → confirmer → vérifier que la ligne disparaît de la liste.
9. Retourner sur le Dashboard → vérifier que le compteur "Étudiants" reflète le nombre réel.

## 10. Commits Git à faire

```bash
git checkout -b feature/students-koita

git add Models/Student.cs
git commit -m "feat(students): completion du modele Student avec validations"

git add Migrations/
git commit -m "feat(students): migration EF pour les nouveaux champs etudiant"

git add Controllers/StudentsController.cs
git commit -m "feat(students): ajout du CRUD etudiant"

git add Views/Students/
git commit -m "feat(students): vues modernes pour la liste, l'ajout, la modification et la suppression"

git push -u origin feature/students-koita
```
Ensuite, ouvre une Pull Request sur GitHub pour fusionner `feature/students-koita` dans `main`.

## 11. Points à savoir répondre au professeur

- **« Pourquoi `.Include(e => e.ClassRoom)` est nécessaire ? »** → Par défaut, Entity Framework ne charge que les colonnes de la table principale. Sans `.Include`, la propriété de navigation `ClassRoom` resterait `null`, même si `ClassRoomId` a une valeur.
- **« Que se passe-t-il si je soumets le formulaire sans remplir le nom ? »** → `ModelState.IsValid` devient `false` côté serveur (même si le JavaScript a été contourné), et le contrôleur renvoie la même vue avec les messages d'erreur, sans toucher à la base de données.
- **« Pourquoi la suppression demande une confirmation et n'est pas un simple lien ? »** → Une suppression est irréversible ; il faut une étape en POST avec confirmation pour éviter qu'elle se déclenche par accident (ex: un robot d'indexation qui suivrait tous les liens de la page).
- **« Comment la base de données a-t-elle été mise à jour pour accueillir les nouveaux champs ? »** → Via une migration EF Core (`AjoutDetailsEtudiant`), qui a ajouté les colonnes `DateNaissance` et `Email` à la table `Students` déjà existante, sans supprimer les données déjà présentes.
