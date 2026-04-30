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
       private readonly IEmailService _emailService; // Changement ici

    public VersementsController(ApplicationDbContext context, IEmailService emailService)
    {
        _context = context;
        _emailService = emailService;
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
        if (membre != null && !string.IsNullOrEmpty(membre.Email))
        {
           // 1. Définition du sujet
string sujet = "📩 Réception de versement - Tontine";

// 2. Construction d'un corps d'email structuré et élégant
string corps = $@"
    <div style='font-family: sans-serif; line-height: 1.6; color: #333;'>
        <h2 style='color: #2c3e50;'>Bonjour {membre.Nom},</h2>
        
        <p>Nous vous informons que votre versement a bien été reçu par notre système :</p>
        
        <div style='background-color: #f8f9fa; padding: 15px; border-radius: 8px; border-left: 5px solid #007bff;'>
            <p style='margin: 0;'><b>Montant versé :</b> {versement.Montant} Ar</p>
            <p style='margin: 0;'><b>Statut :</b> <span style='color: #e67e22;'>En attente de validation</span></p>
        </div>
        
        <p>Votre nouveau solde est désormais de : <b>{membre.Solde} Ar</b>.</p>
        
        <p>Une notification finale vous sera envoyée dès que l'administrateur aura validé la transaction.</p>
        
        <hr style='border: 0; border-top: 1px solid #eee; margin: 20px 0;'>
        <p style='font-size: 0.9em; color: #7f8c8d;'><i>Ceci est un message automatique de votre système de gestion de Tontine.</i></p>
    </div>";

// 3. Envoi de l'email
await _emailService.EnvoyerEmailAsync(membre.Email, sujet, corps);
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
                
               // 1. Définition du sujet (plus affirmatif)
string sujet = "✅ Versement validé avec succès !";

// 2. Construction du corps d'email "Style Succès"
string corps = $@"
    <div style='font-family: sans-serif; line-height: 1.6; color: #333;'>
        <div style='text-align: center; margin-bottom: 20px;'>
            <h1 style='color: #27ae60;'>Félicitations !</h1>
        </div>

        <h2 style='color: #2c3e50;'>Bonjour {membre.Nom},</h2>
        
        <p>Bonne nouvelle ! Votre versement a été <b>officiellement validé</b> par l'administrateur de la tontine.</p>
        
        <div style='background-color: #f0fff4; padding: 20px; border-radius: 8px; border-left: 5px solid #27ae60; margin: 20px 0;'>
            <p style='margin: 0; font-size: 1.1em;'><strong>Montant confirmé :</strong> {versement.Montant} Ar</p>
            <p style='margin: 0; font-size: 1.1em;'><strong>Nouveau Solde :</strong> {membre.Solde} Ar</p>
        </div>
        
        <p>Votre participation est bien à jour. Vous pouvez consulter votre historique complet sur votre espace membre.</p>
        
        <div style='text-align: center; margin-top: 30px;'>
            <p style='color: #7f8c8d;'>Merci d'être un membre actif de notre communauté !</p>
        </div>

        <hr style='border: 0; border-top: 1px solid #eee; margin: 20px 0;'>
        <p style='font-size: 0.8em; color: #95a5a6; text-align: center;'>
            Ceci est une notification automatique de <b>Gestion Tontine v2.0</b>
        </p>
    </div>";

// 3. Envoi de l'email
await _emailService.EnvoyerEmailAsync(membre.Email, sujet, corps);
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