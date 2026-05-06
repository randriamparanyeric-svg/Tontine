using System.ComponentModel.DataAnnotations;

namespace Tontine.Models
{
    public class Membre
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Le nom est requis")]
        [StringLength(100)]
        public string Nom { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le téléphone est requis")]
        [StringLength(20)]
        public string Telephone { get; set; } = string.Empty;

        [EmailAddress]
        [StringLength(100)]
        public string? Email { get; set; }

        public decimal Solde { get; set; } = 0;

        // Liaison au groupe
        public int GroupeId { get; set; }
        public Groupe? Groupe { get; set; }

        // Statut du membre
        public bool Actif { get; set; } = true;
        public DateTime DateAdhesion { get; set; } = DateTime.Now;

        public string? CinRecto { get; set; }
        public string? CinVerso { get; set; }

        // Relations
        public List<Versement> Versements { get; set; } = new();
        public virtual ICollection<Retrait> Retraits { get; set; } = new List<Retrait>();
    }
}