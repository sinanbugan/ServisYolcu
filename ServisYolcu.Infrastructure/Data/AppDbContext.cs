using Microsoft.EntityFrameworkCore;
using ServisYolcu.Core.Entities;

namespace ServisYolcu.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Route> Routes => Set<Route>();
    public DbSet<Trip> Trips => Set<Trip>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<Menu> Menus => Set<Menu>();
    public DbSet<MenuRole> MenuRoles => Set<MenuRole>();
    public DbSet<Stop> Stops => Set<Stop>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.Email).IsRequired().HasMaxLength(100);
            entity.Property(u => u.FirstName).IsRequired().HasMaxLength(50);
            entity.Property(u => u.LastName).IsRequired().HasMaxLength(50);
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasOne(rt => rt.User)
                  .WithMany(u => u.RefreshTokens)
                  .HasForeignKey(rt => rt.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Trip>(entity =>
        {
            entity.HasOne(t => t.Route)
                  .WithMany(r => r.Trips)
                  .HasForeignKey(t => t.RouteId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(t => t.Driver)
                  .WithMany()
                  .HasForeignKey(t => t.DriverId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Reservation>(entity =>
        {
            entity.HasOne(r => r.Trip)
                  .WithMany(t => t.Reservations)
                  .HasForeignKey(r => r.TripId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(r => r.Passenger)
                  .WithMany(u => u.Reservations)
                  .HasForeignKey(r => r.PassengerId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(r => r.BoardingStop)
                  .WithMany()
                  .HasForeignKey(r => r.BoardingStopId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Route>(entity =>
        {
            entity.Property(r => r.PricePerSeat).HasColumnType("decimal(10,2)");
        });

        modelBuilder.Entity<Stop>(entity =>
        {
            entity.Property(s => s.Name).IsRequired().HasMaxLength(150);
            entity.Property(s => s.Address).HasMaxLength(250);

            entity.HasOne(s => s.Route)
                  .WithMany(r => r.Stops)
                  .HasForeignKey(s => s.RouteId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Menu>(entity =>
        {
            entity.HasIndex(m => m.Key).IsUnique();
            entity.Property(m => m.Key).IsRequired().HasMaxLength(100);
            entity.Property(m => m.Label).IsRequired().HasMaxLength(150);
            entity.Property(m => m.Path).HasMaxLength(200);
            entity.Property(m => m.Icon).HasMaxLength(100);

            entity.HasOne(m => m.Parent)
                  .WithMany(m => m.Children)
                  .HasForeignKey(m => m.ParentId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<MenuRole>(entity =>
        {
            entity.HasIndex(mr => new { mr.MenuId, mr.Role }).IsUnique();

            entity.HasOne(mr => mr.Menu)
                  .WithMany(m => m.MenuRoles)
                  .HasForeignKey(mr => mr.MenuId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
