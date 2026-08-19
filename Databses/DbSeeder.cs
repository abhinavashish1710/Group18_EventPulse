using EventPulse.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EventPulse.Data
{
    /// <summary>
    /// Seeds the database with Identity roles and a small amount of demo data
    /// so the app isn't empty on first run. Called once from Program.cs at startup.
    /// </summary>
    public static class DbSeeder
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            var db = services.GetRequiredService<ApplicationDbContext>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

            await db.Database.MigrateAsync();

            // ---- Roles ----
            foreach (var role in new[] { "Attendee", "Organizer", "Admin" })
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }

            // ---- Demo users (only created once) ----
            async Task<ApplicationUser> EnsureUser(string email, string fullName, UserRole role, string password)
            {
                var existing = await userManager.FindByEmailAsync(email);
                if (existing is not null) return existing;

                var user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    FullName = fullName,
                    Role = role,
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(user, password);
                await userManager.AddToRoleAsync(user, role.ToString());
                return user;
            }

            var admin = await EnsureUser("admin@eventpulse.com", "Admin User", UserRole.Admin, "Admin@123");
            var organizer = await EnsureUser("organizer@eventpulse.com", "Demo Organizer", UserRole.Organizer, "Organizer@123");
            await EnsureUser("attendee@eventpulse.com", "Demo Attendee", UserRole.Attendee, "Attendee@123");

            // ---- Demo event (only created once) ----
            if (!await db.Events.AnyAsync())
            {
                db.Events.Add(new Event
                {
                    Name = "Web Development Bootcamp",
                    Description = "A hands-on full-stack workshop covering ASP.NET Core and EF Core.",
                    Category = "Workshop",
                    EventDate = DateTime.UtcNow.AddDays(21),
                    Location = "Bhopal Tech Park",
                    Price = 499.00m,
                    Capacity = 30,
                    SeatsRemaining = 30,
                    OrganizerId = organizer.Id
                });
                await db.SaveChangesAsync();
            }
        }
    }
}
