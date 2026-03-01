namespace Api.Features.Platform;

public interface IProvisioningPipeline
{
    Task<ProvisionResult> ProvisionAsync(ProvisionRequest request, CancellationToken ct = default);
}

public sealed record ProvisionRequest(
    string TenantName,
    string Subdomain,
    string AdminEmail,
    string? Plan);

public sealed record ProvisionResult(
    bool Success,
    string? TenantId,
    string? Message);
