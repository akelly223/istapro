using System.ComponentModel.DataAnnotations;

namespace GestionScolaire.Models
{
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
}
