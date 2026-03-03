namespace Api.Features.Platform;

internal interface IPbsSyncManager
{
    Task<PbsSyncResult> SyncAsync(CancellationToken ct = default);
    Task<PbsSummary> GetSummaryAsync(CancellationToken ct = default);
}

internal sealed record PbsSyncResult(int TotalProducts, int Matched, int Updated);
internal sealed record PbsSummary(int TotalProducts, int WithPbsCode, int WithoutPbsCode);
