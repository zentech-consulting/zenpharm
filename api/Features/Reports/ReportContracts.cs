namespace Api.Features.Reports;

public sealed record DashboardSummary(
    int TotalClients,
    int TotalBookings,
    int TotalEmployees,
    decimal Revenue,
    int TotalProducts,
    int LowStockCount,
    int ExpiringCount,
    IReadOnlyList<DailyStat> DailyStats);

public sealed record DailyStat(
    DateOnly Date,
    int BookingCount,
    decimal Revenue);

// --- Advanced Reports ---

public sealed record TopSellingProductDto(
    Guid ProductId,
    string ProductName,
    string? Brand,
    string Category,
    int TotalSold,
    decimal TotalRevenue);

public sealed record TopSellingProductsReport(
    IReadOnlyList<TopSellingProductDto> Items,
    int TotalStockOutMovements);

public sealed record RevenueByCategoryDto(
    string Category,
    int BookingCount,
    decimal Revenue);

public sealed record RevenueByCategoryReport(
    IReadOnlyList<RevenueByCategoryDto> Items,
    decimal TotalRevenue);

public sealed record ExpiryWasteDto(
    Guid ProductId,
    string ProductName,
    string? Brand,
    int ExpiredQuantity,
    decimal EstimatedWasteValue);

public sealed record ExpiryWasteReport(
    IReadOnlyList<ExpiryWasteDto> Items,
    int TotalExpiredMovements,
    decimal TotalWasteValue);

public sealed record EmployeeUtilisationDto(
    Guid EmployeeId,
    string EmployeeName,
    string Role,
    int TotalBookings,
    int CompletedBookings,
    decimal Revenue);

public sealed record EmployeeUtilisationReport(
    IReadOnlyList<EmployeeUtilisationDto> Items,
    int TotalBookings);
