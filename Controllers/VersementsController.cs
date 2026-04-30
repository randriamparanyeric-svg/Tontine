using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tontine.Data;
using Tontine.Models;
using Tontine.Services; // Ajout indispensable pour reconnaître ISmsService

namespace Tontine.Controllers
{ 
    public class VersementsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ISmsService _smsService; // 1. Déclaration du service SMS

        // 2. Le constructeur reçoit maintenant le service SMS en plus du contexte
        public VersementsController(ApplicationDbContext context, ISmsService smsService)
        {
            _context = context;
            _smsService = smsService; // 3. Liaison
        }

        // 📋 Liste des versements
        public async Task<IActionResult> Index()
        {
            var versements = await _context.Versements
                .Include(v => v.Membre)
                .Include(v => v.Groupe)
                .ToListAsync();

            return View(versements);
        }

        // ➕ Ajouter versement (MEMBRE)
        public async Task<IActionResult> Create(int membreId)
        {
            var membre = await _context.Membres
                .Include(m => m.Groupe)
                .FirstOrDefaultAsync(m => m.Id == membreId);

            if (membre == null) return NotFound();

            var versement = new Versement
            {
                MembreId = membreId,
                Montant = membre.Groupe?.MontantParVersement ?? 0,
                GroupeId = membre.GroupeId,
                Date = DateTime.Now,
                Statut = "En attente",
                Membre = membre
            };

            return View(versement);
        }

        // ➕ Enregistrement du versement (MEMBRE)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Versement versement)
        {
            if (ModelState.IsValid)
            {
                versement.Date = DateTime.Now;
                versement.Statut = "En attente"; 

                _context.Add(versement);
                await _context.SaveChangesAsync();

                var groupe = await _context.Groupes.FindAsync(versement.GroupeId);
                
                // --- ENVOI SMS ---
                var membre = await _context.Membres.FindAsync(versement.MembreId);
                if (membre != null)
                {
                    var message = $"Bonjour {membre.Nom}, votre versement de {versement.Montant} Ar a été enregistré et est en attente de confirmation.";
                    // L'appel fonctionne maintenant car _smsService est déclaré plus haut
                    try {
                    await _smsService.EnvoyerSmsAsync(membre.Telephone, message);
                    // AJOUT DE LA CONSOLE ICI :
                    } catch (Exception ex) {
    Console.WriteLine("⚠️ Erreur réseau : Le téléphone est injoignable. " + ex.Message);
}
    Console.WriteLine("================================================");
    Console.WriteLine($"[DEBUG] Tentative d'envoi SMS à : {membre.Telephone}");
    Console.WriteLine($"[DEBUG] Contenu : {message}");
    Console.WriteLine("================================================");
                }
                // -----------------

                return RedirectToAction("VoirCommeMembe", "Groupes", new { codePartage = groupe?.CodePartage });
            }

            var membreData = await _context.Membres
                .Include(m => m.Groupe)
                .FirstOrDefaultAsync(m => m.Id == versement.MembreId);

            versement.Membre = membreData;
            return View(versement);
        }

        // ✅ Confirmer versement (ADMIN ONLY)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirmer(int id)
        {
            var versement = await _context.Versements
                .Include(v => v.Membre)
                .Include(v => v.Groupe)
                .FirstOrDefaultAsync(v => v.Id == id);

            if (versement == null) return NotFound();

            var isAdmin = HttpContext.Session.GetString($"admin_groupe_{versement.Groupe.Id}") == "true";
            if (!isAdmin)
                return Unauthorized("Seul l'admin peut confirmer les versements");

            versement.Statut = "Confirmé";

            var membre = versement.Membre;
            if (membre != null)
            {
                membre.Solde += versement.Montant;
                _context.Update(membre);
                
                // Optionnel : Envoyer aussi un SMS ici pour dire que c'est validé !
                await _smsService.EnvoyerSmsAsync(membre.Telephone, $"✅ Versement de {versement.Montant} Ar confirmé ! Votre nouveau solde est de {membre.Solde} Ar.");
            }

            _context.Update(versement);
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", "Groupes", new { id = versement.Groupe.Id });
        }

        // ❌ Rejeter versement (ADMIN ONLY)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Rejeter(int id)
        {
            var versement = await _context.Versements
                .Include(v => v.Groupe)
                .FirstOrDefaultAsync(v => v.Id == id);

            if (versement == null) return NotFound();

            var isAdmin = HttpContext.Session.GetString($"admin_groupe_{versement.Groupe.Id}") == "true";
            if (!isAdmin)
                return Unauthorized("Seul l'admin peut rejeter les versements");

            versement.Statut = "Rejeté";
            _context.Update(versement);
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", "Groupes", new { id = versement.Groupe.Id });
        }
    }
}