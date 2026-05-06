using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tontine.Data;
using Tontine.Models;
using Tontine.Services;

namespace Tontine.Controllers
{
    public class RetraitsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ILogger<RetraitsController> _logger;

        public RetraitsController(ApplicationDbContext context, IEmailService emailService, ILogger<RetraitsController> logger)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
        }

        // 📋 LISTE DES RETRAITS (Admin)
        public async Task<IActionResult> Index()
        {
            var retraits = await _context.Retraits
                .Include(r => r.Membre)
                .ToListAsync();
            return View(retraits);
        }

        // ➕ DEMANDE DE RETRAIT (Côté Membre)
// GET: Retraits/Create
public async Task<IActionResult> Create(int membreId)
{
    // On inclut impérativement le Groupe pour le calcul et la DateCreation
    var membre = await _context.Membres
        .Include(m => m.Groupe)
        .Include(m => m.Retraits) 
        .FirstOrDefaultAsync(m => m.Id == membreId);

    if (membre == null) return NotFound();

    // 1. VÉRIFICATION DE SÉCURITÉ : UN SEUL RETRAIT ACTIF PAR CYCLE
    // On bloque si un retrait "Confirmé" ou "En attente" existe déjà pour ce cycle
    bool aDejaRetireCeCycle = membre.Retraits.Any(r => 
        r.DateDemande >= membre.Groupe.DateCreation && 
        (r.Statut == "Confirmé" || r.Statut == "En attente"));

    if (aDejaRetireCeCycle)
    {
        TempData["ErrorMessage"] = "🚫 Action impossible : Vous avez déjà un retrait validé ou une demande en cours pour ce cycle.";
        return RedirectToAction("VoirCommeMembe", "Groupes", new { codePartage = membre.Groupe.CodePartage });
    }

    // 2. CALCUL AUTOMATIQUE DU MONTANT
    // Formule : Nombre de membres total du groupe × Montant de la cotisation individuelle
    decimal montantFixe = (decimal)membre.Groupe.NombreMembresPrevu * membre.Groupe.MontantParVersement;

    // 3. PRÉPARATION DU MODÈLE POUR LA VUE
    var retrait = new Retrait
    {
        MembreId = membreId,
        DateDemande = DateTime.Now,
        Statut = "En attente",
        Montant = montantFixe, // On fixe le montant ici
        Membre = membre        // Nécessaire pour afficher les détails du calcul dans la vue
    };

    ViewBag.MembreNom = membre.Nom;

    return View(retrait);
}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Retrait retrait)
        {
            var membre = await _context.Membres
                .Include(m => m.Groupe)
                .FirstOrDefaultAsync(m => m.Id == retrait.MembreId);

            if (ModelState.IsValid)
            {
                retrait.Statut = "En attente";
                retrait.DateDemande = DateTime.Now;

                _context.Add(retrait);
                await _context.SaveChangesAsync();

                // 📧 EMAIL : ACCUSÉ DE RÉCEPTION DEMANDE
                if (membre != null && !string.IsNullOrWhiteSpace(membre.Email))
                {
                    try
                    {
                        string sujet = "📥 Demande de retrait enregistrée - Tontine";
                        string corps = $@"
                            <div style='font-family: sans-serif; border: 1px solid #ddd; padding: 20px; border-radius: 10px;'>
                                <h2 style='color: #2c3e50;'>Bonjour {membre.Nom},</h2>
                                <p>Votre demande de retrait de <strong>{retrait.Montant.ToString("N0")} Ar</strong> a bien été transmise à l'administrateur du groupe <strong>{membre.Groupe?.Nom}</strong>.</p>
                                <p>Vous recevrez une notification par email dès que les fonds seront débloqués.</p>
                                <hr>
                                <p style='color: #888; font-size: 12px;'>Ceci est un message automatique de votre plateforme Tontine.</p>
                            </div>";

                        await _emailService.EnvoyerEmailAsync(membre.Email, sujet, corps);
                    }
                    catch (Exception ex) { _logger.LogError(ex, "Erreur email Retrait Create"); }
                }

                TempData["SuccessMessage"] = "✅ Demande de retrait envoyée ! L'admin va traiter votre demande.";
                return RedirectToAction("VoirCommeMembe", "Groupes", new { codePartage = membre?.Groupe?.CodePartage });
            }

            retrait.Membre = membre;
            return View(retrait);
        }

       // ✅ CONFIRMER LE RETRAIT (Côté Admin)
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Confirmer(int id)
{
    var retrait = await _context.Retraits
        .Include(r => r.Membre)
        .ThenInclude(m => m.Groupe)
        .FirstOrDefaultAsync(r => r.Id == id);

    if (retrait == null) return NotFound();

    // 1. Vérification de la session Admin
    var isAdmin = HttpContext.Session.GetString($"admin_groupe_{retrait.Membre.GroupeId}") == "true";
    if (!isAdmin) return Unauthorized();

   // --- CALCUL DU SOLDE DE LA CAISSE DU GROUPE ---
// On convertit explicitement en double pour SQLite
var totalVersements = await _context.Versements
    .Where(v => v.GroupeId == retrait.Membre.GroupeId && v.Statut == "Confirmé")
    .SumAsync(v => (double)v.Montant); 

var totalRetraitsPrecedents = await _context.Retraits
    .Where(r => r.Membre.GroupeId == retrait.Membre.GroupeId && r.Statut == "Confirmé")
    .SumAsync(r => (double)r.Montant);

var soldeCaisseActuel = (decimal)(totalVersements - totalRetraitsPrecedents);

    // 2. VÉRIFICATION DE DISPONIBILITÉ
    if (retrait.Montant > soldeCaisseActuel)
    {
        TempData["ErrorMessage"] = $"⚠️ Fonds insuffisants en caisse. Disponible : {soldeCaisseActuel.ToString("N0")} Ar. Impossible de retirer {retrait.Montant.ToString("N0")} Ar.";
        return RedirectToAction("Details", "Groupes", new { id = retrait.Membre.GroupeId });
    }

    // 3. VALIDATION DU RETRAIT
    retrait.Statut = "Confirmé";
    var membre = retrait.Membre;

    if (membre != null)
    {
        // On déduit le montant du solde personnel du membre (comptabilité individuelle)
        membre.Solde -= retrait.Montant;
        _context.Update(membre);

        // MESSAGE DE CONFIRMATION ADMIN
        TempData["SuccessMessage"] = $"✅ Le retrait de {membre.Nom} a été validé. Le montant de {retrait.Montant.ToString("N0")} Ar a été déduit du fond collecté.";

        // 📧 EMAIL : CONFIRMATION DE PAIEMENT
        if (!string.IsNullOrWhiteSpace(membre.Email))
        {
            try
            {
                string sujet = "💸 Votre retrait a été validé !";
                string corps = $@"
                    <div style='font-family: sans-serif; border-left: 5px solid #27ae60; padding: 20px; background-color: #f0fff4;'>
                        <h2 style='color: #27ae60;'>Bonjour {membre.Nom} !</h2>
                        <p>L'administrateur a validé votre retrait de <strong>{retrait.Montant.ToString("N0")} Ar</strong>.</p>
                        <p>Les fonds ont été déduits de la caisse commune et sont mis à votre disposition.</p>
                        <p><strong>Votre solde restant :</strong> {membre.Solde.ToString("N0")} Ar</p>
                    </div>";

                await _emailService.EnvoyerEmailAsync(membre.Email, sujet, corps);
            }
            catch (Exception ex) { _logger.LogError(ex, "Erreur email Retrait Confirmer"); }
        }
    }

    _context.Update(retrait);
    await _context.SaveChangesAsync();

    return RedirectToAction("Details", "Groupes", new { id = retrait.Membre.GroupeId });
}

        // ❌ REJETER LE RETRAIT (Côté Admin)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Rejeter(int id)
        {
            var retrait = await _context.Retraits
                .Include(r => r.Membre)
                .ThenInclude(m => m.Groupe)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (retrait == null) return NotFound();

            var isAdmin = HttpContext.Session.GetString($"admin_groupe_{retrait.Membre.GroupeId}") == "true";
            if (!isAdmin) return Unauthorized();

            retrait.Statut = "Rejeté";

            // MESSAGE INFO ADMIN
            TempData["ErrorMessage"] = $"⚠️ La demande de retrait de {retrait.Membre?.Nom} a été rejetée.";

            // 📧 EMAIL : NOTIFICATION DE REJET
            if (retrait.Membre != null && !string.IsNullOrWhiteSpace(retrait.Membre.Email))
            {
                try
                {
                    string sujet = "❌ Information sur votre demande de retrait";
                    string corps = $@"
                        <div style='font-family: sans-serif; border: 1px solid #dc3545; padding: 20px;'>
                            <h2 style='color: #dc3545;'>Bonjour {retrait.Membre.Nom},</h2>
                            <p>Votre demande de retrait de <strong>{retrait.Montant.ToString("N0")} Ar</strong> a été rejetée par l'administrateur.</p>
                            <p>Veuillez contacter l'administrateur de votre groupe <strong>{retrait.Membre.Groupe?.Nom}</strong> pour obtenir plus d'informations.</p>
                        </div>";

                    await _emailService.EnvoyerEmailAsync(retrait.Membre.Email, sujet, corps);
                }
                catch (Exception ex) { _logger.LogError(ex, "Erreur email Retrait Rejeter"); }
            }

            _context.Update(retrait);
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", "Groupes", new { id = retrait.Membre.GroupeId });
        }
    }
}