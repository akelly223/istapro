using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace GestionScolaire.Models
{
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

        // Liste des étudiants de cette classe (une classe contient plusieurs étudiants)
        public List<Student>? Students { get; set; }
    }
}
