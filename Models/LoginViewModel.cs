using System.ComponentModel.DataAnnotations;

namespace GestionScolaire.Models
{
    // Ce modèle ne correspond à aucune table : il sert uniquement à transporter
    // les données du formulaire de connexion entre la vue et le contrôleur.
    public class LoginViewModel
    {
        [Required(ErrorMessage = "L'email est obligatoire.")]
        [EmailAddress(ErrorMessage = "Le format de l'email n'est pas valide.")]
        [Display(Name = "Adresse email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le mot de passe est obligatoire.")]
        [DataType(DataType.Password)]
        [Display(Name = "Mot de passe")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Se souvenir de moi")]
        public bool RememberMe { get; set; }
    }
}
