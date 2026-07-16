using System;
using System.ComponentModel.DataAnnotations;

namespace GestionScolaire.Models
{
    // Présence (ou absence) d'un étudiant à une date donnée, relevée par le professeur.
    public class Attendance
    {
        public int Id { get; set; }

        [Required]
        public int StudentId { get; set; }
        public Student? Student { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Date")]
        public DateTime DateSeance { get; set; }

        [Display(Name = "Présent")]
        public bool EstPresent { get; set; }
    }
}
