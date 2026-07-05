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
      public DbSet<MonthlyReservation> MonthlyReservations => Set<MonthlyReservation>();
    public DbSet<Menu> Menus => Set<Menu>();
    public DbSet<MenuRole> MenuRoles => Set<MenuRole>();
    public DbSet<Stop> Stops => Set<Stop>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<DeviceToken> DeviceTokens => Set<DeviceToken>();
    public DbSet<NotificationLog> NotificationLogs => Set<NotificationLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Company>(entity =>
        {
            entity.HasIndex(c => c.CompanyCode).IsUnique();
            entity.Property(c => c.Name).IsRequired().HasMaxLength(100);
            entity.Property(c => c.CompanyCode).IsRequired().HasMaxLength(50);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.Email).IsRequired().HasMaxLength(100);
            entity.Property(u => u.FirstName).IsRequired().HasMaxLength(50);
            entity.Property(u => u.LastName).IsRequired().HasMaxLength(50);
            entity.Property(u => u.RefNumber).HasMaxLength(50);

            entity.HasOne(u => u.Company)
                  .WithMany(c => c.Users)
                  .HasForeignKey(u => u.CompanyId)
                  .OnDelete(DeleteBehavior.Restrict);
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

            entity.HasOne(r => r.Company)
                  .WithMany(c => c.Routes)
                  .HasForeignKey(r => r.CompanyId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Stop>(entity =>
        {
            entity.Property(s => s.Name).IsRequired().HasMaxLength(150);
            entity.Property(s => s.Address).HasMaxLength(250);

            entity.HasOne(s => s.Route)
                  .WithMany(r => r.Stops)
                  .HasForeignKey(s => s.RouteId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(s => s.Company)
                  .WithMany(c => c.Stops)
                  .HasForeignKey(s => s.CompanyId)
                  .OnDelete(DeleteBehavior.Restrict);
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

        modelBuilder.Entity<MonthlyReservation>(entity =>
        {
            entity.Property(m => m.DaysOff).HasMaxLength(200);

            entity.HasOne(m => m.Trip)
                  .WithMany()
                  .HasForeignKey(m => m.TripId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(m => m.Passenger)
                  .WithMany()
                  .HasForeignKey(m => m.PassengerId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DeviceToken>(entity =>
        {
            entity.HasIndex(dt => dt.Token).IsUnique();
            entity.Property(dt => dt.Token).IsRequired().HasMaxLength(500);
            entity.Property(dt => dt.Platform).HasMaxLength(50);

            entity.HasOne(dt => dt.User)
                  .WithMany()
                  .HasForeignKey(dt => dt.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<NotificationLog>(entity =>
        {
            entity.Property(nl => nl.Title).IsRequired().HasMaxLength(200);
            entity.Property(nl => nl.Body).IsRequired().HasMaxLength(500);
            entity.Property(nl => nl.Type).IsRequired().HasMaxLength(100);
        });
    }
}
