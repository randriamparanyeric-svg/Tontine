using System.ComponentModel.DataAnnotations;

namespace Tontine.Models
{
    public class Versement
    {
        public int Id { get; set; }

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal Montant { get; set; }

        public DateTime Date { get; set; } = DateTime.Now;

        [StringLength(50)]
        public string? Statut { get; set; } = "Confirmé"; // Confirmé, En attente, Rejeté

        // Relations
        public int MembreId { get; set; }
        public Membre? Membre { get; set; }

        public int GroupeId { get; set; }
        public Groupe? Groupe { get; set; }
    }
}