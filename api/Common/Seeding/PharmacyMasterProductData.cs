namespace Api.Common.Seeding;

internal static partial class PharmacyMasterProductData
{
    internal sealed record SeedProduct(
        string Sku,
        string Name,
        string Category,
        string? Description,
        decimal UnitPrice,
        string? GenericName,
        string? Brand,
        string? Barcode,
        string ScheduleClass,
        string? PackSize,
        string? ActiveIngredients,
        string? Warnings);

    private static SeedProduct[]? _all;

    internal static SeedProduct[] All => _all ??= BuildAll();

    private static SeedProduct[] BuildAll()
    {
        var groups = new List<SeedProduct>();
        groups.AddRange(PainRelief ?? []);
        groups.AddRange(Allergy ?? []);
        groups.AddRange(DigestiveHealth ?? []);
        groups.AddRange(ColdAndFlu ?? []);
        groups.AddRange(EyeCare ?? []);
        groups.AddRange(Respiratory ?? []);
        groups.AddRange(Antibiotics ?? []);
        groups.AddRange(Diabetes ?? []);
        groups.AddRange(Cardiovascular ?? []);
        groups.AddRange(SkinCare ?? []);
        groups.AddRange(FirstAid ?? []);
        groups.AddRange(Vitamins ?? []);
        groups.AddRange(WomensHealth ?? []);
        groups.AddRange(MensHealth ?? []);
        groups.AddRange(OralCare ?? []);
        groups.AddRange(PersonalCare ?? []);
        groups.AddRange(BabyCare ?? []);
        groups.AddRange(MentalHealth ?? []);
        groups.AddRange(Hormones ?? []);
        groups.AddRange(OtherPrescription ?? []);
        groups.AddRange(WoundCare ?? []);
        groups.AddRange(SportsMedicine ?? []);
        return groups.ToArray();
    }
}
