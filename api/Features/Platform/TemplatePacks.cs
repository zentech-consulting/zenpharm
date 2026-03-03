namespace Api.Features.Platform;

/// <summary>
/// Predefined product template packs for new tenant onboarding.
/// Each pack defines a subset of product categories to import from the master catalogue.
/// </summary>
internal static class TemplatePacks
{
    internal sealed record TemplatePack(
        string Id,
        string Name,
        string Description,
        string[] Categories);

    private static readonly TemplatePack[] All =
    [
        new("community-essentials",
            "Community Pharmacy Essentials",
            "Complete catalogue with all product categories — ideal for full-service community pharmacies.",
            []),  // Empty = all categories (default)

        new("health-wellness",
            "Health & Wellness Focus",
            "Vitamins, supplements, natural health, sports, skin care, sun protection, and personal care.",
            [
                "Vitamins & Supplements", "Supplements", "Natural Health",
                "Sports Medicine", "Skin Care", "Sun Protection", "Personal Care"
            ]),

        new("quick-start",
            "Quick Start",
            "Essential categories to get started quickly — pain relief, cold & flu, first aid, vitamins, allergy, and digestive health.",
            [
                "Pain Relief", "Cold & Flu", "First Aid",
                "Vitamins & Supplements", "Allergy", "Digestive Health"
            ])
    ];

    internal static TemplatePack GetDefault() => All[0];

    internal static TemplatePack? GetById(string id)
        => All.FirstOrDefault(p => p.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

    internal static IReadOnlyList<TemplatePack> GetAll() => All;

    /// <summary>
    /// Returns true if the category is included in the specified pack.
    /// An empty Categories array means all categories are included.
    /// </summary>
    internal static bool IncludesCategory(TemplatePack pack, string category)
        => pack.Categories.Length == 0
           || pack.Categories.Any(c => c.Equals(category, StringComparison.OrdinalIgnoreCase));
}
