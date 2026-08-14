-- ============================================
-- EventPulse Database Schema (SQL Server / T-SQL)
-- ============================================

CREATE TABLE Users (
    UserId        INT IDENTITY(1,1) PRIMARY KEY,
    Name          NVARCHAR(100) NOT NULL,
    Email         NVARCHAR(150) NOT NULL UNIQUE,
    PasswordHash  NVARCHAR(256) NOT NULL,
    Role          NVARCHAR(20) NOT NULL CHECK (Role IN ('Attendee','Organizer','Admin')),
    CreatedAt     DATETIME NOT NULL DEFAULT GETDATE()
);

CREATE TABLE Events (
    EventId         INT IDENTITY(1,1) PRIMARY KEY,
    Name            NVARCHAR(150) NOT NULL,
    Description     NVARCHAR(MAX),
    Category        NVARCHAR(50),
    EventDate       DATETIME NOT NULL,
    Location        NVARCHAR(150),
    Price           DECIMAL(10,2) NOT NULL DEFAULT 0,
    Capacity        INT NOT NULL CHECK (Capacity >= 0),
    SeatsRemaining  INT NOT NULL CHECK (SeatsRemaining >= 0),
    OrganizerId     INT NOT NULL,
    CreatedAt       DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Events_Organizer FOREIGN KEY (OrganizerId) REFERENCES Users(UserId)
);

CREATE TABLE Registrations (
    RegistrationId INT IDENTITY(1,1) PRIMARY KEY,
    EventId        INT NOT NULL,
    UserId         INT NOT NULL,
    Status         NVARCHAR(20) NOT NULL CHECK (Status IN ('Confirmed','Cancelled','Waitlisted')),
    QrCode         NVARCHAR(100) UNIQUE,
    CheckedIn      BIT NOT NULL DEFAULT 0,
    RegisteredAt   DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Reg_Event FOREIGN KEY (EventId) REFERENCES Events(EventId),
    CONSTRAINT FK_Reg_User  FOREIGN KEY (UserId) REFERENCES Users(UserId),
    CONSTRAINT UQ_Event_User UNIQUE (EventId, UserId)
);

CREATE TABLE Payments (
    PaymentId       INT IDENTITY(1,1) PRIMARY KEY,
    RegistrationId  INT NOT NULL,
    Amount          DECIMAL(10,2) NOT NULL,
    Status          NVARCHAR(20) NOT NULL CHECK (Status IN ('Pending','Completed','Refunded','Failed')),
    PaymentMethod   NVARCHAR(30),
    TransactionDate DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Payment_Registration FOREIGN KEY (RegistrationId) REFERENCES Registrations(RegistrationId)
);

CREATE TABLE Waitlist (
    WaitlistId INT IDENTITY(1,1) PRIMARY KEY,
    EventId    INT NOT NULL,
    UserId     INT NOT NULL,
    Position   INT NOT NULL,
    JoinedAt   DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Waitlist_Event FOREIGN KEY (EventId) REFERENCES Events(EventId),
    CONSTRAINT FK_Waitlist_User  FOREIGN KEY (UserId) REFERENCES Users(UserId),
    CONSTRAINT UQ_Waitlist_Event_User UNIQUE (EventId, UserId)
);
