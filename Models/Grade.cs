namespace GestionScolaire.Models
{
    // Version minimale : Cécile complètera ce modèle à l'étape 6 (Gestion des notes)
    // en ajoutant DateEvaluation et les validations (note entre 0 et 20).
    public class Grade
    {
        public int Id { get; set; }

        // Clé étrangère vers l'étudiant qui reçoit la note
        public int StudentId { get; set; }

        // Clé étrangère vers la matière concernée par la note
        public int SubjectId { get; set; }

        public double Valeur { get; set; }

        // Propriétés de navigation
        public Student? Student { get; set; }

        public Subject? Subject { get; set; }
    }
}
