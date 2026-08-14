## Team — Group 18

| Application Number | Name |
|---|---|
| IN26011309 | Dhrubo Dutta |
| IN26011617 | Arpit Ambastha |
| IN26010754 | Sarthak Srivastava |
| IN26011121 | Aurko Chatterjee |
| IN26010977 | Abhinash Ashish |
| IN26011220 | Shivam Sharma |
| IN26012006 | Parthib Datta Muhuri |

# EventPulse

**Event registration that keeps every seat, sign-up, and organizer in sync.**

EventPulse is a web-based event registration platform built on ASP.NET Core. It replaces manual, spreadsheet-based event sign-ups with a structured system for browsing events, registering within real-time seat capacity, handling payments and refunds, managing waitlists, and checking attendees in via QR code.

---

## Problem Statement

Organizers of events such as workshops and seminars often manage sign-ups manually through spreadsheets or email threads. This leads to:

- Overbooking, since there's no automated capacity control
- No self-service registration for attendees
- No structured tracking of payments or refunds
- No reliable way to record attendance at the event itself

EventPulse addresses all four by digitizing the full event lifecycle — from browsing and registration through payment, cancellation, waitlisting, and check-in.

---

## Features

### Event Registration & Capacity
- Attendees can browse and search events by name, date, and category
- Seats remaining are shown in real time
- Registration is automatically blocked once an event reaches capacity, using a transaction-safe check to prevent double-booking the last seat

### Payment & Refund (mock)
- Paid events go through a simulated checkout before registration is confirmed
- Free events skip the payment step entirely
- Cancelling a paid registration automatically triggers a refund
- Attendees and organizers can view payment/transaction history

### Cancellation
- Attendees can cancel a confirmed registration before the event date
- Cancellation frees the seat and triggers a refund if applicable

### Auto-Queuing (Waitlist)
- Attendees can join a waitlist when an event is full instead of being blocked outright
- Waitlist is FIFO (first-in, first-out)
- When a seat frees up, the next waitlisted attendee is automatically promoted to a confirmed registration

### QR Code Check-in
- A unique QR code is generated for every confirmed registration
- Organizers scan the code at the event to mark attendance
- Each code is single-use — a repeat scan is flagged as already checked in

### Dashboards
- **Attendee:** upcoming events, registrations, payment history
- **Organizer:** event list, attendee lists, revenue, check-in status
- **Admin:** platform-wide overview of events and organizers

---

## Tech Stack

| Layer | Technology |
|---|---|
| Backend | ASP.NET Core MVC |
| Database | SQL Server + Entity Framework Core |
| Authentication | ASP.NET Core Identity (role-based) |
| Frontend | Razor Views + Bootstrap |

---

## User Roles & Permissions

| Role | Permissions |
|---|---|
| **Attendee** | Browse events, register, pay, view own registrations, cancel, receive refund, view QR ticket |
| **Organizer** | Create/edit/delete own events, set capacity & price, view attendee list, view transactions, scan QR for check-in |
| **Admin** | Manage organizer accounts, view all events and transactions across the platform |

---

## System Architecture

EventPulse follows a layered ASP.NET Core MVC architecture:

- **Presentation Layer** — Razor views and Controllers (Event, Registration, Payment, CheckIn)
- **Application/Service Layer** — `EventService`, `RegistrationService`, `PaymentService`, `WaitlistService`, `QrService`
- **Data Layer** — EF Core `DbContext` and repositories
- **Identity** — ASP.NET Core Identity for authentication and role-based authorization

### Key Workflow — Registering for a Paid Event
1. Attendee selects an event and clicks Register; system checks `SeatsRemaining`
2. If seats are available, the attendee is routed to the mock payment step
3. On payment success, a `Registration` (Confirmed) and `Payment` (Completed) record are created, and a QR code is generated
4. If seats are unavailable, the attendee is offered the option to join the waitlist instead

### Key Workflow — Cancellation
1. Attendee cancels; `Registration` status changes to Cancelled
2. If the registration was paid, the linked `Payment` status changes to Refunded
3. `SeatsRemaining` is incremented
4. If the event has a non-empty waitlist, the next entry is automatically promoted to a Confirmed registration

---

## Database Schema

| Entity | Key Fields |
|---|---|
| `User` | Id, Name, Email, Role (Attendee / Organizer / Admin) |
| `Event` | Id, Name, Description, Date, Location, Category, Price, Capacity, SeatsRemaining, OrganizerId |
| `Registration` | Id, EventId, UserId, Status (Confirmed / Cancelled / Waitlisted), RegisteredAt, QrCode, CheckedIn |
| `Payment` | Id, RegistrationId, Amount, Status (Pending / Completed / Refunded / Failed), PaymentMethod, TransactionDate |
| `Waitlist` | Id, EventId, UserId, Position, JoinedAt |

**Relationships:** one `Event` has many `Registrations` and many `Waitlist` entries; one `Registration` has at most one `Payment`; one `User` can have many `Registrations` across different events.

---

## Getting Started

### Prerequisites
- .NET SDK (8.0 or later)
- SQL Server / SQL Server Express / LocalDB
- Visual Studio or VS Code

### Setup
```bash
# Clone the repository
git clone <repository-url>
cd EventPulse

# Restore dependencies
dotnet restore

# Update the connection string in appsettings.json to point to your SQL Server instance

# Apply database migrations
dotnet ef database update

# Run the application
dotnet run
```

The app will be available at `https://localhost:<port>` once running.

---

## Non-Functional Requirements

- Role-based access control enforced at both UI and backend levels
- No registration beyond capacity; no double registration by the same attendee
- Paginated, filterable lists for performance at scale
- Every registration, payment, refund, and check-in action is timestamped for auditability

---

## Roadmap

Planned but not yet implemented:
- Email notifications for registration confirmation, cancellation, and waitlist promotion
- Integration with a real payment gateway (Razorpay/Stripe sandbox) in place of the mock service

---



## License

This project was built for academic purposes as part of a coursework submission.
