using Microsoft.EntityFrameworkCore;
using Tontine.Models;

namespace Tontine.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Groupe> Groupes { get; set; }
        public DbSet<Membre> Membres { get; set; }
        public DbSet<Versement> Versements { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Groupe -> Membres (1 à many)
            modelBuilder.Entity<Membre>()
                .HasOne(m => m.Groupe)
                .WithMany(g => g.Membres)
                .HasForeignKey(m => m.GroupeId)
                .OnDelete(DeleteBehavior.Cascade);

            // Membre -> Versements (1 à many)
            modelBuilder.Entity<Versement>()
                .HasOne(v => v.Membre)
                .WithMany(m => m.Versements)
                .HasForeignKey(v => v.MembreId)
                .OnDelete(DeleteBehavior.Cascade);

            // Groupe -> Versements (1 à many)
            modelBuilder.Entity<Versement>()
                .HasOne(v => v.Groupe)
                .WithMany(g => g.Versements)
                .HasForeignKey(v => v.GroupeId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}