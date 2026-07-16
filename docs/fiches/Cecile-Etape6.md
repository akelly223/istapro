# Fiche d'explication — Cécile — Étape 6 : Gestion des notes

Cette fiche te sert à défendre ton code devant le professeur. C'est la dernière étape : ton module relie les étudiants (Koita) et les matières (Dougouré) grâce aux notes.

## 1. Objectif de l'étape

- Ajouter une note (étudiant + matière + valeur + date)
- Modifier une note
- Liste de toutes les notes
- Consultation des notes d'un étudiant en particulier, avec sa moyenne

## 2. Fichiers modifiés

| Fichier | Rôle |
|---|---|
| `Models/Grade.cs` | Modèle complété avec `DateEvaluation` et validation `[Range(0,20)]` |

## 3. Fichiers créés

| Fichier | Rôle |
|---|---|
| `Controllers/GradesController.cs` | CRUD des notes + filtre par étudiant + calcul de moyenne |
| `Views/Grades/Index.cshtml` | Liste des notes, avec filtre et moyenne pondérée |
| `Views/Grades/Create.cshtml` | Formulaire d'ajout |
| `Views/Grades/Edit.cshtml` | Formulaire de modification |
| `Views/Grades/Delete.cshtml` | Confirmation de suppression |
| `Migrations/..._AjoutDetailsNote.cs` | Migration EF pour `DateEvaluation` |

Comme les autres, tu n'as touché ni `AppDbContext.cs` ni `_Layout.cshtml` : préparés dès l'étape 1.

