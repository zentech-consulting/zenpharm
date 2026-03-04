using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Api.Features.Platform;

// ── Request / Response DTOs ────────────────────────────────────────

public sealed record CreateCheckoutRequest
{
    [Required, StringLength(200, MinimumLength = 2)]
    public required string PharmacyName { get; init; }

    [Required, StringLength(63, MinimumLength = 3)]
    [RegularExpression(@"^[a-z0-9]([a-z0-9\-]{0,61}[a-z0-9])?$",
        ErrorMessage = "Subdomain must be lowercase alphanumeric with hyphens, 3-63 characters.")]
    public required string Subdomain { get; init; }

    [Required, EmailAddress, StringLength(200)]
    public required string AdminEmail { get; init; }

    [Required, StringLength(200, MinimumLength = 2)]
    public required string AdminFullName { get; init; }

    [Required]
    public required Guid PlanId { get; init; }

    [RegularExpression(@"^(Monthly|Yearly)$")]
    public string BillingPeriod { get; init; } = "Monthly";
}

public sealed record CheckoutResponse(string CheckoutUrl, string SessionId);

public sealed record SignupStatusResponse(
    string Status,
    Guid? TenantId,
    string? AdminPanelUrl,
    string? Message);

public sealed record PlanSummary(
    Guid Id,
    string Name,
    decimal PriceMonthly,
    decimal PriceYearly,
    string? Features,
    int MaxUsers,
    int MaxProducts);

// ── Database Entities ──────────────────────────────────────────────

internal sealed record PendingSignupEntity
{
    public Guid Id { get; init; }
    public string PharmacyName { get; init; } = "";
    public string Subdomain { get; init; } = "";
    public string AdminEmail { get; init; } = "";
    public string AdminFullName { get; init; } = "";
    public Guid PlanId { get; init; }
    public string BillingPeriod { get; init; } = "Monthly";
    public string? StripeSessionId { get; init; }
    public string? StripeCustomerId { get; init; }
    public string? StripeSubscriptionId { get; init; }
    public string Status { get; init; } = "pending_payment";
    public Guid? TenantId { get; init; }
    public string? FailureReason { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}

// ── Validation Helpers ─────────────────────────────────────────────

internal static partial class SignupValidation
{
    private static readonly HashSet<string> ReservedSubdomains = new(StringComparer.OrdinalIgnoreCase)
    {
        "www", "api", "admin", "mail", "ftp", "dev", "staging", "test",
        "demo", "app", "portal", "dashboard", "billing", "support",
        "help", "docs", "status", "cdn", "media", "assets", "static",
        "premium-demo", "localhost"
    };

    [GeneratedRegex(@"^[a-z0-9]([a-z0-9\-]{0,61}[a-z0-9])?$")]
    internal static partial Regex SubdomainPattern();

    internal static bool IsValidSubdomain(string subdomain)
        => !string.IsNullOrWhiteSpace(subdomain)
           && subdomain.Length >= 3
           && subdomain.Length <= 63
           && SubdomainPattern().IsMatch(subdomain);

    internal static bool IsReservedSubdomain(string subdomain)
        => ReservedSubdomains.Contains(subdomain);
}
