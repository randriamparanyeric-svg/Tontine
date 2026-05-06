using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tontine.Data;
using Tontine.Models;
using Tontine.Services;

namespace Tontine.Controllers
{
    public class GroupesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHashService _passwordService;
        private readonly IShareCodeService _shareCodeService;
        private readonly IEmailService _emailService;
        private readonly ILogger<GroupesController> _logger;

        // CORRECTION DU CONSTRUCTEUR : Ajout de IEmailService dans les paramètres
        public GroupesController(
            ApplicationDbContext context, 
            IPasswordHashService passwordService, 
            IShareCodeService shareCodeService,
            IEmailService emailService, // <--- ÉTAIT MANQUANT ICI
            ILogger<GroupesController> logger) // <--- Injection ici
        {
            _context = context;
            _passwordService = passwordService;
            _shareCodeService = shareCodeService;
            _emailService = emailService; 
            _logger = logger;
        }

        // 📋 Liste des groupes
        public async Task<IActionResult> Index()
        {
            var groupes = await _context.Groupes
                .Include(g => g.Membres)
                .Include(g => g.Versements)
                .ToListAsync();
            return View(groupes);
        }

        // ➕ Formulaire de création
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Create(Groupe groupe)
{
    if (ModelState.IsValid)
    {
        // Initialisation
        groupe.EstValideParDev = false; 
        groupe.MotDePasseHash = _passwordService.HashPassword(groupe.MotDePasseHash);
        groupe.CodePartage = _shareCodeService.GenerateShareCode();
        groupe.DateCreation = DateTime.Now;

        _context.Add(groupe);
        await _context.SaveChangesAsync();

        // Envoi de l'email
        try 
        {
            string sujet = $"Félicitations ! Votre tontine '{groupe.Nom}' est créée";
            string messageBody = $@"
                <div style='font-family: Arial, sans-serif; border: 1px solid #ddd; padding: 20px;'>
                    <h2 style='color: #2c3e50;'>Bonjour {groupe.NomAdmin},</h2>
                    <p>Votre tontine <strong>{groupe.Nom}</strong> a bien été enregistrée sur notre plateforme.</p>
                    <hr/>
                    <p><strong>⚠️ Statut : En attente d'activation</strong></p>
                    <p>Pour activer définitivement votre accès, veuillez effectuer le règlement des frais de gestion via <strong>M-Vola</strong> :</p>
                    <ul>
                        <li><strong>Numéro :</strong> +261 34 64 474 61</li>
                        <li><strong>Nom :</strong> Makalahiarison</li>
                    </ul>
                    <p>Une fois le paiement validé, vous recevrez un email de confirmation et vous pourrez inviter vos membres.</p>
                    <br/>
                    <p>Besoin d'aide ? Contactez-nous sur WhatsApp : +261 32 64 572 08</p>
                </div>";

            // UTILISATION DU NOM CORRECT : EnvoyerEmailAsync
            await _emailService.EnvoyerEmailAsync(groupe.AdminEmail, sujet, messageBody);
        }
        catch (Exception ex)
        {
            // On utilise le logger que vous avez ajouté au constructeur
            _logger.LogError(ex, "Échec de l'envoi de l'email de création pour {Email}", groupe.AdminEmail);
        }

        return RedirectToAction(nameof(AttenteActivation), new { id = groupe.Id });
    }
    return View(groupe);
}
        public async Task<IActionResult> AttenteActivation(int id)
        {
            var groupe = await _context.Groupes.FindAsync(id);
            if (groupe == null) return NotFound();
            if (groupe.EstValideParDev) return RedirectToAction(nameof(Details), new { id = id });
            return View(groupe);
        }

