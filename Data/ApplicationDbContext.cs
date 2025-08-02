using Microsoft.EntityFrameworkCore;
using NeshanGeocodingApi.Models;

namespace NeshanGeocodingApi.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Address> Addresses { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Address>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.FullAddress).IsRequired().HasMaxLength(500);
                entity.Property(e => e.Province).HasMaxLength(100);
                entity.Property(e => e.City).HasMaxLength(100);
                entity.Property(e => e.District).HasMaxLength(100);
                entity.Property(e => e.Street).HasMaxLength(200);
                entity.Property(e => e.Alley).HasMaxLength(200);
                entity.Property(e => e.Building).HasMaxLength(100);
                entity.Property(e => e.GeocodedAddress).HasMaxLength(500);
                entity.Property(e => e.Status).HasMaxLength(50);
                entity.Property(e => e.ErrorMessage).HasMaxLength(500);
            });
        }
    }
} 