using System;
using System.ComponentModel.DataAnnotations;

namespace GestionScolaire.Models
{
    // Le fichier envoyé par un étudiant pour répondre à un devoir donné.
    public class HomeworkSubmission
    {
        public int Id { get; set; }

        [Required]
        public int HomeworkId { get; set; }
        public Homework? Homework { get; set; }

        [Required]
        public int StudentId { get; set; }
        public Student? Student { get; set; }

        public DateTime DateEnvoi { get; set; } = DateTime.Now;

        // Nom du fichier tel qu'il est enregistré sur le serveur (unique, pour éviter les doublons).
        [Required]
        public string NomFichier { get; set; } = string.Empty;

        // Nom d'origine du fichier choisi par l'étudiant, pour l'afficher lors du téléchargement.
        [Required]
        public string NomFichierOriginal { get; set; } = string.Empty;
    }
}