        public async Task<IActionResult> Details(int id)
        {
            var groupe = await _context.Groupes
                .Include(g => g.Membres).ThenInclude(m => m.Versements)
                .Include(g => g.Membres).ThenInclude(m => m.Retraits) // <--- AJOUTEZ CECI
                .Include(g => g.Versements)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (groupe == null) return NotFound();
            if (!groupe.EstValideParDev) return RedirectToAction(nameof(AttenteActivation), new { id = groupe.Id });

            var isAdmin = HttpContext.Session.GetString($"admin_groupe_{groupe.Id}") == "true";
            if (!isAdmin) return Unauthorized("Accès refusé");

            return View(groupe);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifierAdmin(string codePartage, string motDePasse)
        {
            var groupe = await _context.Groupes.FirstOrDefaultAsync(g => g.CodePartage == codePartage);
            if (groupe == null) return NotFound("Groupe introuvable");

            if (!groupe.EstValideParDev) return RedirectToAction(nameof(AttenteActivation), new { id = groupe.Id });

            if (!_passwordService.VerifyPassword(motDePasse, groupe.MotDePasseHash))
            {
                ModelState.AddModelError("motDePasse", "Mot de passe incorrect");
                return View("AccederMembre", groupe);
            }

            HttpContext.Session.SetString($"admin_groupe_{groupe.Id}", "true");
            return RedirectToAction(nameof(Details), new { id = groupe.Id });
        }

        public async Task<IActionResult> IdentifierMembre(string codePartage)
        {
            var groupe = await _context.Groupes.Include(g => g.Membres).FirstOrDefaultAsync(g => g.CodePartage == codePartage);
            if (groupe == null) return NotFound();
            return View(groupe);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifierMembre(string codePartage, string telephone)
        {
            var groupe = await _context.Groupes.Include(g => g.Membres).FirstOrDefaultAsync(g => g.CodePartage == codePartage);
            if (groupe == null) return NotFound();

            var membre = await _context.Membres.FirstOrDefaultAsync(m => m.Telephone == telephone && m.GroupeId == groupe.Id);
            if (membre == null)
            {
                ModelState.AddModelError("telephone", "Numéro de téléphone introuvable");
                return View("IdentifierMembre", groupe);
            }

            HttpContext.Session.SetString($"membre_groupe_{groupe.Id}", telephone);
            return RedirectToAction(nameof(VoirCommeMembe), new { codePartage = codePartage });
        }

       // 2. POUR LE MEMBRE (Ne pas oublier celle-ci !)
public async Task<IActionResult> VoirCommeMembe(string codePartage)
{
    var groupe = await _context.Groupes
        .Include(g => g.Membres).ThenInclude(m => m.Versements)
        .Include(g => g.Membres).ThenInclude(m => m.Retraits) // Ajouté ici aussi !
        .Include(g => g.Versements)
        .FirstOrDefaultAsync(g => g.CodePartage == codePartage);

    if (groupe == null) return NotFound();

    var telephoneStocke = HttpContext.Session.GetString($"membre_groupe_{groupe.Id}");
    var membre = groupe.Membres.FirstOrDefault(m => m.Telephone == telephoneStocke);

    if (membre == null) return Unauthorized();

    ViewBag.MembreActuel = membre;
    return View(groupe);
}
        public async Task<IActionResult> AccederMembre(string codePartage)
        {
            var groupe = await _context.Groupes.Include(g => g.Membres).FirstOrDefaultAsync(g => g.CodePartage == codePartage);
            if (groupe == null) return NotFound();
            return View(groupe);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuitterGroupe(int groupeId)
        {
            HttpContext.Session.Remove($"admin_groupe_{groupeId}");
            HttpContext.Session.Remove($"membre_groupe_{groupeId}");
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var groupe = await _context.Groupes.FindAsync(id);
            if (groupe == null) return NotFound();
            var isAdmin = HttpContext.Session.GetString($"admin_groupe_{groupe.Id}") == "true";
            if (!isAdmin) return Unauthorized();
            return View(groupe);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, string motDePasse)
        {
            var groupe = await _context.Groupes.Include(g => g.Membres).Include(g => g.Versements).FirstOrDefaultAsync(g => g.Id == id);
            if (groupe == null) return NotFound();

            var isAdmin = HttpContext.Session.GetString($"admin_groupe_{groupe.Id}") == "true";
            if (!isAdmin) return Unauthorized();

            if (!_passwordService.VerifyPassword(motDePasse, groupe.MotDePasseHash))
            {
                ModelState.AddModelError("motDePasse", "Mot de passe incorrect");
                return View(groupe);
            }

            _context.Versements.RemoveRange(groupe.Versements);
            _context.Membres.RemoveRange(groupe.Membres);
            _context.Groupes.Remove(groupe);
            await _context.SaveChangesAsync();

            HttpContext.Session.Remove($"admin_groupe_{groupe.Id}");
            return RedirectToAction(nameof(Index));
        }

        // --- SECTION RÉCUPÉRATION DE MOT DE PASSE ---

        public IActionResult ForgotPassword() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrEmpty(email)) return View();

            var groupe = await _context.Groupes.FirstOrDefaultAsync(g => g.AdminEmail.Trim() == email.Trim());
            
            if (groupe != null)
            {
                groupe.ResetToken = Guid.NewGuid().ToString();
                groupe.ResetTokenExpiry = DateTime.Now.AddHours(2);
                await _context.SaveChangesAsync();
// Génération du lien complet (assurez-vous que Request.Scheme est bien présent)
var resetLink = Url.Action("ResetPassword", "Groupes", 
    new { token = groupe.ResetToken }, 
    protocol: Request.Scheme);

// Message avec bouton ET lien visible en texte brut
string messageHtml = $@"
    <div style='font-family: Arial, sans-serif; padding: 20px; border: 1px solid #eee;'>
        <h3 style='color: #28a745;'>Récupération de mot de passe</h3>
        <p>Bonjour,</p>
        <p>Cliquez sur le bouton ci-dessous pour changer votre mot de passe :</p>
        <p>
            <a href='{resetLink}' style='display:inline-block; padding:10px 20px; background-color:#28a745; color:white; text-decoration:none; border-radius:5px;'>
                Réinitialiser mon mot de passe
            </a>
        </p>
        <hr />
        <p style='font-size: 12px; color: #666;'>
            Si le bouton ne fonctionne pas, copiez et collez l'adresse suivante dans votre navigateur :<br />
            <span style='color: #007bff;'>{resetLink}</span>
        </p>
    </div>";
                try 
                {
                    await _emailService.EnvoyerEmailAsync(email, "🔑 Récupération de mot de passe", messageHtml);
                    TempData["Success"] = "Le lien a été envoyé à " + email;
                    return RedirectToAction(nameof(ForgotPassword));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Erreur technique d'envoi : " + ex.Message);
                }
            }
            else 
            {
                ModelState.AddModelError("", "Cette adresse email n'est pas reconnue.");
            }
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> ResetPassword(string token)
        {
            var groupe = await _context.Groupes.FirstOrDefaultAsync(g => g.ResetToken == token && g.ResetTokenExpiry > DateTime.Now);
            if (groupe == null) return BadRequest("Lien invalide ou expiré.");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string token, string nouveauMdp)
        {
            var groupe = await _context.Groupes.FirstOrDefaultAsync(g => g.ResetToken == token && g.ResetTokenExpiry > DateTime.Now);
            if (groupe == null) return BadRequest("Lien expiré.");

            if (!string.IsNullOrEmpty(nouveauMdp))
            {
                groupe.MotDePasseHash = _passwordService.HashPassword(nouveauMdp);
                groupe.ResetToken = null;
                groupe.ResetTokenExpiry = null;
                await _context.SaveChangesAsync();

                TempData["Success"] = "Mot de passe modifié ! Connectez-vous.";
                return RedirectToAction(nameof(AccederMembre), new { codePartage = groupe.CodePartage });
            }
            return View();
        }
    }
}