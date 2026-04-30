using System.ComponentModel.DataAnnotations;

namespace Tontine.Models
{
    public class Groupe
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Le nom du groupe est requis")]
        [StringLength(100)]
        public string Nom { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        // 🔐 Sécurité - Mot de passe HASHÉ
        [Required(ErrorMessage = "Le mot de passe est requis")]
        [StringLength(255)]
        public string MotDePasseHash { get; set; } = string.Empty;

        // 👤 Admin/Trésorier
        [Required(ErrorMessage = "Le nom de l'admin est requis")]
        [StringLength(100)]
        public string NomAdmin { get; set; } = string.Empty;

        [StringLength(20)]
        public string? TelephoneAdmin { get; set; }

        // 🔗 Lien de partage unique
        [StringLength(50)]
        public string CodePartage { get; set; } = string.Empty;

        // 💰 Tontine spécifique
        [Required(ErrorMessage = "Le montant par versement est requis")]
        [Range(0.01, double.MaxValue)]
        public decimal MontantParVersement { get; set; }

        [Required(ErrorMessage = "Le nombre de membres est requis")]
        [Range(1, 1000)]
        public int NombreMembresPrevu { get; set; }

        // 📅 Statut
        public DateTime DateCreation { get; set; } = DateTime.Now;
        public bool Actif { get; set; } = true;

        // Relations
        public List<Membre> Membres { get; set; } = new();
        public List<Versement> Versements { get; set; } = new();
    }
}