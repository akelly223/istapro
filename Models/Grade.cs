using System;
using System.ComponentModel.DataAnnotations;

namespace GestionScolaire.Models
{
    public class Grade
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "L'étudiant est obligatoire.")]
        [Display(Name = "Étudiant")]
        public int StudentId { get; set; }

        [Required(ErrorMessage = "La matière est obligatoire.")]
        [Display(Name = "Matière")]
        public int SubjectId { get; set; }

        [Required(ErrorMessage = "La note est obligatoire.")]
        [Range(0, 20, ErrorMessage = "La note doit être comprise entre 0 et 20.")]
        [Display(Name = "Note")]
        public double Valeur { get; set; }

        [Required(ErrorMessage = "La date d'évaluation est obligatoire.")]
        [DataType(DataType.Date)]
        [Display(Name = "Date d'évaluation")]
        public DateTime DateEvaluation { get; set; }

        // Propriétés de navigation
        public Student? Student { get; set; }

        public Subject? Subject { get; set; }
    }
}
