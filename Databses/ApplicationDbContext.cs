using EventPulse.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EventPulse.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<Event> Events => Set<Event>();
        public DbSet<Registration> Registrations => Set<Registration>();
        public DbSet<Payment> Payments => Set<Payment>();
        public DbSet<WaitlistEntry> WaitlistEntries => Set<WaitlistEntry>();
        public DbSet<Notification> Notifications => Set<Notification>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Registration>()
                .HasIndex(r => new { r.EventId, r.UserId })
                .IsUnique();

            builder.Entity<Registration>()
                .HasOne(r => r.Payment)
                .WithOne(p => p.Registration!)
                .HasForeignKey<Payment>(p => p.RegistrationId);

            builder.Entity<Registration>()
                .HasOne(r => r.Event)
                .WithMany(e => e.Registrations)
                .HasForeignKey(r => r.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Registration>()
                .HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<WaitlistEntry>()
                .HasIndex(w => new { w.EventId, w.UserId })
                .IsUnique();

            builder.Entity<WaitlistEntry>()
                .HasOne(w => w.Event)
                .WithMany(e => e.WaitlistEntries)
                .HasForeignKey(w => w.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Registration>()
                .HasIndex(r => r.QrCode)
                .IsUnique();

            builder.Entity<Event>()
                .HasOne(e => e.Organizer)
                .WithMany()
                .HasForeignKey(e => e.OrganizerId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
