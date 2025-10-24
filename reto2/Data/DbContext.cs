using Microsoft.EntityFrameworkCore;
using reto2.Models;

namespace reto2.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Recaudo> Recaudos { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Recaudo>(entity =>
            {
                entity.ToTable("recaudos", "reto_keiner");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Estacion).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Sentido).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Hora).IsRequired();
                entity.Property(e => e.Categoria).IsRequired().HasMaxLength(50);
                entity.Property(e => e.ValorTabulado).HasColumnType("decimal(18,2)");
                entity.Property(e => e.FechaRegistro).HasDefaultValueSql("GETDATE()");
            });
        }
    }
}