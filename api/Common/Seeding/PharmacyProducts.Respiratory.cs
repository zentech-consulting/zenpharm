namespace Api.Common.Seeding;

internal static partial class PharmacyMasterProductData
{
    internal static readonly SeedProduct[] Respiratory =
    [
        new("FLXNS-SPR-120", "Flixonase Nasal Spray", "Respiratory", "Corticosteroid nasal spray for hayfever", 21.99m, "Fluticasone", "Flixonase", "9300607710014", "S3", "120 sprays", "Fluticasone propionate 50mcg/spray", "For nasal use only. Not for children under 12. Schedule 3 — pharmacist only."),
        new("RHNCT-SPR-120", "Rhinocort Nasal Spray", "Respiratory", "Hayfever and nasal allergy relief", 19.99m, "Budesonide", "Rhinocort", "9300607720013", "Unscheduled", "120 sprays", "Budesonide 32mcg/spray", null),
        new("VNTLN-INH-200", "Ventolin Inhaler 100mcg", "Respiratory", "Reliever inhaler for asthma and bronchospasm", 8.99m, "Salbutamol", "Ventolin", "9300607730012", "S3", "200 doses", "Salbutamol 100mcg/dose", "Use only as directed. See doctor if symptoms worsen. Schedule 3 — pharmacist only."),
        new("SRTD-INH-120", "Seretide MDI 250/25", "Respiratory", "Combination preventer for asthma", 62.99m, "Fluticasone + Salmeterol", "Seretide", "9300607090015", "S4", "120 doses", "Fluticasone propionate 250mcg, Salmeterol 25mcg", "Prescription only. Rinse mouth after use."),
        new("SPLVA-INH-30", "Spiriva HandiHaler", "Respiratory", "Long-acting bronchodilator for COPD", 54.99m, "Tiotropium", "Spiriva", "9300607091015", "S4", "30 capsules", "Tiotropium 18mcg", "Prescription only. For inhalation with HandiHaler device only."),
        new("FVRS-INH-120", "Flixotide Inhaler 250mcg", "Respiratory", "Corticosteroid preventer for asthma", 44.99m, "Fluticasone", "Flixotide", "9300607092015", "S4", "120 doses", "Fluticasone propionate 250mcg/dose", "Prescription only. Rinse mouth after use."),
        new("SMBLCT-NEB-20", "Salbutamol Nebules 2.5mg", "Respiratory", "Nebuliser solution for acute asthma", 12.99m, "Salbutamol", null, "9300607093015", "S4", "20 x 2.5ml", "Salbutamol 2.5mg/2.5ml", "Prescription only. For nebuliser use."),
        new("SNGLR-10-28", "Singulair 10mg Tablets", "Respiratory", "Leukotriene inhibitor for asthma", 34.99m, "Montelukast", "Singulair", "9300607094015", "S4", "28 tablets", "Montelukast sodium 10mg", "Prescription only. Take in the evening."),
        new("SPCR-VLM-LRG", "Volumatic Spacer", "Respiratory", "Large volume spacer for MDI inhalers", 24.99m, null, "Volumatic", "9300607095015", "Unscheduled", "1 spacer", null, "Clean monthly with mild detergent. Replace every 12 months."),
        new("BRDL-INH-200", "Bricanyl Turbuhaler 500mcg", "Respiratory", "Reliever inhaler for asthma", 14.99m, "Terbutaline", "Bricanyl", "9300607096015", "S3", "200 doses", "Terbutaline sulfate 500mcg/dose", "Schedule 3 — pharmacist only."),
        new("PKFLW-METER-1", "Peak Flow Meter", "Respiratory", "Portable peak flow measurement device", 29.99m, null, null, "9300607097015", "Unscheduled", "1 meter", null, "For monitoring asthma. Record readings regularly."),
        new("SLNWSH-SPR-100", "Flo Saline Nasal Spray", "Respiratory", "Isotonic saline nasal wash", 9.99m, null, "Flo", "9300607098015", "Unscheduled", "120ml", "Sodium chloride 0.9%", "Non-medicated. Safe for daily use."),
    ];
}
