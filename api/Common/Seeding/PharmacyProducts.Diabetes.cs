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
        new("NVMX-INJ-5", "NovoMix 30 FlexPen", "Diabetes", "Biphasic insulin for type 1 and type 2 diabetes", 49.99m, "Insulin aspart biphasic", "NovoMix", "9300607130015", "S4", "5 x 3ml pens", "Insulin aspart 30% soluble, 70% protaminated", "Prescription only. Inject before meals. Store in refrigerator."),
        new("FRSTL-STRPS-50", "FreeStyle Optium Blood Glucose Test Strips", "Diabetes", "Blood glucose test strips for FreeStyle meters", 29.99m, null, "FreeStyle", "9300607131015", "Unscheduled", "50 strips", null, "Use with FreeStyle Optium meter only."),
        new("FRSTL-LIBRE-1", "FreeStyle Libre 2 Sensor", "Diabetes", "Flash glucose monitoring sensor", 89.99m, null, "FreeStyle", "9300607132015", "Unscheduled", "1 sensor", null, "Wear for up to 14 days. No fingerpricks needed."),
        new("HMLG-INJ-5", "Humalog KwikPen", "Diabetes", "Rapid-acting insulin pen", 44.99m, "Insulin lispro", "Humalog", "9300607133015", "S4", "5 x 3ml pens", "Insulin lispro 100 units/ml", "Prescription only. Inject within 15 minutes of meals."),
        new("LVRM-INJ-5", "Levemir FlexPen", "Diabetes", "Long-acting insulin pen for basal coverage", 52.99m, "Insulin detemir", "Levemir", "9300607134015", "S4", "5 x 3ml pens", "Insulin detemir 100 units/ml", "Prescription only. Once or twice daily. Store in refrigerator."),
        new("TRSBA-INJ-5", "Tresiba FlexTouch Pen", "Diabetes", "Ultra-long-acting insulin for flexible dosing", 59.99m, "Insulin degludec", "Tresiba", "9300607135015", "S4", "5 x 3ml pens", "Insulin degludec 100 units/ml", "Prescription only. Once daily at any time. Over 42-hour duration."),
        new("GLMT-LANCETS-200", "Accu-Chek FastClix Lancets", "Diabetes", "Lancets for blood glucose testing", 16.99m, null, "Accu-Chek", "9300607136015", "Unscheduled", "204 lancets", null, "Use with Accu-Chek FastClix lancing device. Single use."),
        new("FRXG-10-28", "Forxiga 10mg Tablets", "Diabetes", "SGLT2 inhibitor for type 2 diabetes", 49.99m, "Dapagliflozin", "Forxiga", "9300607137015", "S4", "28 tablets", "Dapagliflozin 10mg", "Prescription only. May increase urinary tract infections."),
        new("SHRPS-BIN-1", "BD Sharps Container 1.4L", "Diabetes", "Sharps disposal container for needles and lancets", 6.99m, null, "BD", "9300607138015", "Unscheduled", "1 container", null, "When full, seal and return to pharmacy for disposal."),
        new("GLCGN-INJ-1", "GlucaGen HypoKit", "Diabetes", "Emergency glucagon injection for severe hypoglycaemia", 54.99m, "Glucagon", "GlucaGen", "9300607139015", "S4", "1 kit", "Glucagon 1mg", "Prescription only. For emergency use when patient cannot swallow."),
        new("DBEX-850-60", "Diabex 850mg Tablets", "Diabetes", "Mid-strength metformin for type 2 diabetes", 14.99m, "Metformin", "Diabex", "9300607910032", "S4", "60 tablets", "Metformin hydrochloride 850mg", "Prescription only. Take with meals."),
        new("GLMT-STRPS-C-50", "CareSens N Test Strips", "Diabetes", "Blood glucose test strips for CareSens meters", 19.99m, null, "CareSens", "9300699000021", "Unscheduled", "50 strips", null, "Use with CareSens N meter only."),
    ];
}
