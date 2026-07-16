using System.Collections.Generic;

namespace GestionScolaire.Models
{
    // Version minimale : Dougouré complètera ce modèle à l'étape 4 (Gestion des classes).
    public class ClassRoom
    {
        public int Id { get; set; }

        public string Nom { get; set; } = string.Empty;

        // Liste des étudiants de cette classe (une classe contient plusieurs étudiants)
        public List<Student>? Students { get; set; }
    }
}
