using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tontine.Data;
using Tontine.Models;

namespace Tontine.Controllers
{
    public class MembresController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MembresController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 📋 Liste des membres (ADMIN ONLY)
        public async Task<IActionResult> Index()
        {
            var membres = await _context.Membres
                .Include(m => m.Groupe)
                .Include(m => m.Versements)
                .ToListAsync();

            return View(membres);
        }

        // 📄 Détails d'un membre
        public async Task<IActionResult> Details(int id)
        {
            var membre = await _context.Membres
                .Include(m => m.Groupe)
                .Include(m => m.Versements)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (membre == null) return NotFound();

            return View(membre);
        }

        // ➕ Ajouter membre (ADMIN ONLY)
        public async Task<IActionResult> Create(int? groupeId)
        {
            // Vérifier que c'est un admin
            if (groupeId.HasValue)
            {
                var groupe = await _context.Groupes.FindAsync(groupeId.Value);
                if (groupe != null)
                {
                    var isAdmin = HttpContext.Session.GetString($"admin_groupe_{groupe.Id}") == "true";
                    if (!isAdmin)
                        return Unauthorized("Seul l'admin peut ajouter des membres");
                }
            }

            var groupes = await _context.Groupes.ToListAsync();
            ViewBag.Groupes = groupes;
            ViewBag.GroupeId = groupeId;

            return View();
        }

        // ➕ Enregistrement du membre (ADMIN ONLY)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Membre membre)
        {
            // Vérifier que c'est un admin
            var groupe = await _context.Groupes.FindAsync(membre.GroupeId);
            if (groupe != null)
            {
                var isAdmin = HttpContext.Session.GetString($"admin_groupe_{groupe.Id}") == "true";
                if (!isAdmin)
                    return Unauthorized("Seul l'admin peut ajouter des membres");
            }

            if (ModelState.IsValid)
            {
                membre.DateAdhesion = DateTime.Now;
                _context.Add(membre);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Details), new { id = membre.Id });
            }

            var groupes = await _context.Groupes.ToListAsync();
            ViewBag.Groupes = groupes;

            return View(membre);
        }

        // ✏️ Éditer un membre (ADMIN ONLY)
        public async Task<IActionResult> Edit(int id)
        {
            var membre = await _context.Membres.FindAsync(id);
            if (membre == null) return NotFound();

            // Vérifier que c'est un admin
            var isAdmin = HttpContext.Session.GetString($"admin_groupe_{membre.GroupeId}") == "true";
            if (!isAdmin)
                return Unauthorized("Seul l'admin peut éditer les membres");

            var groupes = await _context.Groupes.ToListAsync();
            ViewBag.Groupes = groupes;

            return View(membre);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Membre membre)
        {
            if (id != membre.Id) return NotFound();

            // Vérifier que c'est un admin
            var isAdmin = HttpContext.Session.GetString($"admin_groupe_{membre.GroupeId}") == "true";
            if (!isAdmin)
                return Unauthorized("Seul l'admin peut éditer les membres");

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(membre);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Details), new { id = membre.Id });
                }
                catch (DbUpdateConcurrencyException)
                {
                    return NotFound();
                }
            }

            var groupes = await _context.Groupes.ToListAsync();
            ViewBag.Groupes = groupes;

            return View(membre);
        }
    }
}