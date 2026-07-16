namespace GestionScolaire.Models
{
    // Version minimale : Koita complètera ce modèle à l'étape 3 (Gestion des étudiants)
    // en ajoutant DateNaissance, Email et les validations.
    public class Student
    {
        public int Id { get; set; }

        public string Nom { get; set; } = string.Empty;

        public string Prenom { get; set; } = string.Empty;

        // Clé étrangère : un étudiant appartient à une seule classe
        public int ClassRoomId { get; set; }

        // Propriété de navigation vers la classe de l'étudiant
        public ClassRoom? ClassRoom { get; set; }
    }
}
