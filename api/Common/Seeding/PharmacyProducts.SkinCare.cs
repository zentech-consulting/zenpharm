namespace Api.Common.Seeding;

internal static partial class PharmacyMasterProductData
{
    internal static readonly SeedProduct[] SkinCare =
    [
        new("SGMCRT-1-30", "Sigmacort 1% Cream", "Skin Care", "Mild topical corticosteroid for eczema and dermatitis", 9.99m, "Hydrocortisone", "Sigmacort", "9300608010014", "Unscheduled", "30g", "Hydrocortisone 1%", "Do not use on face for more than 5 days. Not for infected skin."),
        new("CNSTN-CRM-20", "Canesten Cream 1%", "Skin Care", "Antifungal cream for tinea and fungal infections", 11.99m, "Clotrimazole", "Canesten", "9300608020013", "Unscheduled", "20g", "Clotrimazole 1%", null),
        new("BNZC-GEL-50", "Benzac AC 5% Gel", "Skin Care", "Acne treatment gel with antibacterial action", 14.99m, "Benzoyl peroxide", "Benzac", "9300608030012", "Unscheduled", "50g", "Benzoyl peroxide 5%", "May cause skin dryness and peeling. Avoid contact with eyes and lips."),
        new("CTML-CRM-100", "Cetaphil Moisturising Cream", "Skin Care", "Gentle moisturiser for sensitive skin", 16.99m, null, "Cetaphil", "9300607160015", "Unscheduled", "100g", null, "Suitable for sensitive and eczema-prone skin."),
        new("CTML-CLNSR-500", "Cetaphil Gentle Skin Cleanser", "Skin Care", "Soap-free cleanser for all skin types", 14.99m, null, "Cetaphil", "9300607161015", "Unscheduled", "500ml", null, "Fragrance free. Suitable for sensitive skin."),
        new("LCRPT-BSM-200", "La Roche-Posay Lipikar Baume AP+M", "Skin Care", "Intensive moisturiser for very dry skin", 29.99m, null, "La Roche-Posay", "9300607162015", "Unscheduled", "200ml", "Shea butter, Niacinamide", "Suitable for eczema-prone skin."),
        new("SNSCN-SPF50-200", "Cancer Council SPF50+ Sunscreen", "Skin Care", "Broad spectrum sunscreen", 14.99m, null, "Cancer Council", "9300607163015", "Unscheduled", "200ml", null, "Apply 20 minutes before sun exposure. Reapply every 2 hours."),
        new("BNZC-GEL-10", "Benzac AC 10% Gel", "Skin Care", "Strong acne treatment gel", 16.99m, "Benzoyl peroxide", "Benzac", "9300608030029", "Unscheduled", "50g", "Benzoyl peroxide 10%", "May cause significant dryness. Start with 5% first."),
        new("EPIDUO-GEL-30", "Epiduo Gel", "Skin Care", "Combination acne treatment", 39.99m, "Adapalene + Benzoyl peroxide", "Epiduo", "9300607164015", "S4", "30g", "Adapalene 0.1%, Benzoyl peroxide 2.5%", "Prescription only. Avoid sun exposure. Apply at night."),
        new("DFPRN-CRM-30", "Diprosone Cream 0.05%", "Skin Care", "Potent topical corticosteroid", 14.99m, "Betamethasone", "Diprosone", "9300607165015", "S4", "30g", "Betamethasone dipropionate 0.05%", "Prescription only. Do not use on face."),
        new("LCRSC-CRM-40", "Locoid Cream 0.1%", "Skin Care", "Moderate topical corticosteroid", 12.99m, "Hydrocortisone butyrate", "Locoid", "9300607166015", "S4", "30g", "Hydrocortisone butyrate 0.1%", "Prescription only."),
        new("QVCRM-500", "QV Intensive Moisturising Cream", "Skin Care", "Intensive moisturiser for dry skin", 18.99m, null, "QV", "9300607167015", "Unscheduled", "500g", null, "Fragrance free. Soap free."),
        new("QVWSH-500", "QV Gentle Wash", "Skin Care", "Soap-free body wash", 14.99m, null, "QV", "9300607168015", "Unscheduled", "500ml", null, "Fragrance free. pH balanced."),
        new("ECZML-CRM-100", "Epaderm Ointment", "Skin Care", "Emollient for eczema and dry skin", 12.99m, null, "Epaderm", "9300607169015", "Unscheduled", "125g", "Yellow soft paraffin, Liquid paraffin, Emulsifying wax", "Can be used as soap substitute."),
        new("PLRTM-LTN-200", "Palmer's Cocoa Butter Formula", "Skin Care", "Moisturising lotion for stretch marks", 11.99m, null, "Palmer's", "9300607170015", "Unscheduled", "250ml", "Cocoa butter, Vitamin E", null),
        new("AVNE-CLNW-300", "Avene Gentle Cleansing Foam", "Skin Care", "Gentle foaming cleanser", 24.99m, null, "Avene", "9300607171015", "Unscheduled", "150ml", null, "Soap free. Suitable for sensitive skin."),
        new("SNSCN-FACE-75", "La Roche-Posay Anthelios SPF50+", "Skin Care", "Ultra-light facial sunscreen", 24.99m, null, "La Roche-Posay", "9300607172015", "Unscheduled", "50ml", null, "Non-greasy. Suitable under makeup."),
        new("LMSL-CRM-15", "Lamisil Cream 1%", "Skin Care", "Antifungal cream for athlete's foot", 14.99m, "Terbinafine", "Lamisil", "9300607173015", "Unscheduled", "15g", "Terbinafine hydrochloride 1%", "Apply once or twice daily for 1 week."),
        new("DKTRS-CRM-30", "Daktarin Cream 2%", "Skin Care", "Antifungal cream for skin infections", 9.99m, "Miconazole", "Daktarin", "9300607174015", "Unscheduled", "30g", "Miconazole nitrate 2%", null),
        new("BPNTN-CRM-50", "Bepanthen Antiseptic Cream", "Skin Care", "Healing cream for minor wounds and nappy rash", 12.99m, "Dexpanthenol + Chlorhexidine", "Bepanthen", "9300607175015", "Unscheduled", "50g", "Dexpanthenol 5%, Chlorhexidine dihydrochloride 0.5%", null),
        new("ALVRGL-200", "Aloe Vera Gel Pure", "Skin Care", "Soothing gel for sunburn and minor burns", 9.99m, null, null, "9300607176015", "Unscheduled", "200ml", "Aloe barbadensis leaf juice 99%", "For external use only."),
        new("RSCS-OIL-125", "Rosehip Oil Certified Organic", "Skin Care", "Facial oil for scars and ageing", 19.99m, null, "Trilogy", "9300607177015", "Unscheduled", "45ml", "Rosa canina fruit oil", null),
        new("SCRF-SHMP-200", "Nizoral Anti-Dandruff Shampoo", "Skin Care", "Medicated shampoo for dandruff and seborrheic dermatitis", 16.99m, "Ketoconazole", "Nizoral", "9300607178015", "Unscheduled", "200ml", "Ketoconazole 2%", "Use twice weekly for treatment. Once weekly for prevention."),
        new("DGRSN-SHMP-200", "Selsun Gold Shampoo", "Skin Care", "Anti-dandruff shampoo", 12.99m, "Selenium sulfide", "Selsun", "9300607179015", "Unscheduled", "200ml", "Selenium sulfide 2.5%", "Leave on for 2-3 minutes before rinsing."),
        new("TRTND-CRM-20", "Retrieve Cream 0.05%", "Skin Care", "Retinoid for acne and photo-ageing", 34.99m, "Tretinoin", "Retrieve", "9300607180015", "S4", "20g", "Tretinoin 0.05%", "Prescription only. Apply at night. Use sunscreen during the day."),
    ];
}