**Petite exception à signaler au professeur** : à cette étape, on a aussi modifié `Program.cs` (ajout d'une seule section) pour corriger un bug qui touchait tout le monde, pas seulement les notes — voir section 6 ci-dessous.

## 4. Le modèle `Grade.cs` complété

```csharp
public class Grade
{
    public int Id { get; set; }

    [Required(ErrorMessage = "L'étudiant est obligatoire.")]
    public int StudentId { get; set; }

    [Required(ErrorMessage = "La matière est obligatoire.")]
    public int SubjectId { get; set; }

    [Required(ErrorMessage = "La note est obligatoire.")]
    [Range(0, 20, ErrorMessage = "La note doit être comprise entre 0 et 20.")]
    public double Valeur { get; set; }

    [Required(ErrorMessage = "La date d'évaluation est obligatoire.")]
    [DataType(DataType.Date)]
    public DateTime DateEvaluation { get; set; }

    public Student? Student { get; set; }
    public Subject? Subject { get; set; }
}
```

`Grade` est ce qu'on appelle une **classe association** (voir le MCD de la conception UML) : elle ne représente pas un "objet" du monde réel comme `Student` ou `Subject`, mais **la relation** entre les deux — une note relie toujours un étudiant précis à une matière précise. C'est pour ça qu'elle a deux clés étrangères (`StudentId` et `SubjectId`) au lieu d'une seule.

## 5. Le contrôleur `GradesController.cs`

### 5.1 Le filtre "un seul contrôleur pour deux besoins"

Le cahier des charges demande à la fois une **liste de toutes les notes** et une **consultation des notes d'un étudiant**. Plutôt que de dupliquer le code dans deux actions différentes, une seule action `Index` gère les deux cas grâce à un paramètre optionnel :

```csharp
public async Task<IActionResult> Index(int? studentId)
{
    var requete = _context.Grades
        .Include(n => n.Student)
        .Include(n => n.Subject)
        .AsQueryable();

    if (studentId.HasValue)
    {
        requete = requete.Where(n => n.StudentId == studentId.Value);
    }

    var notes = await requete.OrderByDescending(n => n.DateEvaluation).ToListAsync();
    ...
}
```
- `int? studentId` : le `?` rend le paramètre **facultatif**. `/Grades` (sans paramètre) affiche tout ; `/Grades?studentId=3` filtre sur l'étudiant n°3.
- `.AsQueryable()` : transforme la requête en objet "encore modifiable" — la vraie requête SQL n'est envoyée à la base qu'au moment du `.ToListAsync()` final. Ça permet d'ajouter le `.Where(...)` **seulement si nécessaire**, sans dupliquer la requête.
- Deux `.Include(...)` : un pour charger l'étudiant de chaque note, un pour la matière — sinon `note.Student` et `note.Subject` seraient `null` dans la vue.

Le menu déroulant de filtre (`<select onchange="this.form.submit()">` dans la vue) soumet automatiquement le formulaire dès qu'on choisit un étudiant, sans bouton "Valider" à cliquer.

### 5.2 Le calcul de la moyenne pondérée

```csharp
if (studentId.HasValue && notes.Any())
{
    double sommePonderee = notes.Sum(n => n.Valeur * n.Subject!.Coefficient);
    double sommeCoefficients = notes.Sum(n => n.Subject!.Coefficient);
    ViewBag.Moyenne = sommeCoefficients > 0 ? sommePonderee / sommeCoefficients : (double?)null;
}
```
C'est le calcul classique d'une **moyenne pondérée** : chaque note compte proportionnellement au coefficient de sa matière (une note de 10 en coefficient 4 pèse plus qu'une note de 10 en coefficient 1).
- `n.Subject!.Coefficient` : le `!` (appelé "null-forgiving operator") dit au compilateur « je suis sûr que `Subject` n'est pas `null` ici » — c'est vrai puisqu'on a fait `.Include(n => n.Subject)` juste avant.
- Cette moyenne n'est calculée **que si un étudiant précis est sélectionné** (`studentId.HasValue`), pas sur la liste complète de tout le monde, ce qui n'aurait pas de sens.

### 5.3 Remplir les menus déroulants (Étudiant, Matière)

```csharp
private async Task<List<object>> ListeEtudiantsPourMenu()
{
    return (await _context.Students
        .OrderBy(e => e.Nom)
        .Select(e => new { e.Id, NomComplet = e.Nom + " " + e.Prenom })
        .ToListAsync())
        .Cast<object>()
        .ToList();
}
```
Le modèle `Student` n'a pas de propriété "NomComplet" (et on n'en a pas ajouté, pour ne pas toucher au fichier `Student.cs` de Koita). À la place, on **projette** (`.Select(...)`) chaque étudiant vers un petit objet anonyme avec juste `Id` et `NomComplet` (Nom + Prénom concaténés), directement traduit en SQL par Entity Framework. C'est cette liste qu'on donne ensuite à `SelectList` pour construire le menu déroulant HTML.

### 5.4 Create / Edit / Delete

Même schéma que les autres modules (`[Authorize]`, Post/Redirect/Get, `ValidateAntiForgeryToken`, `TempData["Message"]`). La seule différence : `Create` et `Edit` ont besoin de charger **deux** listes déroulantes (étudiants et matières), factorisées dans une méthode privée `ChargerListesDeroulantes(...)` réutilisée par les deux actions, pour ne pas dupliquer ce code.

## 6. Le bug de culture corrigé (dans `Program.cs`)

En testant l'ajout d'une note avec la valeur `16.5`, le formulaire refusait la valeur avec l'erreur *"The value '16.5' is not valid for Note."* En creusant : ASP.NET Core essayait de convertir le texte `"16.5"` en `double` en utilisant la **culture régionale de l'ordinateur** (sur un PC configuré en français, le séparateur décimal attendu est la virgule `,`, pas le point `.`). Comme le point ne correspondait pas à ce que le serveur attendait, la conversion échouait **avant même** que la validation `[Range(0,20)]` ne s'exécute.

La correction (dans `Program.cs`) :
```csharp
var culture = new CultureInfo("en-US");
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(culture),
    SupportedCultures = new[] { culture },
    SupportedUICultures = new[] { culture }
});
```
Ça force l'application à toujours interpréter les nombres avec un point comme séparateur décimal, peu importe la configuration régionale de l'ordinateur qui l'exécute. Le texte affiché à l'écran reste en français (ce sont juste des chaînes de caractères qu'on a écrites nous-mêmes) — seule l'interprétation des **nombres** est concernée.

## 7. Captures attendues

1. La liste de toutes les notes, avec des badges verts (≥10) et rouges (<10).
2. Le formulaire d'ajout d'une note avec les menus déroulants Étudiant/Matière.
3. La liste filtrée sur un seul étudiant, avec la moyenne pondérée affichée.
4. Une tentative de note à 25 ou -3 → message d'erreur de validation.
5. Le Dashboard montrant que le compteur "Notes" correspond bien au nombre réel.

## 8. Étapes de test

1. `dotnet ef migrations add AjoutDetailsNote` puis `dotnet ef database update`.
2. `dotnet build` → aucune erreur.
3. Se connecter, aller sur "Notes" → la liste s'affiche (vide au départ).
4. Attribuer une note à un étudiant existant, avec une valeur décimale (ex: `14.5`) → doit s'enregistrer sans erreur de format.
5. Essayer une note de `25` → doit afficher "La note doit être comprise entre 0 et 20."
6. Utiliser le filtre "Consulter les notes de" → vérifier que seules les notes du bon étudiant s'affichent, avec la moyenne pondérée.
7. Ajouter une deuxième note (matière différente, coefficient différent) au même étudiant → vérifier que la moyenne se recalcule correctement.
8. Modifier puis supprimer une note → vérifier la mise à jour de la liste et du Dashboard.

## 9. Commits Git à faire

```bash
git checkout -b feature/grades-cecile

git add Models/Grade.cs
git commit -m "feat(grades): completion du modele Grade avec validations"

git add Migrations/
git commit -m "feat(grades): migration EF pour DateEvaluation"

git add Program.cs
git commit -m "fix(culture): forcer la culture en-US pour la conversion des nombres decimaux"

git add Controllers/GradesController.cs
git commit -m "feat(grades): ajout du CRUD notes avec filtre par etudiant et moyenne ponderee"

git add Views/Grades/
git commit -m "feat(grades): vues modernes pour la gestion des notes"

git push -u origin feature/grades-cecile
```
Ensuite, ouvre une Pull Request sur GitHub pour fusionner dans `main`.

## 10. Points à savoir répondre au professeur

- **« Pourquoi la moyenne est-elle "pondérée" et pas juste une moyenne simple ? »** → Parce que toutes les matières n'ont pas la même importance (leur `Coefficient`). Une moyenne simple traiterait une note de maths et une note de sport de la même façon, ce qui ne correspond pas à la réalité scolaire.
- **« Pourquoi une seule action `Index` pour la liste complète et la consultation par étudiant ? »** → Pour éviter de dupliquer le code d'affichage (le tableau, les `.Include`, etc.). Un simple paramètre optionnel (`int? studentId`) suffit à couvrir les deux besoins du cahier des charges.
- **« Qu'est-ce que le bug de culture, et pourquoi ça concernait ton module en particulier ? »** → C'est un problème global (n'importe quel nombre décimal dans l'application aurait pu être affecté, y compris le `Coefficient` des matières), mais c'est en testant l'ajout d'une note avec une décimale qu'on l'a découvert, donc c'est corrigé et documenté à cette étape.
- **« Que se passe-t-il si je supprime un étudiant ou une matière qui a des notes ? »** → Comme expliqué par Dougouré à l'étape 4, la suppression en cascade s'applique aussi ici : supprimer un étudiant ou une matière supprime automatiquement ses notes associées.
