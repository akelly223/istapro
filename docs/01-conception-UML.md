# Conception UML — Application de Gestion Scolaire

> Projet étudiant ASP.NET MVC — Kelly, Koita, Dougouré, Cécile
> Niveau : débutant (≈12h de cours ASP.NET) — code volontairement simple.

## Sommaire

1. Diagramme de cas d'utilisation
2. Diagramme de classes
3. Diagrammes de séquence (Connexion, Ajout étudiant, Ajout note)
4. MCD (Merise)
5. Modèle relationnel
6. Répartition du travail et organisation Git (anti-conflits)

---

## 1. Diagramme de cas d'utilisation

Un seul acteur : **Administrateur** (la seule personne qui utilise l'application, avec un compte protégé par ASP.NET Identity).

```mermaid
graph LR
    Admin["🧑‍💼 Administrateur"]

    UC1(["Se connecter"])
    UC2(["Se déconnecter"])
    UC3(["Ajouter étudiant"])
    UC4(["Modifier étudiant"])
    UC5(["Supprimer étudiant"])
    UC6(["Gérer les classes"])
    UC7(["Gérer les matières"])
    UC8(["Attribuer les notes"])
    UC9(["Consulter les notes"])

    Admin --> UC1
    Admin --> UC2
    Admin --> UC3
    Admin --> UC4
    Admin --> UC5
    Admin --> UC6
    Admin --> UC7
    Admin --> UC8
    Admin --> UC9
```

**Remarque pédagogique (à dire au professeur) :** tous les cas d'usage sauf « Se connecter » nécessitent implicitement d'être authentifié (relation `<<include>>` vers « Se connecter »). On ne le dessine pas pour ne pas surcharger le schéma, mais techniquement cela correspond à l'attribut `[Authorize]` posé sur les contrôleurs.

---

## 2. Diagramme de classes

```mermaid
classDiagram
    class User {
        +string Id
        +string UserName
        +string Email
        +string PasswordHash
    }

    class ClassRoom {
        +int Id
        +string Nom
        +string Niveau
    }

    class Student {
        +int Id
        +string Nom
        +string Prenom
        +DateTime DateNaissance
        +string Email
        +int ClassRoomId
    }

    class Subject {
        +int Id
        +string Nom
        +int Coefficient
    }

    class Grade {
        +int Id
        +int StudentId
        +int SubjectId
        +double Valeur
        +DateTime DateEvaluation
    }

    ClassRoom "1" --> "0..*" Student : contient
    Student "1" --> "0..*" Grade : possède
    Subject "1" --> "0..*" Grade : concerne
```

**Notes :**
- `User` correspond au compte Identity (table `AspNetUsers`, gérée automatiquement par ASP.NET Identity). Il n'a pas de relation directe avec les autres classes : c'est juste le compte de connexion de l'administrateur.
- `Grade` est la classe association entre `Student` et `Subject` : chaque note relie un étudiant à une matière.
- Une classe (`ClassRoom`) contient plusieurs étudiants, mais un étudiant appartient à une seule classe.

---

## 3. Diagrammes de séquence

### 3.1 Connexion

```mermaid
sequenceDiagram
    actor Admin as Administrateur
    participant Vue as Vue Login
    participant Ctrl as AccountController
    participant Identity as SignInManager (Identity)
    participant DB as Base de données

    Admin->>Vue: Saisit email + mot de passe
    Vue->>Ctrl: POST /Account/Login
    Ctrl->>Identity: PasswordSignInAsync(email, motDePasse)
    Identity->>DB: Vérifie l'utilisateur et le mot de passe
    DB-->>Identity: Utilisateur valide
    Identity-->>Ctrl: Résultat = Succeeded (crée le cookie d'authentification)
    Ctrl-->>Vue: Redirect vers /Home/Dashboard
    Vue-->>Admin: Affiche le tableau de bord
```

### 3.2 Ajout d'un étudiant

```mermaid
sequenceDiagram
    actor Admin as Administrateur
    participant Vue as Vue Create (Students)
    participant Ctrl as StudentsController
    participant DB as AppDbContext / Base de données

    Admin->>Vue: Remplit le formulaire (Nom, Prénom, Classe...)
    Vue->>Ctrl: POST /Students/Create
    Ctrl->>Ctrl: Vérifie ModelState.IsValid
    Ctrl->>DB: _context.Students.Add(student)
    Ctrl->>DB: _context.SaveChanges()
    DB-->>Ctrl: Confirmation d'enregistrement
    Ctrl-->>Vue: Redirect vers /Students (liste)
    Vue-->>Admin: Affiche le nouvel étudiant dans le tableau
```

### 3.3 Attribution d'une note

```mermaid
sequenceDiagram
    actor Admin as Administrateur
    participant Vue as Vue Create (Grades)
    participant Ctrl as GradesController
    participant DB as AppDbContext / Base de données

    Admin->>Vue: Choisit étudiant, matière, saisit la note
    Vue->>Ctrl: POST /Grades/Create
    Ctrl->>Ctrl: Vérifie ModelState.IsValid
    Ctrl->>DB: _context.Grades.Add(grade)
    Ctrl->>DB: _context.SaveChanges()
    DB-->>Ctrl: Confirmation d'enregistrement
    Ctrl-->>Vue: Redirect vers /Grades (liste)
    Vue-->>Admin: Affiche la note enregistrée
```

---

## 4. MCD (Modèle Conceptuel de Données — Merise)

```mermaid
erDiagram
    CLASSE ||--o{ ETUDIANT : "contient (1,n)"
    ETUDIANT ||--o{ NOTE : "possède (1,n)"
    MATIERE ||--o{ NOTE : "concerne (1,n)"

    CLASSE {
        int Id
        string Nom
        string Niveau
    }
    ETUDIANT {
        int Id
        string Nom
        string Prenom
        date DateNaissance
        string Email
    }
    MATIERE {
        int Id
        string Nom
        int Coefficient
    }
    NOTE {
        int Id
        float Valeur
        date DateEvaluation
    }
```

**Lecture des cardinalités :**
- Une **Classe** contient 0 à n **Étudiants** ; un **Étudiant** appartient à exactement 1 **Classe**.
- Un **Étudiant** possède 0 à n **Notes** ; une **Note** concerne exactement 1 **Étudiant**.
- Une **Matière** concerne 0 à n **Notes** ; une **Note** concerne exactement 1 **Matière**.
- `NOTE` est l'entité associative qui matérialise la relation n-n conceptuelle entre `ETUDIANT` et `MATIERE`.

---

## 5. Modèle relationnel

Notation Merise classique : `Table(clé primaire soulignée, ..., #clé étrangère)`

```
ClassRooms ( Id, Nom, Niveau )

Students ( Id, Nom, Prenom, DateNaissance, Email, #ClassRoomId )

Subjects ( Id, Nom, Coefficient )

Grades ( Id, Valeur, DateEvaluation, #StudentId, #SubjectId )

AspNetUsers ( Id, UserName, Email, PasswordHash, ... )   -- généré automatiquement par ASP.NET Identity
```

Contraintes de clé étrangère :
- `Students.ClassRoomId` → `ClassRooms.Id`
- `Grades.StudentId` → `Students.Id`
- `Grades.SubjectId` → `Subjects.Id`

---

## 6. Répartition du travail et organisation Git (anti-conflits)

### 6.1 Tableau récapitulatif

| Étudiant  | Responsabilité                  | Fichiers touchés (uniquement)                                                                 |
|-----------|----------------------------------|-------------------------------------------------------------------------------------------------|
| Kelly     | Auth, Layout, Dashboard          | `Program.cs`, `Data/AppDbContext.cs`, `Controllers/AccountController.cs`, `Controllers/HomeController.cs`, `Views/Shared/_Layout.cshtml`, `Views/Home/*`, `Views/Account/*` |
| Koita     | Gestion des étudiants (CRUD)     | `Models/Student.cs`, `Controllers/StudentsController.cs`, `Views/Students/*`                    |
| Dougouré  | Gestion des classes et matières  | `Models/ClassRoom.cs`, `Models/Subject.cs`, `Controllers/ClassRoomsController.cs`, `Controllers/SubjectsController.cs`, `Views/ClassRooms/*`, `Views/Subjects/*` |
| Cécile    | Gestion des notes                | `Models/Grade.cs`, `Controllers/GradesController.cs`, `Views/Grades/*`                          |

### 6.2 Pourquoi les conflits Git seront presque inexistants

Deux fichiers sont "partagés" par nature dans une appli MVC : `Data/AppDbContext.cs` (liste des `DbSet<>`) et `Views/Shared/_Layout.cshtml` (menu latéral). Pour éviter que tout le monde y touche :

- **Étape 1 (Architecture, Kelly)** crée d'un coup :
  - les 4 classes modèles **en version minimale** (`Student`, `ClassRoom`, `Subject`, `Grade` avec juste `Id` + 1-2 champs de base) ;
  - `AppDbContext` avec les 4 `DbSet<>` déjà déclarés ;
  - la sidebar complète du `_Layout.cshtml` avec **tous** les liens de menu (Dashboard, Étudiants, Classes, Matières, Notes), même si les pages n'existent pas encore.
- Ensuite, **chaque étudiant complète uniquement son propre modèle** (ajoute ses champs, ses validations) et crée son contrôleur + ses vues. Il ne touche plus jamais `AppDbContext.cs` ni `_Layout.cshtml`.
- Résultat : Koita, Dougouré et Cécile ne modifient jamais un fichier que quelqu'un d'autre modifie aussi.

### 6.3 Organisation des branches

```
main
 ├─ feature/auth-kelly          (Étapes 1 et 2)
 ├─ feature/students-koita      (Étape 3)
 ├─ feature/classes-subjects-dougoure   (Étapes 4 et 5)
 └─ feature/grades-cecile       (Étape 6)
```

Règle : chaque branche est fusionnée (Pull Request) dans `main` **avant** que la branche suivante ne démarre, dans l'ordre des étapes. Comme cet ordre correspond exactement à l'ordre de génération du projet (Étape 1 → 6), il n'y a jamais deux personnes qui modifient les fichiers partagés en même temps.

### 6.4 Convention de messages de commit

Format : `type(portée): description courte`

Exemples :
- `feat(auth): ajout de la page de connexion`
- `feat(students): ajout du CRUD étudiant`
- `feat(classes): ajout du CRUD classes`
- `feat(grades): ajout de l'attribution des notes`
- `fix(students): correction de la validation email`

---

## Prochaine étape

Conception validée. Prête à démarrer l'**Étape 1 : Architecture du projet** (solution ASP.NET MVC, `Program.cs`, `AppDbContext`, modèles minimaux, layout + sidebar, connexion à la base de données) dès votre feu vert.
