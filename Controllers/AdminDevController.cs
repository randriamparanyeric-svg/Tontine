using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tontine.Data;
using Tontine.Services; // N'oubliez pas d'ajouter le namespace de votre service

namespace Tontine.Controllers
{
    public class AdminDevController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _config;
        private readonly IEmailService _emailService; // Injection du service email

        public AdminDevController(ApplicationDbContext context, IConfiguration config, IEmailService emailService)
        {
            _context = context;
            _config = config;
            _emailService = emailService;
        }

        // 🚪 Redirige automatiquement vers le Dashboard
        public IActionResult Index() => RedirectToAction(nameof(Dashboard));

        // 🛡️ Page de Login
        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(string username, string password)
        {
            if (username == _config["AdminDevSettings:Username"] && 
                password == _config["AdminDevSettings:Password"])
            {
                HttpContext.Session.SetString("IsAdminDev", "true");
                return RedirectToAction(nameof(Dashboard));
            }

            ViewBag.Error = "Identifiants développeur incorrects";
            return View();
        }

        // 📊 Tableau de bord global
        public async Task<IActionResult> Dashboard()
        {
            if (HttpContext.Session.GetString("IsAdminDev") != "true")
                return RedirectToAction(nameof(Login));

            // On récupère tout, trié par date, pour une vue d'ensemble
            var groupes = await _context.Groupes
                .OrderByDescending(g => g.DateCreation)
                .ToListAsync();

            return View(groupes);
        }

