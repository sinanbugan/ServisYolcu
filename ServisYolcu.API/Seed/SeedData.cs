using Microsoft.EntityFrameworkCore;
using ServisYolcu.Core.Entities;
using ServisYolcu.Infrastructure.Data;

namespace Seed;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<AppDbContext>();

        // Ensure database is created and migrations applied
        await db.Database.MigrateAsync();

        // Company
        var company = await db.Companies.FirstOrDefaultAsync(c => c.CompanyCode == "COMPANY1");
        if (company == null)
        {
            company = new Company { Name = "Demo Company", CompanyCode = "COMPANY1" };
            db.Companies.Add(company);
            await db.SaveChangesAsync();
        }

        // Users: driver and passenger
        var driver = await db.Users.FirstOrDefaultAsync(u => u.Email == "driver@example.com");
        if (driver == null)
        {
            driver = new User { FirstName = "Driver", LastName = "Demo", Email = "driver@example.com", CompanyId = company.Id };
            db.Users.Add(driver);
            await db.SaveChangesAsync();
        }

        var passenger = await db.Users.FirstOrDefaultAsync(u => u.Email == "passenger@example.com");
        if (passenger == null)
        {
            passenger = new User { FirstName = "Passenger", LastName = "Demo", Email = "passenger@example.com", CompanyId = company.Id };
            db.Users.Add(passenger);
            await db.SaveChangesAsync();
        }

        // Route
        var route = await db.Routes.FirstOrDefaultAsync(r => r.Name == "Demo Route");
        if (route == null)
        {
            route = new ServisYolcu.Core.Entities.Route { Name = "Demo Route", StartPoint = "A", EndPoint = "B", CompanyId = company.Id, PricePerSeat = 25m };
            db.Routes.Add(route);
            await db.SaveChangesAsync();
        }

        // Trip for next month and for this month
        var nextMonth = DateTime.UtcNow.AddMonths(1);
        var tripDate = new DateTime(nextMonth.Year, nextMonth.Month, 15, 9, 0, 0, DateTimeKind.Utc);

        var trip = await db.Trips.FirstOrDefaultAsync(t => t.RouteId == route.Id && t.DepartureTime == tripDate);
        if (trip == null)
        {
            trip = new Trip { RouteId = route.Id, DriverId = driver.Id, DepartureTime = tripDate, TotalSeats = 20, AvailableSeats = 20 };
            db.Trips.Add(trip);
            await db.SaveChangesAsync();
        }

        // Create a monthly reservation for passenger: days off 1 and 5
        var mr = await db.MonthlyReservations.FirstOrDefaultAsync(m => m.PassengerId == passenger.Id && m.TripId == trip.Id && m.Year == trip.DepartureTime.Year && m.Month == trip.DepartureTime.Month);
        if (mr == null)
        {
            mr = new MonthlyReservation { PassengerId = passenger.Id, TripId = trip.Id, Year = trip.DepartureTime.Year, Month = trip.DepartureTime.Month, DaysOff = "1,5" };
            db.MonthlyReservations.Add(mr);
            await db.SaveChangesAsync();
        }

        Console.WriteLine($"Seeded company {company.Id}, driver {driver.Id}, passenger {passenger.Id}, trip {trip.Id}, monthlyReservation {mr.Id}");
    }
}
