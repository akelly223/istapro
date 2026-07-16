# Fiche d'explication — Dougouré — Étape 4 : Gestion des classes et matières

Cette fiche te sert à défendre ton code devant le professeur. Elle couvre les **deux** modules dont tu es responsable : Classes et Matières. Ils sont très similaires, donc les explications communes ne sont pas répétées deux fois.

## 1. Objectif de l'étape

Réaliser le CRUD complet des classes (`ClassRoom`) et des matières (`Subject`) : liste, ajout, modification, suppression.

## 2. Fichiers modifiés

| Fichier | Rôle |
|---|---|
| `Models/ClassRoom.cs` | Modèle complété avec `Niveau` et les validations |
| `Models/Subject.cs` | Modèle complété avec `Coefficient` et les validations |

## 3. Fichiers créés

| Fichier | Rôle |
|---|---|
| `Controllers/ClassRoomsController.cs` | CRUD des classes |
| `Controllers/SubjectsController.cs` | CRUD des matières |
| `Views/ClassRooms/*.cshtml` | Liste, ajout, modification, suppression des classes |
| `Views/Subjects/*.cshtml` | Liste, ajout, modification, suppression des matières |
| `Migrations/..._AjoutDetailsClasseMatiere.cs` | Migration EF pour les nouveaux champs |

Comme pour Koita à l'étape précédente, tu n'as touché ni `AppDbContext.cs` ni `_Layout.cshtml` : tout était déjà prêt depuis l'étape 1.

## 4. Les modèles complétés

```csharp
public class ClassRoom
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Le nom de la classe est obligatoire.")]
    [StringLength(50, ErrorMessage = "Le nom ne doit pas dépasser 50 caractères.")]
    [Display(Name = "Nom de la classe")]
    public string Nom { get; set; } = string.Empty;

    [Required(ErrorMessage = "Le niveau est obligatoire.")]
    [StringLength(30, ErrorMessage = "Le niveau ne doit pas dépasser 30 caractères.")]
    [Display(Name = "Niveau")]
    public string Niveau { get; set; } = string.Empty;

    public List<Student>? Students { get; set; }
}
```

```csharp
public class Subject
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Le nom de la matière est obligatoire.")]
    [StringLength(50, ErrorMessage = "Le nom ne doit pas dépasser 50 caractères.")]
    [Display(Name = "Nom de la matière")]
    public string Nom { get; set; } = string.Empty;

    [Required(ErrorMessage = "Le coefficient est obligatoire.")]
    [Range(1, 10, ErrorMessage = "Le coefficient doit être compris entre 1 et 10.")]
    [Display(Name = "Coefficient")]
    public int Coefficient { get; set; }
}
```

`[Range(1, 10)]` est un nouvel attribut de validation (par rapport à ceux vus par Koita) : il vérifie qu'un nombre reste dans un intervalle. Ici, un coefficient de matière n'a pas de sens s'il est négatif ou s'il vaut 500.

## 5. La migration EF Core

```bash
dotnet ef migrations add AjoutDetailsClasseMatiere
dotnet ef database update
```

Cette migration ajoute deux colonnes à des tables qui existent déjà (`ClassRooms.Niveau` et `Subjects.Coefficient`), exactement comme la migration de Koita à l'étape 3. Comme il y avait déjà des classes de test dans la base, EF Core leur donne une valeur par défaut (`''` pour le texte) le temps qu'on les modifie manuellement ou via l'interface.

## 6. Le point important de cette étape : la suppression en cascade

C'est la partie la plus intéressante à expliquer au professeur.

### 6.1 Le problème

Une classe (`ClassRoom`) peut contenir des étudiants (`Student.ClassRoomId`). Une matière (`Subject`) peut avoir des notes (`Grade.SubjectId`). Ces relations sont **obligatoires** (`ClassRoomId` et `SubjectId` ne peuvent pas être `null` dans les modèles). Du coup, Entity Framework configure **par défaut** la suppression en cascade (`ON DELETE CASCADE`) sur ce type de relation :

> Si on supprime une classe, **tous ses étudiants sont automatiquement supprimés aussi** (et donc toutes leurs notes, par cascade sur `Grade`). Pareil pour une matière : la supprimer supprime aussi toutes les notes qui la concernent.

Ça se voit dans la migration `InitialCreate` (étape 1) :
```csharp
table.ForeignKey(
    name: "FK_Students_ClassRooms_ClassRoomId",
    column: x => x.ClassRoomId,
    principalTable: "ClassRooms",
    principalColumn: "Id",
    onDelete: ReferentialAction.Cascade);
```

### 6.2 La solution : avertir avant de supprimer

On ne bloque pas la suppression (ça resterait simple à comprendre), mais on **avertit clairement** l'administrateur avant qu'il confirme, en comptant les éléments concernés :

```csharp
// GET: /ClassRooms/Delete/5
public async Task<IActionResult> Delete(int id)
{
    var classe = await _context.ClassRooms
        .Include(c => c.Students)
        .FirstOrDefaultAsync(c => c.Id == id);
    ...
    return View(classe);
}
```
`.Include(c => c.Students)` charge la liste des étudiants de la classe en même temps que la classe elle-même. Dans la vue, `Model.Students?.Count ?? 0` affiche ce nombre, et un message d'avertissement différent s'affiche selon que ce nombre est `> 0` ou non :

