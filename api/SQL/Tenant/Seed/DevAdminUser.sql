-- Tenant DB: Development admin user seed
-- Creates a default admin user for the dev tenant.
-- Password: Admin@123 (BCrypt hash)
-- Safe to run multiple times.

IF NOT EXISTS (SELECT 1 FROM dbo.AdminUsers WHERE Username = 'admin')
BEGIN
    INSERT INTO dbo.AdminUsers (Id, Username, Email, PasswordHash, FullName, Role, IsActive)
    VALUES (
        'C0000000-0000-0000-0000-000000000001',
        'admin',
        'admin@zentech.com.au',
        '$2a$11$K3Q6Fh3k0V8eZ5x7J9sXAe4nRz1.3X4mH2gY7dW9cB5aE8fGhIjK0',  -- BCrypt hash of Admin@123
        'Dev Administrator',
        'SuperAdmin',
        1
    );
END
