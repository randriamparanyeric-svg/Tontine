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

        public GroupesController(ApplicationDbContext context, IPasswordHashService passwordService, IShareCodeService shareCodeService)
        {
            _context = context;
            _passwordService = passwordService;
            _shareCodeService = shareCodeService;
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

        // ➕ Enregistrement du groupe
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Groupe groupe)
        {
            if (ModelState.IsValid)
            {
                // Hash du mot de passe
                groupe.MotDePasseHash = _passwordService.HashPassword(groupe.MotDePasseHash);

                // Code de partage unique
                groupe.CodePartage = _shareCodeService.GenerateShareCode();
                groupe.DateCreation = DateTime.Now;

                _context.Add(groupe);
                await _context.SaveChangesAsync();

                // Authentifier l'admin
                HttpContext.Session.SetString($"admin_groupe_{groupe.Id}", "true");

                return RedirectToAction(nameof(Details), new { id = groupe.Id });
            }
            return View(groupe);
        }

        // 📄 Détails d'un groupe (ADMIN ONLY)
        public async Task<IActionResult> Details(int id)
        {
            var groupe = await _context.Groupes
                .Include(g => g.Membres)
                .ThenInclude(m => m.Versements)
                .Include(g => g.Versements)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (groupe == null) return NotFound();

            // Vérifier si c'est l'admin
            var isAdmin = HttpContext.Session.GetString($"admin_groupe_{groupe.Id}") == "true";
            if (!isAdmin)
                return Unauthorized("Accès refusé");

            return View(groupe);
        }

        // 🔓 Page publique d'accès
        public async Task<IActionResult> AccederMembre(string codePartage)
        {
            var groupe = await _context.Groupes
                .Include(g => g.Membres)
                .FirstOrDefaultAsync(g => g.CodePartage == codePartage);

            if (groupe == null)
                return NotFound("Groupe introuvable");

            return View(groupe);
        }

        // 🔑 Vérifier le mot de passe (pour Admin)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifierAdmin(string codePartage, string motDePasse)
        {
            var groupe = await _context.Groupes
                .FirstOrDefaultAsync(g => g.CodePartage == codePartage);

            if (groupe == null)
                return NotFound("Groupe introuvable");

            // Vérifier le mot de passe
            if (!_passwordService.VerifyPassword(motDePasse, groupe.MotDePasseHash))
            {
                ModelState.AddModelError("motDePasse", "Mot de passe incorrect");
                return View("AccederMembre", groupe);
            }

            // Authentifier l'admin
            HttpContext.Session.SetString($"admin_groupe_{groupe.Id}", "true");

            return RedirectToAction(nameof(Details), new { id = groupe.Id });
        }

        // 📊 Demander le numéro de téléphone du membre
        public async Task<IActionResult> IdentifierMembre(string codePartage)
        {
            var groupe = await _context.Groupes
                .Include(g => g.Membres)
                .FirstOrDefaultAsync(g => g.CodePartage == codePartage);

            if (groupe == null)
                return NotFound();

            return View(groupe);
        }

        // 🔍 Vérifier le numéro de téléphone et afficher la vue membre
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifierMembre(string codePartage, string telephone)
        {
            var groupe = await _context.Groupes
                .Include(g => g.Membres)
                .FirstOrDefaultAsync(g => g.CodePartage == codePartage);

            if (groupe == null)
                return NotFound();

            // Chercher le membre avec ce numéro dans ce groupe
            var membre = await _context.Membres
                .FirstOrDefaultAsync(m => m.Telephone == telephone && m.GroupeId == groupe.Id);

            if (membre == null)
            {
                ModelState.AddModelError("telephone", "Numéro de téléphone introuvable dans ce groupe");
                return View("IdentifierMembre", groupe);
            }

            // Stocker le numéro du membre en session
            HttpContext.Session.SetString($"membre_groupe_{groupe.Id}", telephone);

            return RedirectToAction(nameof(VoirCommeMembe), new { codePartage = codePartage });
        }

        // 📊 Vue pour les MEMBRES (sans édition)
        public async Task<IActionResult> VoirCommeMembe(string codePartage)
        {
            var groupe = await _context.Groupes
                .Include(g => g.Membres)
                .ThenInclude(m => m.Versements)
                .Include(g => g.Versements)
                .FirstOrDefaultAsync(g => g.CodePartage == codePartage);

            if (groupe == null)
                return NotFound();

            // Vérifier que le membre s'est identifié
            var telephoneStocke = HttpContext.Session.GetString($"membre_groupe_{groupe.Id}");
            if (string.IsNullOrEmpty(telephoneStocke))
                return RedirectToAction(nameof(IdentifierMembre), new { codePartage = codePartage });

            // Récupérer le membre
            var membre = groupe.Membres.FirstOrDefault(m => m.Telephone == telephoneStocke);
            if (membre == null)
                return Unauthorized("Membre non trouvé");

            // Passer le membre en ViewBag
            ViewBag.MembreActuel = membre;

            return View(groupe);
        }

        // ❌ Quitter le groupe
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuitterGroupe(int groupeId)
        {
            HttpContext.Session.Remove($"admin_groupe_{groupeId}");
            HttpContext.Session.Remove($"membre_groupe_{groupeId}");
            return RedirectToAction(nameof(Index));
        }
    
    // ❌ Formulaire de suppression (ADMIN ONLY)
public async Task<IActionResult> Delete(int id)
{
    var groupe = await _context.Groupes.FindAsync(id);

    if (groupe == null) return NotFound();

    // Vérifier que c'est l'admin
    var isAdmin = HttpContext.Session.GetString($"admin_groupe_{groupe.Id}") == "true";
    if (!isAdmin)
        return Unauthorized("Accès refusé");

    return View(groupe);
}

// ❌ Suppression du groupe (ADMIN ONLY)
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Delete(int id, string motDePasse)
{
    var groupe = await _context.Groupes
        .Include(g => g.Membres)
        .Include(g => g.Versements)
        .FirstOrDefaultAsync(g => g.Id == id);

    if (groupe == null) return NotFound();

    // Vérifier que c'est l'admin
    var isAdmin = HttpContext.Session.GetString($"admin_groupe_{groupe.Id}") == "true";
    if (!isAdmin)
        return Unauthorized("Accès refusé");

    // Vérifier le mot de passe
    var passwordService = HttpContext.RequestServices.GetService<IPasswordHashService>();
    if (!passwordService.VerifyPassword(motDePasse, groupe.MotDePasseHash))
    {
        ModelState.AddModelError("motDePasse", "Mot de passe incorrect");
        return View(groupe);
    }

    // Supprimer tous les versements
    _context.Versements.RemoveRange(groupe.Versements);

    // Supprimer tous les membres
    _context.Membres.RemoveRange(groupe.Membres);

    // Supprimer le groupe
    _context.Groupes.Remove(groupe);
    await _context.SaveChangesAsync();

    // Déconnecter l'admin
    HttpContext.Session.Remove($"admin_groupe_{groupe.Id}");

    return RedirectToAction(nameof(Index));
}
}
}