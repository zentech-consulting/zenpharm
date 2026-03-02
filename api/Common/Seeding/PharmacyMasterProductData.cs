namespace Api.Common.Seeding;

internal static class PharmacyMasterProductData
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

    internal static readonly SeedProduct[] All =
    [
        // --- Pain Relief (Unscheduled / S2) ---
        new("PNDL-500-20", "Panadol 500mg Tablets", "Pain Relief",
            "For temporary relief of pain and fever",
            6.99m, "Paracetamol", "Panadol", "9300607010015", "Unscheduled",
            "20 tablets", "Paracetamol 500mg",
            "Do not exceed 8 tablets in 24 hours. Do not use with other paracetamol products."),

        new("NRFN-200-24", "Nurofen 200mg Capsules", "Pain Relief",
            "Anti-inflammatory pain relief",
            9.99m, "Ibuprofen", "Nurofen", "9300631030119", "Unscheduled",
            "24 capsules", "Ibuprofen 200mg",
            "Do not use if you have stomach ulcers. Take with food."),

        new("PNDNE-500-20", "Panadeine 500/8mg Tablets", "Pain Relief",
            "Paracetamol with codeine for moderate pain",
            12.49m, "Paracetamol + Codeine", "Panadeine", "9300607010039", "S3",
            "20 tablets", "Paracetamol 500mg, Codeine phosphate 8mg",
            "May cause drowsiness. Do not drive or operate machinery. Schedule 3 — pharmacist only."),

        new("VLTRN-25-20", "Voltaren Rapid 25mg Tablets", "Pain Relief",
            "Fast-acting anti-inflammatory",
            11.99m, "Diclofenac", "Voltaren", "9300607251012", "S2",
            "20 tablets", "Diclofenac potassium 25mg",
            "Do not use for more than 5 days without medical advice."),

        // --- Allergy (Unscheduled) ---
        new("ZRTC-10-30", "Zyrtec 10mg Tablets", "Allergy",
            "Non-drowsy antihistamine for hayfever and allergies",
            14.99m, "Cetirizine", "Zyrtec", "9310160815019", "Unscheduled",
            "30 tablets", "Cetirizine hydrochloride 10mg",
            "May cause drowsiness in some people."),

        new("CLRT-10-30", "Claratyne 10mg Tablets", "Allergy",
            "24-hour non-drowsy hayfever relief",
            16.99m, "Loratadine", "Claratyne", "9310160814012", "Unscheduled",
            "30 tablets", "Loratadine 10mg",
            null),

        new("TLFS-180-30", "Telfast 180mg Tablets", "Allergy",
            "Fast-acting hayfever relief",
            19.99m, "Fexofenadine", "Telfast", "9310160816019", "Unscheduled",
            "30 tablets", "Fexofenadine hydrochloride 180mg",
            null),

        // --- Digestive Health (Unscheduled) ---
        new("LOSC-20-14", "Losec 20mg Capsules", "Digestive Health",
            "Relief of heartburn and acid reflux",
            18.99m, "Omeprazole", "Losec", "9300607350012", "Unscheduled",
            "14 capsules", "Omeprazole 20mg",
            "Do not use for more than 14 days without medical advice."),

        new("IMDM-2-12", "Imodium 2mg Capsules", "Digestive Health",
            "Relief of acute diarrhoea",
            10.99m, "Loperamide", "Imodium", "9300607320015", "Unscheduled",
            "12 capsules", "Loperamide hydrochloride 2mg",
            "Do not use for children under 12 without medical advice."),

        new("GVSN-LIQ-300", "Gaviscon Original Liquid", "Digestive Health",
            "Fast relief of heartburn and indigestion",
            12.99m, "Sodium alginate", "Gaviscon", "9300607410013", "Unscheduled",
            "300ml", "Sodium alginate, Sodium bicarbonate, Calcium carbonate",
            null),

        // --- Cold & Flu (Unscheduled / S3) ---
        new("SDFD-PE-24", "Sudafed PE Sinus + Pain", "Cold & Flu",
            "Relief of sinus congestion and headache",
            11.99m, "Phenylephrine + Paracetamol", "Sudafed", "9300607510010", "S3",
            "24 tablets", "Phenylephrine 5mg, Paracetamol 500mg",
            "Do not use with other decongestants. Schedule 3 — pharmacist only."),

        new("CDRL-DAY-24", "Codral Day & Night Tablets", "Cold & Flu",
            "Multi-symptom cold and flu relief",
            13.99m, "Paracetamol + Phenylephrine", "Codral", "9300607520019", "Unscheduled",
            "24 tablets", "Paracetamol 500mg, Phenylephrine 5mg",
            "Day tablets may cause drowsiness. Check interactions."),

        // --- Eye Care (S3) ---
        new("CLRSG-EYE-5", "Chlorsig Eye Drops 0.5%", "Eye Care",
            "Antibiotic eye drops for bacterial conjunctivitis",
            14.99m, "Chloramphenicol", "Chlorsig", "9300607610017", "S3",
            "5ml", "Chloramphenicol 5mg/ml",
            "For external eye use only. Discard 28 days after opening. Schedule 3 — pharmacist only."),

        // --- Nasal / Respiratory (S3 / Unscheduled) ---
        new("FLXNS-SPR-120", "Flixonase Nasal Spray", "Respiratory",
            "Corticosteroid nasal spray for hayfever",
            21.99m, "Fluticasone", "Flixonase", "9300607710014", "S3",
            "120 sprays", "Fluticasone propionate 50mcg/spray",
            "For nasal use only. Not for children under 12. Schedule 3 — pharmacist only."),

        new("RHNCT-SPR-120", "Rhinocort Nasal Spray", "Respiratory",
            "Hayfever and nasal allergy relief",
            19.99m, "Budesonide", "Rhinocort", "9300607720013", "Unscheduled",
            "120 sprays", "Budesonide 32mcg/spray",
            null),

        new("VNTLN-INH-200", "Ventolin Inhaler 100mcg", "Respiratory",
            "Reliever inhaler for asthma and bronchospasm",
            8.99m, "Salbutamol", "Ventolin", "9300607730012", "S3",
            "200 doses", "Salbutamol 100mcg/dose",
            "Use only as directed. See doctor if symptoms worsen. Schedule 3 — pharmacist only."),

        // --- Antibiotics (S4) ---
        new("AMXL-500-20", "Amoxil 500mg Capsules", "Antibiotics",
            "Broad-spectrum antibiotic for bacterial infections",
            15.99m, "Amoxicillin", "Amoxil", "9300607810011", "S4",
            "20 capsules", "Amoxicillin 500mg",
            "Prescription only. Complete the full course. Report allergic reactions immediately."),

        new("KFLX-500-20", "Keflex 500mg Capsules", "Antibiotics",
            "Cephalosporin antibiotic for infections",
            18.99m, "Cefalexin", "Keflex", "9300607820010", "S4",
            "20 capsules", "Cefalexin 500mg",
            "Prescription only. Complete the full course."),

        new("DRYX-100-30", "Doryx 100mg Tablets", "Antibiotics",
            "Antibiotic for acne and infections",
            24.99m, "Doxycycline", "Doryx", "9300607830019", "S4",
            "30 tablets", "Doxycycline 100mg",
            "Prescription only. Avoid sun exposure. Take upright with full glass of water."),

        // --- Diabetes (S4) ---
        new("DBEX-500-60", "Diabex 500mg Tablets", "Diabetes",
            "First-line treatment for type 2 diabetes",
            12.99m, "Metformin", "Diabex", "9300607910018", "S4",
            "60 tablets", "Metformin hydrochloride 500mg",
            "Prescription only. Take with meals. Monitor blood glucose."),

        new("DBEX-1000-60", "Diabex XR 1000mg Tablets", "Diabetes",
            "Extended-release metformin for type 2 diabetes",
            16.99m, "Metformin", "Diabex", "9300607910025", "S4",
            "60 tablets", "Metformin hydrochloride 1000mg (extended release)",
            "Prescription only. Swallow whole — do not crush or chew."),

        // --- Cardiovascular (S4) ---
        new("LPTR-20-30", "Lipitor 20mg Tablets", "Cardiovascular",
            "Statin for cholesterol management",
            22.99m, "Atorvastatin", "Lipitor", "9300607920017", "S4",
            "30 tablets", "Atorvastatin 20mg",
            "Prescription only. Report unexplained muscle pain."),

        new("NRVC-5-30", "Norvasc 5mg Tablets", "Cardiovascular",
            "Calcium channel blocker for high blood pressure",
            14.99m, "Amlodipine", "Norvasc", "9300607930016", "S4",
            "30 tablets", "Amlodipine 5mg",
            "Prescription only. May cause ankle swelling."),

        // --- Skin Care (Unscheduled) ---
        new("SGMCRT-1-30", "Sigmacort 1% Cream", "Skin Care",
            "Mild topical corticosteroid for eczema and dermatitis",
            9.99m, "Hydrocortisone", "Sigmacort", "9300608010014", "Unscheduled",
            "30g", "Hydrocortisone 1%",
            "Do not use on face for more than 5 days. Not for infected skin."),

        new("CNSTN-CRM-20", "Canesten Cream 1%", "Skin Care",
            "Antifungal cream for tinea and fungal infections",
            11.99m, "Clotrimazole", "Canesten", "9300608020013", "Unscheduled",
            "20g", "Clotrimazole 1%",
            null),

        new("BNZC-GEL-50", "Benzac AC 5% Gel", "Skin Care",
            "Acne treatment gel with antibacterial action",
            14.99m, "Benzoyl peroxide", "Benzac", "9300608030012", "Unscheduled",
            "50g", "Benzoyl peroxide 5%",
            "May cause skin dryness and peeling. Avoid contact with eyes and lips."),

        // --- First Aid (Unscheduled) ---
        new("BTRDN-CRM-50", "Betadine Antiseptic Cream", "First Aid",
            "Broad-spectrum antiseptic for cuts and abrasions",
            8.99m, "Povidone-iodine", "Betadine", "9310160820013", "Unscheduled",
            "50g", "Povidone-iodine 10%",
            "For external use only. Do not use if allergic to iodine."),

        // --- Vitamins (Unscheduled) ---
        new("BRCCA-VD-60", "Bioceuticals Vitamin D3 1000IU", "Vitamins",
            "Supports bone health and immune function",
            19.99m, null, "BioCeuticals", "9310160830012", "Unscheduled",
            "60 capsules", "Cholecalciferol (Vitamin D3) 1000IU",
            "Always read the label. Supplements should not replace a balanced diet."),

        new("BLKMRS-FSH-200", "Blackmores Fish Oil 1000mg", "Vitamins",
            "Omega-3 fatty acids for heart and brain health",
            24.99m, null, "Blackmores", "9300807010011", "Unscheduled",
            "200 capsules", "Fish oil 1000mg (EPA 180mg, DHA 120mg)",
            null),

        // --- Women's Health (S4) ---
        new("CNST-ED-28", "Levlen ED Tablets", "Women's Health",
            "Combined oral contraceptive pill",
            12.99m, "Levonorgestrel + Ethinylestradiol", "Levlen", "9300608040011", "S4",
            "28 tablets", "Levonorgestrel 150mcg, Ethinylestradiol 30mcg",
            "Prescription only. Does not protect against STIs.")
    ];
}
