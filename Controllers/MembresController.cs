using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tontine.Data;
using Tontine.Models;
using Microsoft.AspNetCore.Http; // Nécessaire pour IFormFile
using System.IO;

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

        // ➕ Enregistrement du membre avec gestion des fichiers CIN
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Membre membre, IFormFile? cinRectoFile, IFormFile? cinVersoFile)
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
                // --- GESTION DES PHOTOS CIN ---
                string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/cin");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                if (cinRectoFile != null && cinRectoFile.Length > 0)
                {
                    string fileName = Guid.NewGuid().ToString() + "_R_" + cinRectoFile.FileName;
                    string filePath = Path.Combine(uploadsFolder, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create)) { await cinRectoFile.CopyToAsync(stream); }
                    membre.CinRecto = fileName; // Assurez-vous que cette propriété existe dans votre modèle
                }

                if (cinVersoFile != null && cinVersoFile.Length > 0)
                {
                    string fileName = Guid.NewGuid().ToString() + "_V_" + cinVersoFile.FileName;
                    string filePath = Path.Combine(uploadsFolder, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create)) { await cinVersoFile.CopyToAsync(stream); }
                    membre.CinVerso = fileName; // Assurez-vous que cette propriété existe dans votre modèle
                }

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
        public async Task<IActionResult> Edit(int id, Membre membre, IFormFile? cinRectoFile, IFormFile? cinVersoFile)
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
                    // --- GESTION DES PHOTOS CIN LORS DE L'ÉDITION ---
                    string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/cin");
                    
                    if (cinRectoFile != null)
                    {
                        string fileName = Guid.NewGuid().ToString() + "_R_" + cinRectoFile.FileName;
                        string filePath = Path.Combine(uploadsFolder, fileName);
                        using (var stream = new FileStream(filePath, FileMode.Create)) { await cinRectoFile.CopyToAsync(stream); }
                        membre.CinRecto = fileName;
                    }

                    if (cinVersoFile != null)
                    {
                        string fileName = Guid.NewGuid().ToString() + "_V_" + cinVersoFile.FileName;
                        string filePath = Path.Combine(uploadsFolder, fileName);
                        using (var stream = new FileStream(filePath, FileMode.Create)) { await cinVersoFile.CopyToAsync(stream); }
                        membre.CinVerso = fileName;
                    }

                    _context.Update(membre);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Details), new { id = membre.Id });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Membres.Any(e => e.Id == membre.Id)) return NotFound();
                    else throw;
                }
            }

            var groupes = await _context.Groupes.ToListAsync();
            ViewBag.Groupes = groupes;

            return View(membre);
        }

        // 🗑️ Supprimer un membre (ADMIN ONLY)
        public async Task<IActionResult> Delete(int id)
        {
            var membre = await _context.Membres
                .Include(m => m.Groupe)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (membre == null) return NotFound();

            // Vérifier que c'est un admin
            var isAdmin = HttpContext.Session.GetString($"admin_groupe_{membre.GroupeId}") == "true";
            if (!isAdmin)
                return Unauthorized("Seul l'admin peut supprimer des membres");

            return View(membre);
        }

        [HttpPost, ActionName("Delete")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> DeleteConfirmed(int id)
{
    var membre = await _context.Membres.FindAsync(id);
    if (membre == null) return NotFound();

    // 1. SAUVEGARDER L'ID DU GROUPE AVANT LA SUPPRESSION
    int idDuGroupeCible = membre.GroupeId;

    // Vérifier que c'est un admin
    var isAdmin = HttpContext.Session.GetString($"admin_groupe_{idDuGroupeCible}") == "true";
    if (!isAdmin)
        return Unauthorized("Seul l'admin peut supprimer des membres");

    // --- SUPPRESSION DES FICHIERS PHYSIQUES SUR LE SERVEUR ---
    string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/cin");

    if (!string.IsNullOrEmpty(membre.CinRecto))
    {
        string fullPath = Path.Combine(uploadsFolder, membre.CinRecto);
        if (System.IO.File.Exists(fullPath)) System.IO.File.Delete(fullPath);
    }

    if (!string.IsNullOrEmpty(membre.CinVerso))
    {
        string fullPath = Path.Combine(uploadsFolder, membre.CinVerso);
        if (System.IO.File.Exists(fullPath)) System.IO.File.Delete(fullPath);
    }

    // 2. SUPPRESSION EN BASE DE DONNÉES
    _context.Membres.Remove(membre);
    await _context.SaveChangesAsync();

    // 3. REDIRECTION EN UTILISANT LA VARIABLE SAUVEGARDÉE
    // On utilise "Details" ou "Admin" selon le nom de votre action dans GroupesController
    return RedirectToAction("Details", "Groupes", new { id = idDuGroupeCible });
}
    }
}