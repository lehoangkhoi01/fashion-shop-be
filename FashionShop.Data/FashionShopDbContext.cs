using FashionShop.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace FashionShop.Data
{
    public class FashionShopDbContext : DbContext
    {
        public FashionShopDbContext(DbContextOptions<FashionShopDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Catalog> Catalogs { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Inventory> Inventories { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure JSONB for Product Properties if needed explicitly, 
            // but the attribute [Column(TypeName = "jsonb")] in the entity usually suffices for Npgsql.
            
            // Additional configurations can go here
            modelBuilder.Entity<Order>()
                .HasOne(o => o.User)
                .WithMany()
                .HasForeignKey(o => o.UserId)
                .IsRequired(false);

            // Global query filters for soft delete
            modelBuilder.Entity<Product>()
                .HasQueryFilter(p => !p.IsDeleted);

            modelBuilder.Entity<Catalog>()
                .HasQueryFilter(c => !c.IsDeleted);
        }
    }
}
