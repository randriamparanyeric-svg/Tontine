using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tontine.Data;
using Tontine.Models;
using Tontine.Services;

namespace Tontine.Controllers
{
    public class VersementsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ILogger<VersementsController> _logger;

        public VersementsController(ApplicationDbContext context, IEmailService emailService, ILogger<VersementsController> logger)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
        }

        // 📋 LISTE DES VERSEMENTS
        public async Task<IActionResult> Index()
        {
            var versements = await _context.Versements
                .Include(v => v.Membre)
                .Include(v => v.Groupe)
                .ToListAsync();
            return View(versements);
        }

        // ➕ FORMULAIRE DE VERSEMENT (Côté Membre)
       // GET: Versements/Create
public async Task<IActionResult> Create(int membreId)
{
    // On inclut impérativement le Groupe pour éviter les erreurs NullReference dans la vue
    var membre = await _context.Membres
        .Include(m => m.Groupe) 
        .FirstOrDefaultAsync(m => m.Id == membreId);

    if (membre == null) return NotFound();

    var versement = new Versement
    {
        MembreId = membreId,
        GroupeId = membre.GroupeId,
        Montant = membre.Groupe?.MontantParVersement ?? 0,
        Date = DateTime.Now,
        Statut = "En attente",
        Membre = membre // Nécessaire pour l'affichage initial des infos du membre
    };

    return View(versement);
}

        [HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Create(Versement versement, IFormFile? fichierPreuve, string ModePaiement)
{
    // On récupère les infos du membre et du groupe pour la redirection et l'email
    var membre = await _context.Membres
        .Include(m => m.Groupe)
        .FirstOrDefaultAsync(m => m.Id == versement.MembreId);

    if (ModelState.IsValid)
    {
        // 1. GESTION DU MODE DE PAIEMENT
        versement.ModePaiement = ModePaiement;

        // 2. GESTION DU FICHIER IMAGE (si présent)
        if (fichierPreuve != null && fichierPreuve.Length > 0)
        {
            // Créer le nom unique du fichier (ex: 20240503_1230_nom.jpg)
            string extension = Path.GetExtension(fichierPreuve.FileName);
            string nouveauNomFichier = Guid.NewGuid().ToString() + extension;
            
            // Chemin physique vers wwwroot/uploads/preuves
            string dossierUpload = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "preuves");

            // Créer le dossier s'il n'existe pas
            if (!Directory.Exists(dossierUpload)) Directory.CreateDirectory(dossierUpload);

            string cheminComplet = Path.Combine(dossierUpload, nouveauNomFichier);

            // Enregistrement du fichier sur le disque
            using (var stream = new FileStream(cheminComplet, FileMode.Create))
            {
                await fichierPreuve.CopyToAsync(stream);
            }

            // On stocke uniquement le nom du fichier dans l'objet Versement
            versement.PreuveImage = nouveauNomFichier;
        }

        versement.Statut = "En attente";
        _context.Add(versement);
        await _context.SaveChangesAsync();

        // 3. EMAIL : ACCUSÉ DE RÉCEPTION
        if (membre != null && !string.IsNullOrWhiteSpace(membre.Email))
        {
            try 
            {
                string sujet = "📩 Réception de versement - Tontine";
                string corps = $@"
                    <div style='font-family: sans-serif; border: 1px solid #ddd; padding: 20px; border-radius: 10px;'>
                        <h2 style='color: #2c3e50;'>Bonjour {membre.Nom},</h2>
                        <p>Nous avons bien reçu votre déclaration de versement de <strong>{versement.Montant.ToString("N0")} Ar</strong> via <strong>{versement.ModePaiement}</strong>.</p>
                        <p>L'administrateur va vérifier la transaction. Vous recevrez une confirmation dès validation.</p>
                        <hr>
                        <p style='color: #888; font-size: 12px;'>Ceci est un message automatique de votre plateforme Tontine.</p>
                    </div>";

                await _emailService.EnvoyerEmailAsync(membre.Email, sujet, corps);
            }
            catch (Exception ex) { _logger.LogError(ex, "Erreur email Create"); }
        }

        TempData["SuccessMessage"] = "✅ Versement envoyé ! En attente de validation par l'admin.";
        
        return RedirectToAction("VoirCommeMembe", "Groupes", new { codePartage = membre?.Groupe?.CodePartage });
    }

    // Si erreur, on re-remplit l'objet membre pour que la vue ne plante pas
    versement.Membre = membre;
    return View(versement);
}
        // ✅ CONFIRMER VERSEMENT (Côté Admin)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirmer(int id)
        {
            var versement = await _context.Versements
                .Include(v => v.Membre)
                .Include(v => v.Groupe)
                .FirstOrDefaultAsync(v => v.Id == id);

            if (versement == null) return NotFound();

            // Vérification de la session Admin
            var isAdmin = HttpContext.Session.GetString($"admin_groupe_{versement.GroupeId}") == "true";
            if (!isAdmin) return Unauthorized();

            versement.Statut = "Confirmé";
            var membre = versement.Membre;

            if (membre != null)
            {
                membre.Solde += versement.Montant;
                _context.Update(membre);

                // MESSAGE DE CONFIRMATION ADMIN
                TempData["SuccessMessage"] = $"✅ Versement de {membre.Nom} validé (+{versement.Montant} Ar).";

                // EMAIL : CONFIRMATION FINALE
                if (!string.IsNullOrWhiteSpace(membre.Email))
                {
                    try 
                    {
                        string sujet = "✅ Votre versement a été validé !";
                        string corps = $@"
                            <div style='font-family: sans-serif; border-left: 5px solid #27ae60; padding: 20px; background-color: #f0fff4;'>
                                <h2 style='color: #27ae60;'>Félicitations {membre.Nom} !</h2>
                                <p>Votre versement de <strong>{versement.Montant} Ar</strong> est désormais confirmé.</p>
                                <p><strong>Nouveau Solde :</strong> {membre.Solde} Ar</p>
                                <p>Merci pour votre confiance !</p>
                            </div>";

                        await _emailService.EnvoyerEmailAsync(membre.Email, sujet, corps);
                    }
                    catch (Exception ex) { _logger.LogError(ex, "Erreur email Confirmer"); }
                }
            }

            _context.Update(versement);
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", "Groupes", new { id = versement.GroupeId });
        }

        // ❌ REJETER VERSEMENT (Côté Admin)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Rejeter(int id)
        {
            var versement = await _context.Versements
                .Include(v => v.Membre)
                .Include(v => v.Groupe)
                .FirstOrDefaultAsync(v => v.Id == id);

            if (versement == null) return NotFound();

            var isAdmin = HttpContext.Session.GetString($"admin_groupe_{versement.GroupeId}") == "true";
            if (!isAdmin) return Unauthorized();

            versement.Statut = "Rejeté";

            // MESSAGE INFO ADMIN
            TempData["ErrorMessage"] = $"⚠️ Le versement de {versement.Membre?.Nom} a été rejeté.";

            // EMAIL : NOTIFICATION DE REJET
            if (versement.Membre != null && !string.IsNullOrWhiteSpace(versement.Membre.Email))
            {
                try
                {
                    string sujet = "❌ Information sur votre versement";
                    string corps = $@"
                        <div style='font-family: sans-serif; border: 1px solid #dc3545; padding: 20px;'>
                            <h2 style='color: #dc3545;'>Bonjour {versement.Membre.Nom},</h2>
                            <p>Votre versement de <strong>{versement.Montant} Ar</strong> a été rejeté par l'administrateur.</p>
                            <p>Veuillez vérifier les informations transmises ou contacter l'administrateur de votre groupe.</p>
                        </div>";

                    await _emailService.EnvoyerEmailAsync(versement.Membre.Email, sujet, corps);
                }
                catch (Exception ex) { _logger.LogError(ex, "Erreur email Rejeter"); }
            }

            _context.Update(versement);
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", "Groupes", new { id = versement.GroupeId });
        }
    }
}