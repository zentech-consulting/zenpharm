# AI Tool Design Guide — By Domain

How to design Claude API tools for different industries. Each section shows the tools, their schemas, and implementation patterns.

## Design Principles

1. **Tools are for data and side effects** — If Claude can answer from its own knowledge, don't make a tool
2. **3-8 tools per domain** — Too few = limited AI, too many = confused AI
3. **Verb_noun naming** — `search_products`, `create_booking`, `get_store_info`
4. **Descriptions are prompts** — Tell Claude WHEN and WHY to use the tool
5. **Return structured JSON** — Claude processes JSON better than free text
6. **Fail gracefully** — Return error messages the AI can relay to the user

## Tool Categories

### Query Tools (Read-only)
```csharp
// Pattern: Return data the AI needs to answer questions
name = "get_[entity]"        // Single item
name = "search_[entities]"   // Multiple items with filtering
name = "check_[status]"      // Boolean/status check

// Always include in schema:
// - Filters (optional): narrow results
// - max_results (optional): prevent data overload, default 5, max 10
```

### Action Tools (Side effects)
```csharp
// Pattern: Create/modify records, trigger processes
name = "create_[entity]"
name = "book_[entity]"
name = "add_to_[collection]"

// CRITICAL: Always validate input before executing
// - Required field checks
// - Format validation (email, phone, dates)
// - Fake data detection (for customer-facing tools)
// - Return confirmation with reference ID
```

### Analysis Tools (Computed)
```csharp
// Pattern: Process input and return structured insights
name = "analyze_[subject]"
name = "recommend_[entity]"

// Return structured analysis, not free text
// Let Claude interpret and present the analysis to the user
```

## Domain Templates

### Healthcare / Pharmacy
```
Tools:
├── get_available_time_slots   → Query appointment availability
├── create_pickup_order        → Book prescription pickup
├── create_delivery_enquiry    → Request medication delivery
├── create_immunisation_booking → Book vaccination
├── get_store_info             → Store hours, address, phone
├── search_products            → Health products by name/need
├── get_product_details        → Full product info + stock
├── check_product_availability → Stock at specific location
├── add_to_cart                → E-commerce cart
├── get_cart_summary           → Cart review
├── get_product_recommendations → Health-need based suggestions
└── search_knowledge_base      → FAQ, policies, services
```

**Key patterns:**
- Slot-based booking with availability check BEFORE creation
- Multi-location support (each store has different hours/stock)
- Health concern → search term mapping for recommendations
- Medical disclaimer requirement on product recommendations

### IT Consulting / Professional Services
```
Tools:
├── analyze_business_needs     → Structured needs analysis
├── get_case_studies           → Filter by industry/challenge
├── get_service_info           → Service details by type
├── book_consultation          → Create lead with contact info
└── search_knowledge_base      → Tech FAQ, methodologies
```

**Key patterns:**
- Industry classification for case study matching
- Challenge type → solution mapping
- Lead scoring based on company size, urgency
- Consultation booking creates Lead record for CRM

### E-commerce / Retail
```
Tools:
├── search_products            → By name, category, price range
├── get_product_details        → Description, specs, reviews
├── check_stock                → Availability by location/variant
├── compare_products           → Side-by-side comparison
├── add_to_cart                → Cart management
├── get_cart_summary           → Review before checkout
├── apply_discount             → Promo code validation
├── track_order                → Order status lookup
└── search_faq                 → Returns, shipping, policies
```

### Real Estate
```
Tools:
├── search_properties          → By area, price, type, beds
├── get_property_details       → Full listing with media
├── check_availability         → Inspection time slots
├── book_inspection            → Schedule property viewing
├── get_suburb_info            → Demographics, schools, transport
├── calculate_repayment        → Mortgage calculator
├── submit_enquiry             → Contact agent about property
└── search_knowledge_base      → Buying process FAQ
```

### Education / Training
```
Tools:
├── search_courses             → By topic, level, format
├── get_course_details         → Syllabus, schedule, pricing
├── check_availability         → Seats remaining
├── enrol_student              → Course registration
├── get_learning_path          → Recommended course sequence
└── search_knowledge_base      → Admission, fees, policies
```

## Implementation Patterns

### Tool Executor Switch Pattern
```csharp
public async Task<ToolExecutionResult> ExecuteAsync(string toolName, JsonElement input, CancellationToken ct)
{
    try
    {
        return toolName switch
        {
            "search_products" => await ExecuteSearchProducts(input, ct),
            "create_booking"  => await ExecuteCreateBooking(input, ct),
            _ => ToolExecutionResult.Fail("The requested operation is not available.")
        };
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error executing tool {ToolName}", toolName);
        return ToolExecutionResult.Fail("Unable to complete the operation. Please try again.");
    }
}
```

### JSON Property Helper
```csharp
private static string? GetStringProperty(JsonElement element, string propertyName)
{
    if (element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String)
        return prop.GetString();
    return null;
}

private static int? GetIntProperty(JsonElement element, string propertyName)
{
    if (element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.Number)
        return prop.GetInt32();
    return null;
}

private static bool GetBoolProperty(JsonElement element, string propertyName)
{
    if (element.TryGetProperty(propertyName, out var prop))
        return prop.ValueKind == JsonValueKind.True;
    return false;
}
```

### Tool Result Pattern
```csharp
// ✅ GOOD: Return structured data for Claude to interpret
return ToolExecutionResult.Ok(JsonSerializer.Serialize(new {
    success = true,
    booking_id = id.ToString(),
    message = "Booking created successfully!",
    details = new { name, date, time }
}, JsonOptions));

// ❌ BAD: Return free text
return ToolExecutionResult.Ok("The booking was created for John at 3pm on Monday.");
```

### Health Concern Mapping (Healthcare domain)
```csharp
private static string[] MapHealthConcernToSearchTerms(string concern)
{
    var c = concern.ToLowerInvariant();
    return c switch
    {
        var x when x.Contains("immune") || x.Contains("cold") => new[] { "vitamin C", "zinc", "echinacea" },
        var x when x.Contains("energy") || x.Contains("tired") => new[] { "vitamin B", "iron", "CoQ10" },
        var x when x.Contains("sleep") => new[] { "magnesium", "melatonin", "valerian" },
        var x when x.Contains("joint") => new[] { "glucosamine", "fish oil", "turmeric" },
        _ => new[] { concern }
    };
}
```

### In-Memory Data for MVP / Prototyping
```csharp
// Start with hardcoded data, migrate to database later
private static readonly List<CaseStudy> _caseStudies = new()
{
    new("Pharmacy Management", "healthcare", "Legacy modernization with AI customer service", ...),
    new("Cloud Migration", "real-estate", "On-premises to AWS serverless", ...),
};

// Tool executor uses in-memory data
private Task<ToolExecutionResult> ExecuteGetCaseStudies(JsonElement input, CancellationToken ct)
{
    var industry = GetStringProperty(input, "industry");
    var filtered = industry != null
        ? _caseStudies.Where(c => c.Industry == industry)
        : _caseStudies;
    return Task.FromResult(ToolExecutionResult.Ok(JsonSerializer.Serialize(filtered)));
}
```

## Adding a New Tool — Step by Step

1. **Define** in AiToolDefinitions.cs: name, description, input_schema
2. **Add to GetAllTools()** list
3. **Implement** ExecuteXxx method in AiToolExecutor.cs
4. **Register** in the switch statement
5. **Update system prompt** to mention the new tool's purpose
6. **Test** with DryRun=false: verify Claude calls it correctly
