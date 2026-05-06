using System;
using System.ComponentModel.DataAnnotations;

namespace Tontine.Models
{
    public class Retrait
    {
        public int Id { get; set; }
        
        [Required]
        public int MembreId { get; set; }
        public Membre? Membre { get; set; }

        [Required]
        [Display(Name = "Montant demandé")]
        public decimal Montant { get; set; }

        public DateTime DateDemande { get; set; } = DateTime.Now;

        [Required]
        public string Statut { get; set; } = "En attente"; // En attente, Confirmé, Rejeté

        [Display(Name = "Commentaire / Motif")]
        public string? Commentaire { get; set; }
    }
}