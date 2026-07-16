using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace GestionScolaire.Models
{
    // Un devoir assigné par un professeur à toute une classe, pour une matière donnée.
    public class Homework
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Le titre est obligatoire.")]
        [StringLength(100, ErrorMessage = "Le titre ne doit pas dépasser 100 caractères.")]
        [Display(Name = "Titre")]
        public string Titre { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "La description ne doit pas dépasser 500 caractères.")]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "La matière est obligatoire.")]
        [Display(Name = "Matière")]
        public int SubjectId { get; set; }
        public Subject? Subject { get; set; }

        [Required(ErrorMessage = "La classe est obligatoire.")]
        [Display(Name = "Classe")]
        public int ClassRoomId { get; set; }
        public ClassRoom? ClassRoom { get; set; }

        [Required(ErrorMessage = "La date limite est obligatoire.")]
        [DataType(DataType.Date)]
        [Display(Name = "Date limite")]
        public DateTime DateLimite { get; set; }

        public DateTime DateCreation { get; set; } = DateTime.Now;

        // Rendus envoyés par les étudiants pour ce devoir
        public List<HomeworkSubmission>? Soumissions { get; set; }
    }
}
