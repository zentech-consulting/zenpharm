namespace Api.Common.Seeding;

internal static partial class PharmacyMasterProductData
{
    internal static readonly SeedProduct[] Diabetes =
    [
        new("DBEX-500-60", "Diabex 500mg Tablets", "Diabetes", "First-line treatment for type 2 diabetes", 12.99m, "Metformin", "Diabex", "9300607910018", "S4", "60 tablets", "Metformin hydrochloride 500mg", "Prescription only. Take with meals. Monitor blood glucose."),
        new("DBEX-1000-60", "Diabex XR 1000mg Tablets", "Diabetes", "Extended-release metformin for type 2 diabetes", 16.99m, "Metformin", "Diabex", "9300607910025", "S4", "60 tablets", "Metformin hydrochloride 1000mg (extended release)", "Prescription only. Swallow whole — do not crush or chew."),
        new("JDNS-100-28", "Jardiance 10mg Tablets", "Diabetes", "SGLT2 inhibitor for type 2 diabetes", 49.99m, "Empagliflozin", "Jardiance", "9300607120015", "S4", "28 tablets", "Empagliflozin 10mg", "Prescription only. May increase urinary tract infections."),
        new("GLCMT-500-60", "Glucophage 500mg Tablets", "Diabetes", "Metformin for type 2 diabetes", 12.99m, "Metformin", "Glucophage", "9300607121015", "S4", "100 tablets", "Metformin hydrochloride 500mg", "Prescription only. Take with meals."),
        new("NVLG-INJ-5", "NovoRapid FlexPen", "Diabetes", "Rapid-acting insulin pen", 42.99m, "Insulin aspart", "NovoRapid", "9300607122015", "S4", "5 x 3ml pens", "Insulin aspart 100 units/ml", "Prescription only. Store in refrigerator. Inject subcutaneously."),
        new("LNTS-INJ-5", "Lantus SoloStar", "Diabetes", "Long-acting insulin pen", 54.99m, "Insulin glargine", "Lantus", "9300607123015", "S4", "5 x 3ml pens", "Insulin glargine 100 units/ml", "Prescription only. Store in refrigerator. Once-daily injection."),
        new("GLMT-STRPS-50", "Accu-Chek Performa Test Strips", "Diabetes", "Blood glucose test strips", 29.99m, null, "Accu-Chek", "9300607124015", "Unscheduled", "50 strips", null, "Use with Accu-Chek Performa meter only."),
        new("GLMT-METER-1", "Accu-Chek Guide Meter Kit", "Diabetes", "Blood glucose monitoring meter", 29.99m, null, "Accu-Chek", "9300607125015", "Unscheduled", "1 kit", null, "Includes meter, 10 test strips, lancing device."),
        new("GLCLZ-80-60", "Gliclazide 80mg Tablets", "Diabetes", "Sulfonylurea for type 2 diabetes", 14.99m, "Gliclazide", null, "9300607126015", "S4", "60 tablets", "Gliclazide 80mg", "Prescription only. Risk of hypoglycaemia."),
        new("SGPTN-100-28", "Januvia 100mg Tablets", "Diabetes", "DPP-4 inhibitor for type 2 diabetes", 49.99m, "Sitagliptin", "Januvia", "9300607127015", "S4", "28 tablets", "Sitagliptin 100mg", "Prescription only."),
        new("LNCET-BOX-100", "BD Ultra-Fine Pen Needles 4mm", "Diabetes", "Pen needles for insulin injectors", 19.99m, null, "BD", "9300607128015", "Unscheduled", "100 needles", null, "Single use only. Dispose in sharps container."),
        new("DPGLF-1-28", "Ozempic 1mg Pen", "Diabetes", "GLP-1 receptor agonist for type 2 diabetes", 89.99m, "Semaglutide", "Ozempic", "9300607129015", "S4", "1 pre-filled pen", "Semaglutide 1mg/dose", "Prescription only. Once-weekly injection."),
    ];
}
