namespace Api.Common.Seeding;

internal static partial class PharmacyMasterProductData
{
    internal static readonly SeedProduct[] ColdAndFlu =
    [
        new("SDFD-PE-24", "Sudafed PE Sinus + Pain", "Cold & Flu", "Relief of sinus congestion and headache", 11.99m, "Phenylephrine + Paracetamol", "Sudafed", "9300607510010", "S3", "24 tablets", "Phenylephrine 5mg, Paracetamol 500mg", "Do not use with other decongestants. Schedule 3 — pharmacist only."),
        new("CDRL-DAY-24", "Codral Day & Night Tablets", "Cold & Flu", "Multi-symptom cold and flu relief", 13.99m, "Paracetamol + Phenylephrine", "Codral", "9300607520019", "Unscheduled", "24 tablets", "Paracetamol 500mg, Phenylephrine 5mg", "Day tablets may cause drowsiness. Check interactions."),
        new("STRPS-LNG-16", "Strepsils Sore Throat Lozenges", "Cold & Flu", "Antibacterial lozenges for sore throat", 6.99m, null, "Strepsils", "9300607060015", "Unscheduled", "16 lozenges", "2,4-Dichlorobenzyl alcohol, Amylmetacresol", null),
        new("RBTSS-DM-200", "Robitussin DM Cough Syrup", "Cold & Flu", "Cough suppressant for dry cough", 11.99m, "Dextromethorphan", "Robitussin", "9300607061015", "Unscheduled", "200ml", "Dextromethorphan 15mg/10ml", "Do not use with MAO inhibitors."),
        new("RBTSS-EX-200", "Robitussin Chesty Cough", "Cold & Flu", "Expectorant for chesty cough", 11.99m, "Guaifenesin", "Robitussin", "9300607061022", "Unscheduled", "200ml", "Guaifenesin 100mg/5ml", null),
        new("DRST-NAS-15", "Drixine Nasal Spray", "Cold & Flu", "Decongestant nasal spray", 8.99m, "Oxymetazoline", "Drixine", "9300607062015", "Unscheduled", "15ml", "Oxymetazoline hydrochloride 0.05%", "Do not use for more than 3 days."),
        new("LMSPL-HNY-8", "Lemsip Max Cold & Flu", "Cold & Flu", "Hot drink for cold and flu symptoms", 9.99m, "Paracetamol + Phenylephrine", "Lemsip", "9300607063015", "Unscheduled", "10 sachets", "Paracetamol 1000mg, Phenylephrine 12.2mg", "Do not use with other paracetamol products."),
        new("CDRL-NGHT-24", "Codral Nightime Cold & Flu", "Cold & Flu", "Night-time cold and flu relief", 12.99m, "Paracetamol + Doxylamine + Phenylephrine", "Codral", "9300607520026", "S2", "24 tablets", "Paracetamol 500mg, Doxylamine 6.25mg, Phenylephrine 5mg", "Causes drowsiness. Do not drive."),
        new("BSTWN-LZNG-20", "Betadine Sore Throat Gargle", "Cold & Flu", "Antiseptic gargle for sore throat", 9.99m, "Povidone-iodine", "Betadine", "9300607064015", "Unscheduled", "120ml", "Povidone-iodine 0.5%", "Dilute before use. Do not swallow."),
        new("DFLD-PE-24", "Demazin Cold & Flu Day/Night", "Cold & Flu", "Combination cold and flu tablets", 12.99m, "Paracetamol + Phenylephrine + Chlorpheniramine", "Demazin", "9300607065015", "S2", "24 tablets", "Paracetamol 500mg, Phenylephrine 5mg, Chlorpheniramine 2mg", "Night tablets cause drowsiness."),
        new("BSCP-THRML-10", "Difflam Anti-inflammatory Throat Spray", "Cold & Flu", "Anti-inflammatory spray for sore throat", 12.99m, "Benzydamine", "Difflam", "9300607066015", "Unscheduled", "30ml", "Benzydamine hydrochloride 0.15%", "Spray into throat. Do not swallow."),
        new("STRPS-HNYL-16", "Strepsils Honey & Lemon Lozenges", "Cold & Flu", "Soothing lozenges for sore throat", 6.99m, null, "Strepsils", "9300607060022", "Unscheduled", "16 lozenges", "2,4-Dichlorobenzyl alcohol, Amylmetacresol", null),
        new("DURO-COUGH-200", "Duro-Tuss Dry Cough Liquid", "Cold & Flu", "Cough suppressant liquid", 10.99m, "Pholcodine", "Duro-Tuss", "9300607067015", "S2", "200ml", "Pholcodine 1mg/ml", "May cause drowsiness. Schedule 2."),
        new("RBTSS-NGHT-200", "Robitussin Night-Time Cough", "Cold & Flu", "Night-time cough suppressant", 12.99m, "Dextromethorphan + Doxylamine", "Robitussin", "9300607061039", "S2", "200ml", "Dextromethorphan 15mg/10ml, Doxylamine 6.25mg/10ml", "Causes drowsiness."),
        new("CDRL-FLU-20", "Codral Cold & Flu + Cough", "Cold & Flu", "Multi-symptom with cough suppressant", 14.99m, "Paracetamol + Phenylephrine + Dextromethorphan", "Codral", "9300607520033", "S2", "20 capsules", "Paracetamol 500mg, Phenylephrine 5mg, Dextromethorphan 15mg", null),
        new("SDFD-SNSS-12", "Sudafed Sinus 12 Hour Relief", "Cold & Flu", "Extended-release decongestant", 13.99m, "Pseudoephedrine", "Sudafed", "9300607510027", "S3", "10 tablets", "Pseudoephedrine sulfate 120mg", "Schedule 3 — pharmacist only. ID required."),
        new("VCKSVP-RUB-50", "Vicks VapoRub", "Cold & Flu", "Topical cough and cold relief", 8.99m, null, "Vicks", "9300607068015", "Unscheduled", "50g", "Camphor 5.25%, Menthol 2.82%, Eucalyptus oil 1.49%", "For external use only. Not for children under 2."),
        new("MLNX-1000-10", "Mucinex 1000mg Tablets", "Cold & Flu", "Sustained-release expectorant", 14.99m, "Guaifenesin", "Mucinex", "9300607069015", "Unscheduled", "20 tablets", "Guaifenesin 600mg (extended release)", "Swallow whole. Drink plenty of fluids."),
        new("SRHNG-LZNG-16", "Soothers Medicated Lozenges", "Cold & Flu", "Medicated throat lozenges", 4.99m, null, "Soothers", "9300607070015", "Unscheduled", "16 lozenges", "Menthol 1.5mg, Eucalyptus oil", null),
    ];
}
