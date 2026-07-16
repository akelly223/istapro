using System;
using System.ComponentModel.DataAnnotations;

namespace GestionScolaire.Models
{
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

        // Clé étrangère : un étudiant appartient à une seule classe
        [Required(ErrorMessage = "La classe est obligatoire.")]
        [Display(Name = "Classe")]
        public int ClassRoomId { get; set; }

        // Propriété de navigation vers la classe de l'étudiant
        public ClassRoom? ClassRoom { get; set; }

        // Identifiant du compte Identity (AspNetUsers.Id) créé automatiquement pour cet étudiant,
        // afin qu'il puisse se connecter à son propre espace. Null tant que le compte n'est pas créé.
        public string? UserId { get; set; }
    }
}
