/* ============================================================
   EventPulse — SQL Server Schema
   Generated to match ApplicationDbContext.cs (Code-First / EF Core)

   NOTE: Column types/lengths for domain tables (Events, Registrations, etc.)
   are inferred from how properties are used in ApplicationDbContext.cs and
   DbSeeder.cs, since the actual Models/ classes weren't available when this
   was written. If your real model classes use different types (e.g. NVARCHAR
   lengths, decimal precision), adjust below to match, or better: run
       dotnet ef migrations script -o EventPulse_SQLServer_Schema.sql
   from the project to get the exact, guaranteed-accurate version of this file.
   ============================================================ */

IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'EventPulseDb')
BEGIN
    CREATE DATABASE EventPulseDb;
END
GO

USE EventPulseDb;
GO

/* ============================================================
   IDENTITY TABLES
   (Standard ASP.NET Core Identity schema, extended with
    FullName + Role on AspNetUsers via ApplicationUser)
   ============================================================ */

IF OBJECT_ID('dbo.AspNetRoles', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AspNetRoles (
        Id               NVARCHAR(450)   NOT NULL PRIMARY KEY,
        Name             NVARCHAR(256)   NULL,
        NormalizedName   NVARCHAR(256)   NULL,
        ConcurrencyStamp NVARCHAR(MAX)   NULL
    );
    CREATE UNIQUE INDEX RoleNameIndex ON dbo.AspNetRoles (NormalizedName)
        WHERE NormalizedName IS NOT NULL;
END
GO

IF OBJECT_ID('dbo.AspNetUsers', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AspNetUsers (
        Id                   NVARCHAR(450)   NOT NULL PRIMARY KEY,
        -- ApplicationUser extensions
        FullName             NVARCHAR(200)   NOT NULL,
        Role                 NVARCHAR(50)    NOT NULL,      -- UserRole enum: Attendee / Organizer / Admin
        -- Standard IdentityUser columns
        UserName             NVARCHAR(256)   NULL,
        NormalizedUserName   NVARCHAR(256)   NULL,
        Email                NVARCHAR(256)   NULL,
        NormalizedEmail      NVARCHAR(256)   NULL,
        EmailConfirmed       BIT             NOT NULL DEFAULT 0,
        PasswordHash         NVARCHAR(MAX)   NULL,
        SecurityStamp        NVARCHAR(MAX)   NULL,
        ConcurrencyStamp     NVARCHAR(MAX)   NULL,
        PhoneNumber          NVARCHAR(MAX)   NULL,
        PhoneNumberConfirmed BIT             NOT NULL DEFAULT 0,
        TwoFactorEnabled     BIT             NOT NULL DEFAULT 0,
        LockoutEnd           DATETIMEOFFSET  NULL,
        LockoutEnabled       BIT             NOT NULL DEFAULT 0,
        AccessFailedCount    INT             NOT NULL DEFAULT 0
    );
    CREATE UNIQUE INDEX UserNameIndex ON dbo.AspNetUsers (NormalizedUserName)
        WHERE NormalizedUserName IS NOT NULL;
    CREATE INDEX EmailIndex ON dbo.AspNetUsers (NormalizedEmail);
END
GO

IF OBJECT_ID('dbo.AspNetUserRoles', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AspNetUserRoles (
        UserId NVARCHAR(450) NOT NULL,
        RoleId NVARCHAR(450) NOT NULL,
        PRIMARY KEY (UserId, RoleId),
        CONSTRAINT FK_AspNetUserRoles_AspNetUsers FOREIGN KEY (UserId)
            REFERENCES dbo.AspNetUsers (Id) ON DELETE CASCADE,
        CONSTRAINT FK_AspNetUserRoles_AspNetRoles FOREIGN KEY (RoleId)
            REFERENCES dbo.AspNetRoles (Id) ON DELETE CASCADE
    );
END
GO

IF OBJECT_ID('dbo.AspNetUserClaims', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AspNetUserClaims (
        Id         INT IDENTITY(1,1) PRIMARY KEY,
        UserId     NVARCHAR(450) NOT NULL,
        ClaimType  NVARCHAR(MAX) NULL,
        ClaimValue NVARCHAR(MAX) NULL,
        CONSTRAINT FK_AspNetUserClaims_AspNetUsers FOREIGN KEY (UserId)
            REFERENCES dbo.AspNetUsers (Id) ON DELETE CASCADE
    );
END
GO

IF OBJECT_ID('dbo.AspNetUserLogins', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AspNetUserLogins (
        LoginProvider       NVARCHAR(450) NOT NULL,
        ProviderKey          NVARCHAR(450) NOT NULL,
        ProviderDisplayName NVARCHAR(MAX) NULL,
        UserId               NVARCHAR(450) NOT NULL,
        PRIMARY KEY (LoginProvider, ProviderKey),
        CONSTRAINT FK_AspNetUserLogins_AspNetUsers FOREIGN KEY (UserId)
            REFERENCES dbo.AspNetUsers (Id) ON DELETE CASCADE
    );
END
GO

IF OBJECT_ID('dbo.AspNetUserTokens', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AspNetUserTokens (
        UserId        NVARCHAR(450) NOT NULL,
        LoginProvider NVARCHAR(450) NOT NULL,
        Name          NVARCHAR(450) NOT NULL,
        Value         NVARCHAR(MAX) NULL,
        PRIMARY KEY (UserId, LoginProvider, Name),
        CONSTRAINT FK_AspNetUserTokens_AspNetUsers FOREIGN KEY (UserId)
            REFERENCES dbo.AspNetUsers (Id) ON DELETE CASCADE
    );
END
GO

IF OBJECT_ID('dbo.AspNetRoleClaims', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AspNetRoleClaims (
        Id         INT IDENTITY(1,1) PRIMARY KEY,
        RoleId     NVARCHAR(450) NOT NULL,
        ClaimType  NVARCHAR(MAX) NULL,
        ClaimValue NVARCHAR(MAX) NULL,
        CONSTRAINT FK_AspNetRoleClaims_AspNetRoles FOREIGN KEY (RoleId)
            REFERENCES dbo.AspNetRoles (Id) ON DELETE CASCADE
    );
END
GO

/* ============================================================
   DOMAIN TABLES
   ============================================================ */

IF OBJECT_ID('dbo.Events', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Events (
        Id             NVARCHAR(450)  NOT NULL PRIMARY KEY DEFAULT NEWID(),
        Name           NVARCHAR(200)  NOT NULL,
        Description    NVARCHAR(MAX)  NULL,
        Category       NVARCHAR(100)  NULL,
        EventDate      DATETIME2      NOT NULL,
        Location       NVARCHAR(300)  NULL,
        Price          DECIMAL(10,2)  NOT NULL DEFAULT 0,
        Capacity       INT            NOT NULL,
        SeatsRemaining INT            NOT NULL,
        OrganizerId    NVARCHAR(450)  NOT NULL,
        CONSTRAINT FK_Events_AspNetUsers_OrganizerId FOREIGN KEY (OrganizerId)
            REFERENCES dbo.AspNetUsers (Id) ON DELETE NO ACTION  -- Restrict
    );
    CREATE INDEX IX_Events_OrganizerId ON dbo.Events (OrganizerId);
END
GO

IF OBJECT_ID('dbo.Registrations', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Registrations (
        Id       NVARCHAR(450) NOT NULL PRIMARY KEY DEFAULT NEWID(),
        EventId  NVARCHAR(450) NOT NULL,
        UserId   NVARCHAR(450) NOT NULL,
        QrCode   NVARCHAR(450) NOT NULL,
        CONSTRAINT FK_Registrations_Events_EventId FOREIGN KEY (EventId)
            REFERENCES dbo.Events (Id) ON DELETE CASCADE,
        CONSTRAINT FK_Registrations_AspNetUsers_UserId FOREIGN KEY (UserId)
            REFERENCES dbo.AspNetUsers (Id) ON DELETE NO ACTION  -- Restrict
    );
    -- One registration per user per event
    CREATE UNIQUE INDEX IX_Registrations_EventId_UserId ON dbo.Registrations (EventId, UserId);
    -- Every QR code maps to exactly one registration
    CREATE UNIQUE INDEX IX_Registrations_QrCode ON dbo.Registrations (QrCode);
END
GO

IF OBJECT_ID('dbo.Payments', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Payments (
        Id             NVARCHAR(450)  NOT NULL PRIMARY KEY DEFAULT NEWID(),
        RegistrationId NVARCHAR(450)  NOT NULL,
        Amount         DECIMAL(10,2)  NOT NULL DEFAULT 0,
        Status         NVARCHAR(50)   NOT NULL DEFAULT 'Pending',   -- e.g. Pending / Completed / Refunded
        PaidAt         DATETIME2      NULL,
        CONSTRAINT FK_Payments_Registrations_RegistrationId FOREIGN KEY (RegistrationId)
            REFERENCES dbo.Registrations (Id) ON DELETE CASCADE
    );
    -- One-to-one: a registration has at most one payment
    CREATE UNIQUE INDEX IX_Payments_RegistrationId ON dbo.Payments (RegistrationId);
END
GO

IF OBJECT_ID('dbo.WaitlistEntries', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.WaitlistEntries (
        Id        NVARCHAR(450) NOT NULL PRIMARY KEY DEFAULT NEWID(),
        EventId   NVARCHAR(450) NOT NULL,
        UserId    NVARCHAR(450) NOT NULL,
        JoinedAt  DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_WaitlistEntries_Events_EventId FOREIGN KEY (EventId)
            REFERENCES dbo.Events (Id) ON DELETE CASCADE,
        CONSTRAINT FK_WaitlistEntries_AspNetUsers_UserId FOREIGN KEY (UserId)
            REFERENCES dbo.AspNetUsers (Id) ON DELETE NO ACTION  -- Restrict
    );
    -- One waitlist entry per user per event
    CREATE UNIQUE INDEX IX_WaitlistEntries_EventId_UserId ON dbo.WaitlistEntries (EventId, UserId);
END
GO

IF OBJECT_ID('dbo.Notifications', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Notifications (
        Id        NVARCHAR(450) NOT NULL PRIMARY KEY DEFAULT NEWID(),
        UserId    NVARCHAR(450) NOT NULL,
        Message   NVARCHAR(500) NOT NULL,
        IsRead    BIT           NOT NULL DEFAULT 0,
        CreatedAt DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_Notifications_AspNetUsers_UserId FOREIGN KEY (UserId)
            REFERENCES dbo.AspNetUsers (Id) ON DELETE CASCADE
    );
    CREATE INDEX IX_Notifications_UserId ON dbo.Notifications (UserId);
END
GO

/* ============================================================
   EF MIGRATIONS HISTORY TABLE
   (EF Core creates this automatically to track applied migrations)
   ============================================================ */

IF OBJECT_ID('dbo.__EFMigrationsHistory', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.__EFMigrationsHistory (
        MigrationId    NVARCHAR(150) NOT NULL PRIMARY KEY,
        ProductVersion NVARCHAR(32)  NOT NULL
    );
END
GO

PRINT 'EventPulse schema created successfully.';
GO
