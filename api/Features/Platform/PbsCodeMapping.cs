namespace Api.Features.Platform;

/// <summary>
/// Static mapping of common PBS item codes by active ingredient.
/// PBS = Pharmaceutical Benefits Scheme (Australian Government subsidy programme).
/// Format: ActiveIngredient (lowercase, normalised) → PBS item code.
/// </summary>
internal static class PbsCodeMapping
{
    internal static readonly IReadOnlyDictionary<string, string> ByActiveIngredient =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // Pain Relief / Anti-inflammatory
            ["paracetamol"] = "2622B",
            ["ibuprofen"] = "3146E",
            ["diclofenac"] = "1164G",
            ["naproxen"] = "2011Y",
            ["celecoxib"] = "8419G",
            ["tramadol"] = "8308Y",
            ["oxycodone"] = "8303T",
            ["pregabalin"] = "9128D",
            ["gabapentin"] = "8209R",
            ["codeine"] = "2132B",
            ["paracetamol + codeine"] = "2133C",

            // Cardiovascular
            ["atorvastatin"] = "8575B",
            ["rosuvastatin"] = "9063B",
            ["simvastatin"] = "8231M",
            ["amlodipine"] = "8075B",
            ["perindopril"] = "8340M",
            ["ramipril"] = "8234Q",
            ["metoprolol"] = "1840B",
            ["bisoprolol"] = "8809B",
            ["irbesartan"] = "8433W",
            ["candesartan"] = "8601D",
            ["warfarin"] = "3103L",
            ["apixaban"] = "10393E",
            ["rivaroxaban"] = "10089M",
            ["clopidogrel"] = "8546Y",

            // Diabetes
            ["metformin"] = "2164J",
            ["gliclazide"] = "1508P",
            ["insulin glargine"] = "9168T",
            ["insulin aspart"] = "8720M",
            ["empagliflozin"] = "10613B",
            ["sitagliptin"] = "9319F",
            ["dapagliflozin"] = "10413B",

            // Respiratory
            ["salbutamol"] = "2464J",
            ["fluticasone"] = "8447M",
            ["budesonide"] = "8050P",
            ["tiotropium"] = "8838H",
            ["fluticasone + salmeterol"] = "8597C",
            ["montelukast"] = "8490T",

            // Mental Health
            ["sertraline"] = "8826T",
            ["escitalopram"] = "8898D",
            ["venlafaxine"] = "8217X",
            ["fluoxetine"] = "1455M",
            ["mirtazapine"] = "8558M",
            ["quetiapine"] = "8616V",
            ["olanzapine"] = "8518X",
            ["diazepam"] = "1153T",
            ["temazepam"] = "2652G",
            ["alprazolam"] = "8001E",

            // Antibiotics
            ["amoxicillin"] = "1003K",
            ["amoxicillin + clavulanic acid"] = "8035Y",
            ["cefalexin"] = "1083C",
            ["azithromycin"] = "8048N",
            ["doxycycline"] = "1214X",
            ["ciprofloxacin"] = "8037B",
            ["trimethoprim"] = "2668Y",
            ["metronidazole"] = "1858W",
            ["flucloxacillin"] = "1437R",

            // Hormones / Thyroid
            ["levothyroxine"] = "2773G",
            ["oestradiol"] = "1282D",
            ["medroxyprogesterone"] = "1791L",
            ["testosterone"] = "8847J",

            // Gastrointestinal
            ["esomeprazole"] = "8879R",
            ["pantoprazole"] = "8338K",
            ["omeprazole"] = "8205L",
            ["lansoprazole"] = "8200F",

            // Eye Care
            ["timolol"] = "2662Q",
            ["latanoprost"] = "8506N",
            ["brimonidine"] = "8586P",

            // Dermatology
            ["betamethasone"] = "1040F",
            ["hydrocortisone"] = "1564L",
            ["mometasone"] = "8305V",
            ["acyclovir"] = "8000D",
            ["mupirocin"] = "8036Y",

            // Other
            ["allopurinol"] = "1002J",
            ["prednisone"] = "2305G",
            ["prednisolone"] = "2290L",
            ["folic acid"] = "1433L",
            ["alendronate"] = "8010Q",
            ["colchicine"] = "1129R",
            ["tamsulosin"] = "8890F",
            ["finasteride"] = "8226E",
            ["sildenafil"] = "8834B",
        };

    /// <summary>
    /// Attempts to match an active ingredient string to a PBS code.
    /// Handles partial matching (e.g. "Paracetamol 500mg" → matches "paracetamol").
    /// </summary>
    internal static string? FindPbsCode(string? activeIngredients)
    {
        if (string.IsNullOrWhiteSpace(activeIngredients))
            return null;

        var normalised = activeIngredients.Trim().ToLowerInvariant();

        // Exact match first
        if (ByActiveIngredient.TryGetValue(normalised, out var code))
            return code;

        // Try matching the first ingredient (before comma or bracket)
        var firstIngredient = normalised
            .Split([',', '(', '+'], StringSplitOptions.TrimEntries)
            .FirstOrDefault();

        if (firstIngredient is not null)
        {
            // Strip dosage suffixes like "500mg", "200mg/5ml"
            var ingredientName = System.Text.RegularExpressions.Regex
                .Replace(firstIngredient, @"\s*\d+[\d./]*\s*(mg|mcg|g|ml|iu|units?).*$", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase)
                .Trim();

            if (!string.IsNullOrWhiteSpace(ingredientName) && ByActiveIngredient.TryGetValue(ingredientName, out code))
                return code;
        }

        return null;
    }
}
