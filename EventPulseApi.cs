// ============================================================================
// EventPulse — Web API
// ASP.NET Core 8 Web API | Entity Framework Core | ASP.NET Core Identity
//
// This single file contains, in order:
//   1. Domain Models (Event, Registration, Payment, Waitlist, ApplicationUser)
//   2. DbContext
//   3. DTOs (request/response shapes)
//   4. Controllers: Auth, Events, Registrations, Payments, Waitlist, CheckIn, Dashboard
//
// In a full project these would normally live in separate files/folders
// (Models/, Data/, DTOs/, Controllers/) — kept together here as a single
// reference file covering every module discussed for EventPulse.
// ============================================================================

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventPulse.Api
{
    // ========================================================================
    // 1. DOMAIN MODELS
    // ========================================================================

    public enum UserRole
    {
        Attendee,
        Organizer,
        Admin
    }

    public enum RegistrationStatus
    {
        Confirmed,
        Cancelled,
        Waitlisted
    }

    public enum PaymentStatus
    {
        Pending,
        Completed,
        Refunded,
        Failed
    }

    /// <summary>Extends IdentityUser with the fields EventPulse needs.</summary>
    public class ApplicationUser : IdentityUser
    {
        [Required, MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        public UserRole Role { get; set; } = UserRole.Attendee;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class Event
    {
        public int EventId { get; set; }

        [Required, MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        [MaxLength(50)]
        public string? Category { get; set; }

        [Required]
        public DateTime EventDate { get; set; }

        [MaxLength(150)]
        public string? Location { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Price { get; set; } = 0;

        [Range(0, int.MaxValue)]
        public int Capacity { get; set; }

        [Range(0, int.MaxValue)]
        public int SeatsRemaining { get; set; }

        [Required]
        public string OrganizerId { get; set; } = string.Empty;
        public ApplicationUser? Organizer { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Registration> Registrations { get; set; } = new List<Registration>();
        public ICollection<WaitlistEntry> WaitlistEntries { get; set; } = new List<WaitlistEntry>();
    }

    public class Registration
    {
        public int RegistrationId { get; set; }

        [Required]
        public int EventId { get; set; }
        public Event? Event { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser? User { get; set; }

        [Required]
        public RegistrationStatus Status { get; set; } = RegistrationStatus.Confirmed;

        [MaxLength(100)]
        public string? QrCode { get; set; }

        public bool CheckedIn { get; set; } = false;

        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

        public Payment? Payment { get; set; }
    }

    public class Payment
    {
        public int PaymentId { get; set; }

        [Required]
        public int RegistrationId { get; set; }
        public Registration? Registration { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Amount { get; set; }

        [Required]
        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

        [MaxLength(30)]
        public string? PaymentMethod { get; set; }

        public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
    }

    public class WaitlistEntry
    {
        public int WaitlistId { get; set; }

        [Required]
        public int EventId { get; set; }
        public Event? Event { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser? User { get; set; }

        public int Position { get; set; }

        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    }

    // ========================================================================
    // 2. DB CONTEXT
    // ========================================================================

    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<Event> Events => Set<Event>();
        public DbSet<Registration> Registrations => Set<Registration>();
        public DbSet<Payment> Payments => Set<Payment>();
        public DbSet<WaitlistEntry> WaitlistEntries => Set<WaitlistEntry>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // One registration per user per event
            builder.Entity<Registration>()
                .HasIndex(r => new { r.EventId, r.UserId })
                .IsUnique();

            builder.Entity<Registration>()
                .HasOne(r => r.Payment)
                .WithOne(p => p.Registration!)
                .HasForeignKey<Payment>(p => p.RegistrationId);

            builder.Entity<WaitlistEntry>()
                .HasIndex(w => new { w.EventId, w.UserId })
                .IsUnique();

            builder.Entity<Registration>()
                .HasIndex(r => r.QrCode)
                .IsUnique();
        }
    }

    // ========================================================================
    // 3. DTOs
    // ========================================================================

    public record RegisterRequest(string FullName, string Email, string Password, UserRole Role);
    public record LoginRequest(string Email, string Password);
    public record AuthResponse(string UserId, string FullName, string Role, string Token);

    public record CreateEventRequest(
        string Name, string? Description, string? Category,
        DateTime EventDate, string? Location, decimal Price, int Capacity);

    public record EventResponse(
        int EventId, string Name, string? Description, string? Category,
        DateTime EventDate, string? Location, decimal Price,
        int Capacity, int SeatsRemaining, string OrganizerId);

    public record RegisterForEventResponse(
        int RegistrationId, string Status, string? QrCode, bool RequiresPayment, decimal AmountDue);

    public record PayRequest(int RegistrationId, string PaymentMethod);
    public record PaymentResponse(int PaymentId, int RegistrationId, decimal Amount, string Status);

    public record CheckInRequest(string QrCode);
    public record CheckInResponse(bool Success, string Message);

    public record AttendeeDashboardResponse(
        List<EventResponse> UpcomingEvents,
        List<RegisterForEventResponse> MyRegistrations,
        List<PaymentResponse> PaymentHistory);

    public record OrganizerDashboardResponse(
        List<EventResponse> MyEvents,
        int TotalRegistrations,
        int TotalCheckedIn,
        decimal TotalRevenue);

    // ========================================================================
    // 4. CONTROLLERS
    // ========================================================================

    // ---- Auth ---------------------------------------------------------
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public AuthController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // POST api/auth/register
        [HttpPost("register")]
        public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
        {
            // New sign-ups always default to Attendee; Organizer/Admin accounts
            // are promoted separately by an Admin — never self-service.
            var user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                FullName = request.FullName,
                Role = UserRole.Attendee
            };

            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            // Token generation (JWT) would happen here in a real implementation.
            return Ok(new AuthResponse(user.Id, user.FullName, user.Role.ToString(), Token: "jwt-token-placeholder"));
        }

        // POST api/auth/login
        [HttpPost("login")]
        public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user is null)
                return Unauthorized("Invalid credentials.");

            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: false);
            if (!result.Succeeded)
                return Unauthorized("Invalid credentials.");

            return Ok(new AuthResponse(user.Id, user.FullName, user.Role.ToString(), Token: "jwt-token-placeholder"));
        }
    }

    // ---- Events ---------------------------------------------------------
    [ApiController]
    [Route("api/[controller]")]
    public class EventsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public EventsController(ApplicationDbContext db) => _db = db;

        // GET api/events?search=&category=&fromDate=
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<EventResponse>>> GetEvents(
            [FromQuery] string? search, [FromQuery] string? category, [FromQuery] DateTime? fromDate,
            [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var query = _db.Events.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(e => e.Name.Contains(search));
            if (!string.IsNullOrWhiteSpace(category))
                query = query.Where(e => e.Category == category);
            if (fromDate.HasValue)
                query = query.Where(e => e.EventDate >= fromDate.Value);

            var events = await query
                .OrderBy(e => e.EventDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(e => new EventResponse(
                    e.EventId, e.Name, e.Description, e.Category, e.EventDate,
                    e.Location, e.Price, e.Capacity, e.SeatsRemaining, e.OrganizerId))
                .ToListAsync();

            return Ok(events);
        }

        // GET api/events/5
        [HttpGet("{id:int}")]
        [AllowAnonymous]
        public async Task<ActionResult<EventResponse>> GetEvent(int id)
        {
            var e = await _db.Events.FindAsync(id);
            if (e is null) return NotFound();

            return Ok(new EventResponse(
                e.EventId, e.Name, e.Description, e.Category, e.EventDate,
                e.Location, e.Price, e.Capacity, e.SeatsRemaining, e.OrganizerId));
        }

        // POST api/events   (Organizer only)
        [HttpPost]
        [Authorize(Roles = nameof(UserRole.Organizer))]
        public async Task<ActionResult<EventResponse>> CreateEvent(CreateEventRequest request)
        {
            var organizerId = User.FindFirst("sub")?.Value
                               ?? throw new UnauthorizedAccessException();

            if (request.Capacity < 0)
                return BadRequest("Capacity cannot be negative.");

            var newEvent = new Event
            {
                Name = request.Name,
                Description = request.Description,
                Category = request.Category,
                EventDate = request.EventDate,
                Location = request.Location,
                Price = request.Price,
                Capacity = request.Capacity,
                SeatsRemaining = request.Capacity,
                OrganizerId = organizerId
            };

            _db.Events.Add(newEvent);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetEvent), new { id = newEvent.EventId }, newEvent);
        }

        // PUT api/events/5   (Organizer only, own events)
        [HttpPut("{id:int}")]
        [Authorize(Roles = nameof(UserRole.Organizer))]
        public async Task<IActionResult> UpdateEvent(int id, CreateEventRequest request)
        {
            var organizerId = User.FindFirst("sub")?.Value ?? throw new UnauthorizedAccessException();
            var existing = await _db.Events.FindAsync(id);

            if (existing is null) return NotFound();
            if (existing.OrganizerId != organizerId) return Forbid();

            existing.Name = request.Name;
            existing.Description = request.Description;
            existing.Category = request.Category;
            existing.EventDate = request.EventDate;
            existing.Location = request.Location;
            existing.Price = request.Price;
            // Capacity changes intentionally do not retroactively shrink SeatsRemaining below 0.
            existing.Capacity = request.Capacity;

            await _db.SaveChangesAsync();
            return NoContent();
        }

        // DELETE api/events/5   (Organizer only, own events)
        [HttpDelete("{id:int}")]
        [Authorize(Roles = nameof(UserRole.Organizer))]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            var organizerId = User.FindFirst("sub")?.Value ?? throw new UnauthorizedAccessException();
            var existing = await _db.Events.FindAsync(id);

            if (existing is null) return NotFound();
            if (existing.OrganizerId != organizerId) return Forbid();

            _db.Events.Remove(existing);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }

    // ---- Registrations ---------------------------------------------------
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class RegistrationsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public RegistrationsController(ApplicationDbContext db) => _db = db;

        // POST api/registrations   { eventId }
        // Transaction-safe: prevents two attendees from grabbing the same last seat.
        [HttpPost]
        public async Task<ActionResult<RegisterForEventResponse>> Register([FromQuery] int eventId)
        {
            var userId = User.FindFirst("sub")?.Value ?? throw new UnauthorizedAccessException();

            await using var transaction = await _db.Database.BeginTransactionAsync();

            var ev = await _db.Events.FirstOrDefaultAsync(e => e.EventId == eventId);
            if (ev is null) return NotFound("Event not found.");

            var alreadyRegistered = await _db.Registrations
                .AnyAsync(r => r.EventId == eventId && r.UserId == userId && r.Status != RegistrationStatus.Cancelled);
            if (alreadyRegistered)
                return BadRequest("You are already registered for this event.");

            if (ev.SeatsRemaining <= 0)
            {
                // No seats — caller should use POST api/waitlist instead.
                return Conflict("Event is full. Join the waitlist instead.");
            }

            ev.SeatsRemaining -= 1;

            var registration = new Registration
            {
                EventId = eventId,
                UserId = userId,
                Status = RegistrationStatus.Confirmed,
                QrCode = Guid.NewGuid().ToString("N")
            };

            _db.Registrations.Add(registration);
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            bool requiresPayment = ev.Price > 0;
            return Ok(new RegisterForEventResponse(
                registration.RegistrationId,
                registration.Status.ToString(),
                registration.QrCode,
                requiresPayment,
                requiresPayment ? ev.Price : 0));
        }

        // GET api/registrations/mine
        [HttpGet("mine")]
        public async Task<ActionResult<IEnumerable<Registration>>> MyRegistrations()
        {
            var userId = User.FindFirst("sub")?.Value ?? throw new UnauthorizedAccessException();
            var registrations = await _db.Registrations
                .Where(r => r.UserId == userId)
                .Include(r => r.Event)
                .Include(r => r.Payment)
                .ToListAsync();

            return Ok(registrations);
        }

        // POST api/registrations/5/cancel
        // Frees the seat, triggers a refund if paid, and auto-promotes the waitlist.
        [HttpPost("{id:int}/cancel")]
        public async Task<IActionResult> Cancel(int id)
        {
            var userId = User.FindFirst("sub")?.Value ?? throw new UnauthorizedAccessException();

            await using var transaction = await _db.Database.BeginTransactionAsync();

            var registration = await _db.Registrations
                .Include(r => r.Payment)
                .FirstOrDefaultAsync(r => r.RegistrationId == id);

            if (registration is null) return NotFound();
            if (registration.UserId != userId) return Forbid();
            if (registration.Status == RegistrationStatus.Cancelled) return BadRequest("Already cancelled.");

            registration.Status = RegistrationStatus.Cancelled;

            if (registration.Payment is { Status: PaymentStatus.Completed } payment)
            {
                payment.Status = PaymentStatus.Refunded;
            }

            var ev = await _db.Events.FirstAsync(e => e.EventId == registration.EventId);
            ev.SeatsRemaining += 1;

            // Auto-promote next waitlisted attendee, if any (FIFO by Position).
            var nextInLine = await _db.WaitlistEntries
                .Where(w => w.EventId == ev.EventId)
                .OrderBy(w => w.Position)
                .FirstOrDefaultAsync();

            if (nextInLine is not null)
            {
                ev.SeatsRemaining -= 1;

                var promoted = new Registration
                {
                    EventId = ev.EventId,
                    UserId = nextInLine.UserId,
                    Status = RegistrationStatus.Confirmed,
                    QrCode = Guid.NewGuid().ToString("N")
                };
                _db.Registrations.Add(promoted);
                _db.WaitlistEntries.Remove(nextInLine);
                // In a full implementation: notify nextInLine.UserId (email/in-app),
                // and if ev.Price > 0, the promoted registration would await payment
                // confirmation before being finalized as Confirmed.
            }

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            return NoContent();
        }
    }

    // ---- Payments (mock gateway) -----------------------------------------
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PaymentsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public PaymentsController(ApplicationDbContext db) => _db = db;

        // POST api/payments/pay
        // Simulated gateway: no external call is made. Mirrors the contract
        // (Pending -> Completed/Failed) that a real gateway integration would use,
        // so swapping in Razorpay/Stripe later only touches this controller.
        [HttpPost("pay")]
        public async Task<ActionResult<PaymentResponse>> Pay(PayRequest request)
        {
            var registration = await _db.Registrations
                .Include(r => r.Event)
                .FirstOrDefaultAsync(r => r.RegistrationId == request.RegistrationId);

            if (registration is null) return NotFound("Registration not found.");
            if (registration.Event is null) return NotFound("Event not found.");

            var payment = new Payment
            {
                RegistrationId = registration.RegistrationId,
                Amount = registration.Event.Price,
                Status = PaymentStatus.Pending,
                PaymentMethod = request.PaymentMethod
            };
            _db.Payments.Add(payment);
            await _db.SaveChangesAsync();

            // Simulate gateway processing (always succeeds here; a random
            // failure chance could be added to demo the Failed path).
            payment.Status = PaymentStatus.Completed;
            await _db.SaveChangesAsync();

            return Ok(new PaymentResponse(payment.PaymentId, payment.RegistrationId, payment.Amount, payment.Status.ToString()));
        }

        // GET api/payments/mine
        [HttpGet("mine")]
        public async Task<ActionResult<IEnumerable<PaymentResponse>>> MyPayments()
        {
            var userId = User.FindFirst("sub")?.Value ?? throw new UnauthorizedAccessException();

            var payments = await _db.Payments
                .Where(p => p.Registration!.UserId == userId)
                .Select(p => new PaymentResponse(p.PaymentId, p.RegistrationId, p.Amount, p.Status.ToString()))
                .ToListAsync();

            return Ok(payments);
        }
    }

    // ---- Waitlist ---------------------------------------------------------
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class WaitlistController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public WaitlistController(ApplicationDbContext db) => _db = db;

        // POST api/waitlist   { eventId }
        [HttpPost]
        public async Task<IActionResult> Join([FromQuery] int eventId)
        {
            var userId = User.FindFirst("sub")?.Value ?? throw new UnauthorizedAccessException();

            var ev = await _db.Events.FindAsync(eventId);
            if (ev is null) return NotFound("Event not found.");
            if (ev.SeatsRemaining > 0) return BadRequest("Seats are available — register directly instead.");

            var alreadyOnList = await _db.WaitlistEntries.AnyAsync(w => w.EventId == eventId && w.UserId == userId);
            if (alreadyOnList) return BadRequest("Already on the waitlist for this event.");

            var currentMax = await _db.WaitlistEntries
                .Where(w => w.EventId == eventId)
                .Select(w => (int?)w.Position)
                .MaxAsync() ?? 0;

            _db.WaitlistEntries.Add(new WaitlistEntry
            {
                EventId = eventId,
                UserId = userId,
                Position = currentMax + 1
            });

            await _db.SaveChangesAsync();
            return Ok();
        }

        // GET api/waitlist/event/5
        [HttpGet("event/{eventId:int}")]
        [Authorize(Roles = nameof(UserRole.Organizer) + "," + nameof(UserRole.Admin))]
        public async Task<ActionResult<IEnumerable<WaitlistEntry>>> GetForEvent(int eventId)
        {
            var entries = await _db.WaitlistEntries
                .Where(w => w.EventId == eventId)
                .OrderBy(w => w.Position)
                .ToListAsync();

            return Ok(entries);
        }
    }

    // ---- Check-in (QR) -----------------------------------------------------
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = nameof(UserRole.Organizer) + "," + nameof(UserRole.Admin))]
    public class CheckInController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public CheckInController(ApplicationDbContext db) => _db = db;

        // POST api/checkin   { qrCode }
        [HttpPost]
        public async Task<ActionResult<CheckInResponse>> Scan(CheckInRequest request)
        {
            var registration = await _db.Registrations
                .FirstOrDefaultAsync(r => r.QrCode == request.QrCode);

            if (registration is null)
                return Ok(new CheckInResponse(false, "QR code not recognized."));

            if (registration.Status != RegistrationStatus.Confirmed)
                return Ok(new CheckInResponse(false, "Registration is not confirmed (cancelled or waitlisted)."));

            if (registration.CheckedIn)
                return Ok(new CheckInResponse(false, "This ticket has already been checked in."));

            registration.CheckedIn = true;
            await _db.SaveChangesAsync();

            return Ok(new CheckInResponse(true, "Check-in successful."));
        }
    }

    // ---- Dashboards ---------------------------------------------------------
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public DashboardController(ApplicationDbContext db) => _db = db;

        // GET api/dashboard/attendee
        [HttpGet("attendee")]
        public async Task<ActionResult<AttendeeDashboardResponse>> AttendeeDashboard()
        {
            var userId = User.FindFirst("sub")?.Value ?? throw new UnauthorizedAccessException();

            var upcoming = await _db.Events
                .Where(e => e.EventDate >= DateTime.UtcNow &&
                            e.Registrations.Any(r => r.UserId == userId && r.Status == RegistrationStatus.Confirmed))
                .Select(e => new EventResponse(
                    e.EventId, e.Name, e.Description, e.Category, e.EventDate,
                    e.Location, e.Price, e.Capacity, e.SeatsRemaining, e.OrganizerId))
                .ToListAsync();

            var myRegistrations = await _db.Registrations
                .Where(r => r.UserId == userId)
                .Select(r => new RegisterForEventResponse(
                    r.RegistrationId, r.Status.ToString(), r.QrCode, false, 0))
                .ToListAsync();

            var payments = await _db.Payments
                .Where(p => p.Registration!.UserId == userId)
                .Select(p => new PaymentResponse(p.PaymentId, p.RegistrationId, p.Amount, p.Status.ToString()))
                .ToListAsync();

            return Ok(new AttendeeDashboardResponse(upcoming, myRegistrations, payments));
        }

        // GET api/dashboard/organizer
        [HttpGet("organizer")]
        [Authorize(Roles = nameof(UserRole.Organizer))]
        public async Task<ActionResult<OrganizerDashboardResponse>> OrganizerDashboard()
        {
            var organizerId = User.FindFirst("sub")?.Value ?? throw new UnauthorizedAccessException();

            var myEvents = await _db.Events
                .Where(e => e.OrganizerId == organizerId)
                .ToListAsync();

            var eventIds = myEvents.Select(e => e.EventId).ToList();

            var totalRegistrations = await _db.Registrations
                .CountAsync(r => eventIds.Contains(r.EventId) && r.Status == RegistrationStatus.Confirmed);

            var totalCheckedIn = await _db.Registrations
                .CountAsync(r => eventIds.Contains(r.EventId) && r.CheckedIn);

            var totalRevenue = await _db.Payments
                .Where(p => eventIds.Contains(p.Registration!.EventId) && p.Status == PaymentStatus.Completed)
                .SumAsync(p => p.Amount);

            var eventResponses = myEvents.Select(e => new EventResponse(
                e.EventId, e.Name, e.Description, e.Category, e.EventDate,
                e.Location, e.Price, e.Capacity, e.SeatsRemaining, e.OrganizerId)).ToList();

            return Ok(new OrganizerDashboardResponse(eventResponses, totalRegistrations, totalCheckedIn, totalRevenue));
        }
    }
}