```csharp
@if (nombreEtudiants > 0)
{
    <span><strong>Cette classe contient @nombreEtudiants étudiant(s)</strong> : ils seront eux aussi supprimés, ainsi que toutes leurs notes.</span>
}
```

Pour les matières, le principe est le même mais avec un simple comptage (pas besoin de propriété de navigation) :
```csharp
ViewBag.NombreNotes = await _context.Grades.CountAsync(g => g.SubjectId == id);
```

**Piège rencontré en la construisant** : à l'intérieur d'un bloc Razor `@if { ... }`, il faut que le texte HTML reste **dans une balise** (ici `<span>...</span>`). Un texte "nu" juste après une balise fermante (`</strong> : ils seront...`) est interprété par erreur comme du code C#, ce qui casse la compilation. C'est une subtilité de Razor à connaître : dans un bloc de code (`@if`, `@foreach`, etc.), tout texte affiché doit être entouré d'une balise HTML (ou de `<text>...</text>` s'il n'y a pas de balise naturelle).

## 7. Les contrôleurs

`ClassRoomsController` et `SubjectsController` suivent exactement le même schéma que `StudentsController` de Koita (`[Authorize]`, Index/Create/Edit/Delete, Post/Redirect/Get, `TempData["Message"]`, `ValidateAntiForgeryToken`). Une différence notable pour la liste des classes :

```csharp
public async Task<IActionResult> Index()
{
    var classes = await _context.ClassRooms
        .Include(c => c.Students)
        .OrderBy(c => c.Nom)
        .ToListAsync();

    return View(classes);
}
```
On charge aussi `Students` ici, pour pouvoir afficher le nombre d'étudiants de chaque classe directement dans le tableau, sans requête supplémentaire par ligne.

## 8. Captures attendues

1. La liste des classes avec la colonne "Étudiants" montrant le bon nombre pour chacune.
2. La liste des matières avec les coefficients.
3. Le formulaire d'ajout d'une classe ou d'une matière.
4. La page de suppression d'une classe **contenant des étudiants**, montrant le message d'avertissement rouge.
5. La page de suppression d'une classe **vide**, montrant le message neutre.

## 9. Étapes de test

1. `dotnet ef migrations add AjoutDetailsClasseMatiere` puis `dotnet ef database update`.
2. `dotnet build` → aucune erreur.
3. Se connecter, aller sur "Classes" → ajouter une classe, la modifier, vérifier la liste.
4. Aller sur "Matières" → ajouter une matière avec un coefficient, essayer un coefficient de `0` ou `15` → doit afficher une erreur de validation (`[Range(1,10)]`).
5. Créer un étudiant dans une classe (module de Koita), puis revenir sur "Classes" → vérifier que le compteur d'étudiants de cette classe a augmenté.
6. Essayer de supprimer cette classe → vérifier que le message d'avertissement mentionne le bon nombre d'étudiants concernés.
7. Vérifier sur le Dashboard que les compteurs "Classes" et "Matières" reflètent bien la base de données.

## 10. Commits Git à faire

```bash
git checkout -b feature/classes-subjects-dougoure

git add Models/ClassRoom.cs Models/Subject.cs
git commit -m "feat(classes): completion des modeles ClassRoom et Subject"

git add Migrations/
git commit -m "feat(classes): migration EF pour Niveau et Coefficient"

git add Controllers/ClassRoomsController.cs Controllers/SubjectsController.cs
git commit -m "feat(classes): ajout du CRUD classes et matieres"

git add Views/ClassRooms/ Views/Subjects/
git commit -m "feat(classes): vues modernes pour classes et matieres"

git push -u origin feature/classes-subjects-dougoure
```
Ensuite, ouvre une Pull Request sur GitHub pour fusionner dans `main`.

## 11. Points à savoir répondre au professeur

- **« Que se passe-t-il si je supprime une classe qui a des étudiants ? »** → Grâce à la contrainte `ON DELETE CASCADE` configurée automatiquement par Entity Framework (parce que `ClassRoomId` est obligatoire sur `Student`), la base de données supprime aussi les étudiants de cette classe, et donc leurs notes. On prévient l'administrateur de cette conséquence sur la page de confirmation avant qu'il ne valide.
- **« Pourquoi ne pas simplement empêcher la suppression si la classe n'est pas vide ? »** → C'est un choix de conception possible et plus prudent, mais on a choisi la solution la plus simple à ce niveau (avertir plutôt que bloquer), qui reste cohérente avec le principe "code simple" du projet. On pourrait le faire évoluer plus tard.
- **« Pourquoi Subject n'a pas de propriété de navigation vers Grade ? »** → Parce qu'on n'en a pas eu besoin : un simple `_context.Grades.CountAsync(...)` suffit pour compter les notes liées, sans avoir à ajouter de code au modèle `Subject`.
- **« Comment le nombre d'étudiants par classe s'affiche-t-il sans requête supplémentaire ? »** → Grâce à `.Include(c => c.Students)` dans la requête `Index()`, qui charge la liste des étudiants de chaque classe en une seule requête SQL (avec une jointure), plutôt que de faire une requête par classe.
