-- Tenant DB: AdminUsers table
-- Per-tenant admin users with BCrypt password hashing, lockout, and role support.

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'AdminUsers' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.AdminUsers (
        Id                  UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
        Username            NVARCHAR(100)    NOT NULL,
        Email               NVARCHAR(200)    NOT NULL,
        PasswordHash        NVARCHAR(200)    NOT NULL,
        FullName            NVARCHAR(200)    NOT NULL,
        Role                NVARCHAR(50)     NOT NULL DEFAULT 'Admin',
        IsActive            BIT              NOT NULL DEFAULT 1,
        FailedLoginAttempts INT              NOT NULL DEFAULT 0,
        LockoutEnd          DATETIMEOFFSET   NULL,
        LastLoginAt         DATETIMEOFFSET   NULL,
        LastLoginIp         NVARCHAR(45)     NULL,
        CreatedAt           DATETIMEOFFSET   NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedAt           DATETIMEOFFSET   NOT NULL DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_AdminUsers PRIMARY KEY (Id),
        CONSTRAINT UQ_AdminUsers_Username UNIQUE (Username),
        CONSTRAINT UQ_AdminUsers_Email UNIQUE (Email),
        CONSTRAINT CK_AdminUsers_Role CHECK (Role IN ('SuperAdmin', 'Admin', 'Manager', 'Staff'))
    );
END
