using System.Text.Json;
using EmailStudio.Models;
using EmailStudio.Services;

static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
var dir = Path.Combine(Path.GetTempPath(), "email-studio-checks-" + Guid.NewGuid().ToString("N"));
var migrationDir = dir + "-migration";
try
{
    var store = new TemplateStore(new FileStorage(dir));
    var seeds = await store.ListAsync();
    Check(seeds.Count >= 120 && seeds.Select(x => x.Name).Distinct().Count() >= 120, "Starter library count");
    Check(new[] { "Reports", "Stocks", "Tables" }.All(c => seeds.Any(t => t.Category == c)), "Categories");
    Check(seeds.All(t => t.Blocks.Any(b => b.Type == "image" && string.IsNullOrWhiteSpace(b.Url))), "Branded image placeholders in starter templates");
    Check(seeds.All(t => t.Blocks.Any(b => b.Type == "footer") && t.BrandName == "ABC Company" && t.BrandSignature == "The ABC Company team"), "Brand identity and signature");
    Check(TemplateStore.Themes.Length >= 32, "Theme count");
    Check(new[] { "classic", "striped", "accent" }.All(style => seeds.Any(t => t.Blocks.Any(b => b.Type == "table" && b.TableStyle == style))), "Starter table designs");
    Check(new[] { "line chart", "area chart", "pie chart", "doughnut chart" }.All(type => seeds.Any(t => t.Blocks.Any(b => b.Type == type))), "Chart starter examples");
    var legacy = new EmailTemplate
    {
        Name = "Weekly performance",
        Category = "Reports",
        Description = "A ready-to-customize weekly performance email for your reports workflow.",
        Blocks =
        [
            new() { Type = "header", Title = "Weekly performance", Content = "REPORTS / YOUR COMPANY" },
            new() { Type = "text", Title = "A clearer picture, delivered.", Content = "Hello,\n\nHere's your weekly performance. Replace this sample copy and data with the details that matter to your audience." },
            new() { Type = "bar chart", Title = "Trends at a glance", Content = "Week 1,42\nWeek 2,58\nWeek 3,67\nWeek 4,79", AccentColor = "#123456" },
            new() { Type = "table", Title = "The details", Content = "Metric | Current | Previous\nTotal | 12,480 | 10,920\nChange | +14% | +8%\nStatus | On track | On track" },
            new() { Type = "button", Title = "View the full update", Url = "https://example.com" },
            new() { Type = "footer", Title = "Your company", Content = "You're receiving this email because you opted in. Update your preferences anytime." }
        ]
    };
    Check(StarterTemplates.TryUpgradeLegacy(legacy, out var upgraded) && upgraded!.Blocks.Any(b => b.Type == "image"), "Legacy starter migration");
    Check(upgraded!.Blocks.First(b => b.Type == "bar chart").AccentColor == "#123456", "Legacy theme preservation");
    Directory.CreateDirectory(migrationDir);
    File.WriteAllText(Path.Combine(migrationDir, "templates.json"), JsonSerializer.Serialize(new[] { legacy }, TemplateStore.JsonOptions));
    var migratedLibrary = await new TemplateStore(new FileStorage(migrationDir)).ListAsync();
    var migrated = migratedLibrary.Single(t => t.Id == legacy.Id);
    Check(migratedLibrary.Count == 61 && migrated.Blocks.Any(b => b.Type == "image") && migrated.BrandName == "ABC Company", "Persisted legacy library migration");
    Check(File.ReadAllText(Path.Combine(migrationDir, "templates.json")).Contains("ABC Company"), "Legacy migration persisted");
    var migrationStore = new TemplateStore(new FileStorage(migrationDir));
    var addedStarter = migratedLibrary.First(t => t.Id != legacy.Id);
    addedStarter.Name = "A renamed starter";
    addedStarter.BrandName = "Custom brand";
    addedStarter.Blocks[0].Content = "Custom copy";
    await migrationStore.SaveAsync(addedStarter);
    var reloaded = await new TemplateStore(new FileStorage(migrationDir)).ListAsync();
    Check(reloaded.Count == 61 && reloaded.Single(t => t.Id == addedStarter.Id).Name == "A renamed starter" &&
        reloaded.Single(t => t.Id == addedStarter.Id).BrandName == "Custom brand" &&
        reloaded.Single(t => t.Id == addedStarter.Id).Blocks[0].Content == "Custom copy", "Idempotent migration preserves edits");
    legacy.Blocks[1].Content += " Custom copy.";
    Check(!StarterTemplates.TryUpgradeLegacy(legacy, out _), "Do not replace customized legacy content");
    var previousStarter = StarterTemplates.Create().First(t => t.Name == "Weekly performance");
    previousStarter.BrandName = "Northstar";
    previousStarter.BrandSignature = "The Northstar team";
    previousStarter.Blocks[0].Content = "Custom copy";
    Check(StarterTemplates.UpgradeDefaultBrand(previousStarter) && previousStarter.BrandName == "ABC Company" &&
          previousStarter.Blocks[0].Content == "Custom copy", "Safe upgrade for prior default brand");
    previousStarter.BrandName = "Northstar"; previousStarter.BrandSignature = "Custom signature";
    Check(!StarterTemplates.UpgradeDefaultBrand(previousStarter) && previousStarter.BrandName == "Northstar", "Preserve customized branding");
    var olderJson = JsonSerializer.Deserialize<EmailTemplate>("""{"name":"Imported legacy","category":"Custom","blocks":[]}""", TemplateStore.JsonOptions);
    Check(olderJson?.BrandName == "Your brand" && olderJson.BrandSignature == "Your team", "Backward-compatible brand defaults");
    var olderBlock = JsonSerializer.Deserialize<EmailBlock>("""{"type":"table","content":"A|B"}""", TemplateStore.JsonOptions);
    Check(olderBlock?.TableStyle == "classic", "Backward-compatible table design");
    var template = TemplateStore.Clone(seeds.First(t => t.Category == "Forecasts"));
    template.Id = Guid.NewGuid(); template.Name = "Test <template>";
    template.Blocks[0].Title = "<script>alert(1)</script>";
    template.Blocks.First(b => b.Type == "forecast chart").Content = "Actual,5\nFuture,10,forecast";
    await store.SaveAsync(template);
    var saved = await new TemplateStore(new FileStorage(dir)).GetAsync(template.Id);
    Check(saved?.Name == template.Name, "Persistence after reload");
    var extra = TemplateStore.Clone(template);
    extra.Id = Guid.NewGuid();
    extra.Name = "Another imported template";
    var changed = TemplateStore.Clone(template);
    changed.Name = "Imported version";
    Check(await store.ImportLibraryAsync([template, extra, extra, changed]) == 2, "Library import skips identical designs and copies conflicts");
    Check((await store.ListAsync()).Single(t => t.Id == template.Id).Name == template.Name, "Library import preserves existing designs");
    Check((await store.ListAsync()).Any(t => t.Id != template.Id && t.Name == changed.Name), "Conflicting imported edit is preserved");
    Check(await store.ImportLibraryAsync([template, extra, changed]) == 0, "Library import is repeatable");
    var stale = TemplateStore.Clone(saved!);
    var staleSnapshot = JsonSerializer.Serialize(stale, TemplateStore.JsonOptions);
    var newer = TemplateStore.Clone(stale);
    newer.Name = "Edited in another tab";
    await store.SaveAsync(newer);
    try { await store.SaveEditedAsync(stale, staleSnapshot); throw new Exception("Stale edit accepted"); }
    catch (InvalidOperationException) { }
    Check((await store.GetAsync(stale.Id))!.Name == newer.Name, "Stale edit does not overwrite newer changes");
    var roundtrip = JsonSerializer.Deserialize<EmailTemplate>(JsonSerializer.Serialize(saved, TemplateStore.JsonOptions), TemplateStore.JsonOptions)!;
    TemplateStore.Validate(roundtrip);
    var html = EmailRenderer.Render(roundtrip);
    Check(html.Contains("&lt;script&gt;") && !html.Contains("<script>"), "HTML escaping");
    Check(html.Contains("ABC Company") && html.Contains("The ABC Company team") && html.Contains("IMAGE PREVIEW") &&
        html.Contains(">ABC</span>") && html.Contains("border-radius:16px"), "Branding, logo, signature, and image placeholder rendering");
    Check(html.Contains("<span style=\"display:inline-block;background:") && !html.Contains("href=\"https://example.com\""), "Unconfigured calls to action are non-navigating");
    Check(html.Contains("Future (forecast)") && html.Contains("width=\"50%\""), "Forecast chart output");
    var image = roundtrip.Blocks.First(b => b.Type == "image");
    image.Url = "https://cdn.example/image.jpg";
    Check(EmailRenderer.Render(roundtrip).Contains("src=\"https://cdn.example/image.jpg\""), "Image URL rendering");
    image.Url = "";
    var table = roundtrip.Blocks.First(b => b.Type == "table");
    var tableOutputs = new List<string>();
    foreach (var style in new[] { "classic", "striped", "accent" })
    {
        table.TableStyle = style;
        var output = EmailRenderer.Render(roundtrip);
        Check(output.Contains("border-radius:12px") && output.Contains("Key indicators") == (table.Title == "Key indicators"), $"{style} rounded table");
        tableOutputs.Add(output);
    }
    Check(tableOutputs.Distinct().Count() == 3, "Distinct table designs");
    table.TableStyle = "invalid";
    try { TemplateStore.Validate(roundtrip); throw new Exception("Invalid table style accepted"); } catch (InvalidDataException) { }
    table.TableStyle = "classic";
    foreach (var type in new[] { "line chart", "area chart", "pie chart", "doughnut chart" })
    {
        var chart = roundtrip.Blocks.First(b => b.Type == "forecast chart");
        chart.Type = type; chart.Content = "First,5\nSecond,10";
        var output = EmailRenderer.Render(roundtrip);
        Check(output.Contains("<svg") && output.Contains("First") && output.Contains("Second"), $"{type}: visual and fallback");
        Check(type switch
        {
            "line chart" => output.Contains("<polyline"),
            "area chart" => output.Contains("<polygon"),
            "pie chart" => output.Contains("<path"),
            _ => output.Contains("r=\"27\"")
        }, $"{type}: shape");
        chart.Type = "forecast chart";
    }
    var pie = roundtrip.Blocks.First(b => b.Type == "forecast chart");
    pie.Type = "pie chart"; pie.Content = "Only,10\nZero,0";
    Check(EmailRenderer.Render(roundtrip).Contains("r=\"50\""), "Single-slice pie");
    pie.Content = "First,0\nSecond,0";
    Check(EmailRenderer.Render(roundtrip).Contains("First"), "Zero-value pie fallback");
    roundtrip.Blocks[0].TextColor = "red;evil";
    try { TemplateStore.Validate(roundtrip); throw new Exception("Invalid color accepted"); } catch (InvalidDataException) { }
    roundtrip.Blocks[0].TextColor = "#123456";
    roundtrip.BrandLogoUrl = "javascript:alert(1)";
    try { TemplateStore.Validate(roundtrip); throw new Exception("Unsafe logo URL accepted"); } catch (InvalidDataException) { }
    roundtrip.BrandLogoUrl = "";
    pie.Content = "Future,not-a-number";
    try { TemplateStore.Validate(roundtrip); throw new Exception("Invalid chart accepted"); } catch (InvalidDataException) { }
    
    // C# Export checks
    var csCode = CSharpEmailExporter.Export(seeds.First());
    Check(csCode.Contains("public static class"), "C# export generates static class");
    Check(csCode.Contains("public static string Render("), "C# export generates Render method");
    Check(csCode.Contains("public static string GenerateHtml("), "C# export generates GenerateHtml method");
    Check(csCode.Contains("public sealed class EmailModel"), "C# export generates EmailModel class");
    Check(csCode.Contains("html.Append($\"\"\""), "C# export uses string interpolation");
    Check(csCode.Contains("brandName"), "C# export parameterizes brandName");
    Check(csCode.Contains("brandSignature"), "C# export parameterizes brandSignature");

    Console.WriteLine("Passed seed, theme, persistence, JSON, HTML, C# export, chart and validation checks.");
}
finally
{
    if (Directory.Exists(dir)) Directory.Delete(dir, true);
    if (Directory.Exists(migrationDir)) Directory.Delete(migrationDir, true);
}

sealed class FileStorage(string directory) : ITemplateStorage
{
    private string PathName => Path.Combine(directory, "templates.json");
    public async Task<string?> ReadAsync() => File.Exists(PathName) ? await File.ReadAllTextAsync(PathName) : null;
    public async Task WriteAsync(string? expected, string value)
    {
        if (await ReadAsync() != expected) throw new InvalidOperationException("Concurrent library change");
        Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(PathName, value);
    }
}