        // ✅ Action pour Basculer (Valider/Suspendre) avec envoi d'email
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleValidation(int id)
        {
            if (HttpContext.Session.GetString("IsAdminDev") != "true") return Unauthorized();

            var groupe = await _context.Groupes.FindAsync(id);
            if (groupe != null)
            {
                // Inversion de l'état
                groupe.EstValideParDev = !groupe.EstValideParDev;
                await _context.SaveChangesAsync();

                // Préparation et envoi de l'email
                try
        {
            // 1. DÉCLARATION INDISPENSABLE AVANT LE IF
         
            
            string sujet;
            string messageBody;
            string urlConnexion = $"{Request.Scheme}://{Request.Host}/groupe/{groupe.CodePartage}";

            if (groupe.EstValideParDev)
            {
            // Le lien court que vous avez demandé
                sujet = $"🚀 Votre tontine '{groupe.Nom}' est prête !";
                
                messageBody = $@"
                <div style='font-family: ""Segoe UI"", Tahoma, Geneva, Verdana, sans-serif; max-width: 600px; margin: auto; border: 1px solid #e0e0e0; border-radius: 10px; overflow: hidden; box-shadow: 0 4px 10px rgba(0,0,0,0.1);'>
                    <div style='background-color: #28a745; padding: 20px; text-align: center;'>
                        <h1 style='color: white; margin: 0; font-size: 24px;'>Activation Réussie !</h1>
                    </div>
                    
                    <div style='padding: 30px; color: #333; line-height: 1.6;'>
                        <p style='font-size: 18px;'>Bonjour <strong>{groupe.NomAdmin}</strong>,</p>
                        
                        <p>Bonne nouvelle ! Votre tontine <strong>{groupe.Nom}</strong> a été validée avec succès. Vous pouvez maintenant commencer vos opérations et inviter vos membres.</p>
                        
                        <div style='background-color: #f8f9fa; border-radius: 8px; padding: 20px; margin: 25px 0; text-align: center; border: 1px dashed #28a745;'>
                            <p style='margin-bottom: 10px; color: #666; font-size: 14px;'>Votre lien d'accès direct et de partage :</p>
                            <a href='{urlConnexion}' style='font-size: 18px; color: #007bff; font-weight: bold; text-decoration: none; word-break: break-all;'>
                                {urlConnexion}
                            </a>
                        </div>

                        <p style='font-size: 15px;'><strong>Comment ça marche ?</strong></p>
                        <ul style='padding-left: 20px; color: #555;'>
                            <li>Cliquez sur le lien ci-dessus pour accéder à votre espace.</li>
                            <li>Partagez ce même lien à vos membres pour qu'ils rejoignent le groupe directement.</li>
                        </ul>

                        <div style='margin-top: 30px; padding-top: 20px; border-top: 1px solid #eee; font-size: 13px; color: #888; text-align: center;'>
                            <p>Besoin d'assistance ? Contactez Eric Gr sur Facebook ou WhatsApp.</p>
                            <p>© 2026 Tontine Gestion - Madagascar</p>
                        </div>
                    </div>
                </div>";
            }
            else
            {
                sujet = $"⚠️ Notification importante : Suspension de votre accès '{groupe.Nom}'";

messageBody = $@"
<div style='font-family: ""Segoe UI"", Tahoma, Geneva, Verdana, sans-serif; max-width: 600px; margin: auto; border: 1px solid #f5c6cb; border-radius: 10px; overflow: hidden; box-shadow: 0 4px 6px rgba(0,0,0,0.05);'>
    <div style='background-color: #dc3545; padding: 15px; text-align: center;'>
        <h2 style='color: white; margin: 0; font-size: 20px;'>Avis de suspension d'accès</h2>
    </div>
    
    <div style='padding: 30px; color: #444; line-height: 1.6;'>
        <p style='font-size: 16px;'>Bonjour <strong>{groupe.NomAdmin}</strong>,</p>
        
        <p>Nous vous informons que l'accès à votre espace de gestion pour la tontine <strong>{groupe.Nom}</strong> a été temporairement suspendu.</p>
        
        <div style='background-color: #fff3cd; border: 1px solid #ffeeba; border-radius: 8px; padding: 15px; margin: 20px 0; color: #856404;'>
            <p style='margin: 0;'><strong>Pourquoi ce message ?</strong><br/>
            Cette mesure est généralement prise suite à un défaut de paiement des frais de gestion ou au non-respect des conditions d'utilisation.</p>
        </div>

        <p><strong>Comment rétablir votre accès ?</strong></p>
        <p>Pour régulariser votre situation et réactiver votre groupe immédiatement, veuillez nous contacter via l'un des canaux suivants :</p>
        
        <div style='margin: 20px 0; padding-left: 10px;'>
            <p>💬 <strong>WhatsApp :</strong> +261 32 64 572 08</p>
            <p>👤 <strong>Facebook :</strong> Eric Gr</p>
            <p>📱 <strong>M-Vola :</strong> +261 34 64 474 61 (Makalahiarison)</p>
        </div>

        <p style='font-size: 14px; color: #777;'>Une fois la régularisation effectuée, votre accès sera rétabli dans un délai de 2 heures.</p>

        <div style='margin-top: 30px; padding-top: 20px; border-top: 1px solid #eee; font-size: 12px; color: #999; text-align: center;'>
            <p>Merci de votre compréhension.<br/>Support Technique - Tontine Gestion</p>
        </div>
    </div>
</div>";
            }
                    await _emailService.EnvoyerEmailAsync(groupe.AdminEmail, sujet, messageBody);
                }
                catch (Exception ex)
                {
                    // Optionnel : Vous pouvez ajouter un logger ici si vous l'avez injecté
                    // _logger.LogError(ex, "Erreur envoi mail lors du ToggleValidation");
                }
            }

            return RedirectToAction(nameof(Dashboard));
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction(nameof(Login));
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteGroupe(int id, string passwordConfirm)
        {
            // 1. Vérification du mot de passe (Remplacez "VotreMotDePasse" par votre logique)
            if (passwordConfirm != "819600")
            {
                TempData["ErrorMessage"] = "❌ Mot de passe incorrect. Suppression annulée.";
                return RedirectToAction(nameof(Dashboard));
            }

            var groupe = await _context.Groupes.FindAsync(id);
            if (groupe != null)
            {
                _context.Groupes.Remove(groupe);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "✅ La tontine a été supprimée définitivement.";
            }

            return RedirectToAction(nameof(Dashboard));
        }
// 🔄 Action pour renouveler la tontine (Réinitialise la date de création)
// 🔄 Action pour renouveler la tontine avec vérification de mot de passe
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> RenewDate(int id, string passwordRenew)
{
    if (HttpContext.Session.GetString("IsAdminDev") != "true") return Unauthorized();

    // Vérification du mot de passe
    if (passwordRenew != "819600") 
    {
        TempData["ErrorMessage"] = "❌ Mot de passe incorrect. Renouvellement annulé.";
        return RedirectToAction(nameof(Dashboard));
    }

    var groupe = await _context.Groupes.FindAsync(id);
    if (groupe != null)
    {
        groupe.DateCreation = DateTime.Now; 
        _context.Update(groupe);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"🔄 La tontine '{groupe.Nom}' a été renouvelée avec succès à la date d'aujourd'hui.";
    }

    return RedirectToAction(nameof(Dashboard));
}
    }
}