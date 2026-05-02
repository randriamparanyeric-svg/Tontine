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

        // --- AUTHENTIFICATION ---

        [Required(ErrorMessage = "L'email de l'admin est requis")]
        [EmailAddress(ErrorMessage = "Format d'email invalide")]
        [StringLength(255)]
        public string AdminEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le mot de passe est requis")]
        [StringLength(255)]
        public string MotDePasseHash { get; set; } = string.Empty;

        // --- ADMIN / TRÉSORIER ---

        [Required(ErrorMessage = "Le nom de l'admin est requis")]
        [StringLength(100)]
        public string NomAdmin { get; set; } = string.Empty;

        [StringLength(20)]
        public string? TelephoneAdmin { get; set; }

        // --- VERROU DÉVELOPPEUR (VOTRE POUVOIR) ---

        // On renomme 'Actif' en 'EstValideParDev' pour plus de clarté
        // Par défaut c'est FALSE : le carnet est bloqué à la création
        public bool EstValideParDev { get; set; } = false; 

        // --- SÉCURITÉ / RÉCUPÉRATION ---

        public string? ResetToken { get; set; }
        public DateTime? ResetTokenExpiry { get; set; }

        // --- CONFIGURATION TONTINE ---

        [StringLength(50)]
        public string CodePartage { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le montant par versement est requis")]
        [Range(0.01, double.MaxValue)]
        public decimal MontantParVersement { get; set; }

        [Required(ErrorMessage = "Le nombre de membres est requis")]
        [Range(1, 1000)]
        public int NombreMembresPrevu { get; set; }

        // --- STATUT ET RELATIONS ---

        public DateTime DateCreation { get; set; } = DateTime.Now;
        
        public List<Membre> Membres { get; set; } = new();
        public List<Versement> Versements { get; set; } = new();
    }
}