using System.Text.Json;
using System.Text.RegularExpressions;
using System.Globalization;
using EmailStudio.Models;

namespace EmailStudio.Services;

public sealed class TemplateStore
{
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private readonly ITemplateStorage storage;
    private string? loadedJson;
    private readonly SemaphoreSlim gate = new(1, 1);
    private List<EmailTemplate>? templates;
    public static readonly string[] BlockTypes = ["header", "text", "button", "image", "table", "bar chart", "comparison chart", "forecast chart", "line chart", "area chart", "pie chart", "doughnut chart", "divider", "footer"];
    public static readonly EmailTheme[] Themes =
    [
        new("Indigo", "#f3f5f9", "#ffffff", "#182033", "#6658ed"),
        new("Ocean", "#edf7fa", "#ffffff", "#123444", "#007f9e"),
        new("Forest", "#eef6ef", "#ffffff", "#193529", "#2f8059"),
        new("Sunset", "#fff3ec", "#ffffff", "#4b2925", "#df704b"),
        new("Rose", "#fff1f4", "#ffffff", "#4f2732", "#c64c73"),
        new("Midnight", "#151d2e", "#243047", "#f1f5ff", "#8e9fff"),
        new("Slate", "#edf0f3", "#ffffff", "#263342", "#526a83"),
        new("Citrus", "#fbf8e9", "#ffffff", "#373623", "#a67b12"),
        new("Lavender", "#f5f0fb", "#ffffff", "#34264a", "#9062bf"),
        new("Teal", "#eaf7f5", "#ffffff", "#173e3a", "#168779"),
        new("Coral", "#fff1ee", "#ffffff", "#482d2b", "#dd655a"),
        new("Sapphire", "#eef4ff", "#ffffff", "#1a3152", "#3468c9"),
        new("Earth", "#f4f1eb", "#fffdf9", "#433a2d", "#927351"),
        new("Mint", "#effaf4", "#ffffff", "#1d3d30", "#39a66e"),
        new("Berry", "#f9eef5", "#ffffff", "#48283e", "#a34883"),
        new("Graphite", "#e9ebee", "#ffffff", "#20252c", "#454f5e"),
        new("Azure", "#eef6ff", "#ffffff", "#192e49", "#2467c5"),
        new("Clay", "#fbf1ed", "#ffffff", "#492d28", "#b6553f"),
        new("Plum", "#f7f0f8", "#ffffff", "#3a2941", "#854ca0"),
        new("Pine", "#edf5f1", "#ffffff", "#18362b", "#287252"),
        new("Marine", "#eaf3f7", "#ffffff", "#18384a", "#176d91"),
        new("Honey", "#faf5e8", "#ffffff", "#44361e", "#a36a10"),
        new("Orchid", "#f7effa", "#ffffff", "#3e294c", "#a04cb2"),
        new("Brick", "#f9eeeb", "#ffffff", "#492d29", "#ae493f"),
        new("Nordic", "#eef2f5", "#ffffff", "#233646", "#476c87"),
        new("Lagoon", "#eaf8f6", "#ffffff", "#153c3b", "#0c817c"),
        new("Peach", "#fff3ec", "#ffffff", "#523529", "#c76642"),
        new("Iris", "#f0effb", "#ffffff", "#302c4c", "#6758bd"),
        new("Olive", "#f2f5e9", "#ffffff", "#33402b", "#718337"),
        new("Ruby", "#fbf0f2", "#ffffff", "#4a2630", "#b43959"),
        new("Ink", "#e9edf4", "#ffffff", "#17273f", "#314b78"),
        new("Sand", "#f8f3eb", "#ffffff", "#45392d", "#927149")
    ];

    public TemplateStore(ITemplateStorage storage) => this.storage = storage;

    private async Task EnsureLoadedAsync()
    {
        loadedJson = await storage.ReadAsync();
        if (loadedJson is not null)
        {
            templates = JsonSerializer.Deserialize<List<EmailTemplate>>(loadedJson, JsonOptions)
                ?? throw new InvalidDataException("The template library is empty or invalid.");
            var migrated = false;
            for (var index = 0; index < templates.Count; index++)
            {
                if (StarterTemplates.TryUpgradeLegacy(templates[index], out var replacement))
                {
                    templates[index] = replacement!;
                    migrated = true;
                }
                else if (StarterTemplates.UpgradeDefaultBrand(templates[index]))
                    migrated = true;
            }
            var existingIds = templates.Select(t => t.Id).ToHashSet();
            var existingNames = templates.Select(t => (t.Category, t.Name)).ToHashSet();
            foreach (var starter in StarterTemplates.CreateAdditional())
                if (!existingIds.Contains(starter.Id) && !existingNames.Contains((starter.Category, starter.Name)))
                {
                    templates.Add(starter);
                    migrated = true;
                }
            if (migrated) await PersistAsync();
        }
        else
        {
            templates = StarterTemplates.Create();
            await PersistAsync();
        }
    }

    private async Task PersistAsync()
    {
        var json = JsonSerializer.Serialize(templates, JsonOptions);
        await storage.WriteAsync(loadedJson, json);
        loadedJson = json;
    }

    public static EmailTemplate Clone(EmailTemplate value) =>
        JsonSerializer.Deserialize<EmailTemplate>(JsonSerializer.Serialize(value, JsonOptions), JsonOptions)!;

    public static string ContrastText(string color)
    {
        static double Linear(int value) { var c = value / 255.0; return c <= 0.04045 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4); }
        var r = Convert.ToInt32(color.Substring(1, 2), 16);
        var g = Convert.ToInt32(color.Substring(3, 2), 16);
        var b = Convert.ToInt32(color.Substring(5, 2), 16);
        return 0.2126 * Linear(r) + 0.7152 * Linear(g) + 0.0722 * Linear(b) > 0.179 ? "#000000" : "#ffffff";
    }

    public async Task<List<EmailTemplate>> ListAsync()
    {
        await gate.WaitAsync();
        try { await EnsureLoadedAsync(); return templates!.Select(Clone).ToList(); }
        finally { gate.Release(); }
    }

    public async Task<EmailTemplate?> GetAsync(Guid id) =>
        (await ListAsync()).FirstOrDefault(t => t.Id == id);

    public Task SaveAsync(EmailTemplate template) => SaveCoreAsync(template, false, null);

    public Task SaveEditedAsync(EmailTemplate template, string? expectedSnapshot) =>
        SaveCoreAsync(template, true, expectedSnapshot);

    private async Task SaveCoreAsync(EmailTemplate template, bool checkSnapshot, string? expectedSnapshot)
    {
        Validate(template);
        await gate.WaitAsync();
        try
        {
            await EnsureLoadedAsync();
            var copy = Clone(template);
            copy.UpdatedUtc = DateTime.UtcNow;
            var index = templates!.FindIndex(t => t.Id == copy.Id);
            var previous = index < 0 ? null : templates[index];
            var actualSnapshot = previous is null ? null : JsonSerializer.Serialize(previous, JsonOptions);
            if (checkSnapshot && actualSnapshot != expectedSnapshot)
                throw new InvalidOperationException("This design changed in another tab. Copy your changes before reopening it.");
            if (index < 0) templates.Add(copy); else templates[index] = copy;
            try { await PersistAsync(); }
            catch { if (index < 0) templates.Remove(copy); else templates[index] = previous!; throw; }
        }
        finally { gate.Release(); }
    }

    public async Task<int> ImportLibraryAsync(List<EmailTemplate> imported)
    {
        foreach (var template in imported) Validate(template);
        await gate.WaitAsync();
        try
        {
            await EnsureLoadedAsync();
            var current = templates!;
            var ids = current.Select(t => t.Id).ToHashSet();
            static string ContentKey(EmailTemplate value)
            {
                var copy = Clone(value);
                copy.Id = Guid.Empty;
                copy.UpdatedUtc = DateTime.MinValue;
                return JsonSerializer.Serialize(copy, JsonOptions);
            }
            var contents = current.Select(ContentKey).ToHashSet();
            var added = new List<EmailTemplate>();
            foreach (var item in imported)
            {
                if (!contents.Add(ContentKey(item))) continue;
                var copy = Clone(item);
                if (!ids.Add(copy.Id))
                {
                    copy.Id = Guid.NewGuid();
                    ids.Add(copy.Id);
                }
                added.Add(copy);
            }
            if (added.Count == 0) return 0;
            current.AddRange(added);
            await PersistAsync();
            return added.Count;
        }
        finally { gate.Release(); }
    }

    public static void Validate(EmailTemplate t)
    {
        if (t.Id == Guid.Empty || string.IsNullOrWhiteSpace(t.Name) || t.Name.Length > 120 ||
            string.IsNullOrWhiteSpace(t.Category) || t.Category.Length > 60 || t.Description is null || t.Description.Length > 500 ||
            string.IsNullOrWhiteSpace(t.BrandName) || t.BrandName.Length > 100 || t.BrandSignature is null || t.BrandSignature.Length > 200 ||
            t.BrandLogoUrl is null || t.BrandLogoUrl.Length > 2000 || t.BrandWebsiteUrl is null || t.BrandWebsiteUrl.Length > 2000)
            throw new InvalidDataException("Provide a name (up to 120 characters), a category, and a description under 500 characters.");
        if (t.Blocks is null || t.Blocks.Count > 100)
            throw new InvalidDataException("A template can contain at most 100 blocks.");
        ValidateOptionalWebUrl(t.BrandLogoUrl, "Brand logo URLs");
        ValidateOptionalWebUrl(t.BrandWebsiteUrl, "Brand website URLs");
        CheckColor(t.BackgroundColor); CheckColor(t.SurfaceColor); CheckColor(t.TextColor); CheckColor(t.AccentColor);
        foreach (var b in t.Blocks)
        {
            if (b is null || !BlockTypes.Contains(b.Type) || b.Title is null || b.Content is null || b.Url is null ||
                b.Title.Length > 300 || b.Content.Length > 20000 || b.Url.Length > 2000)
                throw new InvalidDataException("A block contains invalid or oversized fields.");
            CheckColor(b.BackgroundColor); CheckColor(b.TextColor); CheckColor(b.AccentColor);
            if (b.TableStyle is not ("classic" or "striped" or "accent"))
                throw new InvalidDataException("Choose a supported table style.");
            if (!string.IsNullOrWhiteSpace(b.Url) && (!Uri.TryCreate(b.Url, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeMailto)))
                throw new InvalidDataException("Links and image URLs must use http, https, or mailto.");
            if (b.Type == "image" && b.Url.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Image URLs must use http or https.");
            if (b.Type.Contains("chart"))
            {
                var lines = b.Content.Replace("\r\n", "\n").Split('\n', StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length > 50) throw new InvalidDataException("A chart can contain at most 50 rows.");
                foreach (var line in lines)
                {
                    var parts = line.Split(',');
                    if (parts.Length < 2 || parts.Length > 3 || string.IsNullOrWhiteSpace(parts[0]) ||
                        !double.TryParse(parts[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var value) || !double.IsFinite(value) || value < 0 ||
                        (parts.Length == 3 && (b.Type != "forecast chart" || !parts[2].Trim().Equals("forecast", StringComparison.OrdinalIgnoreCase))))
                        throw new InvalidDataException("Chart rows must be label,non-negative number, optionally followed by ,forecast for forecast charts.");
                }
            }
            if (b.Type == "table")
            {
                var lines = b.Content.Replace("\r\n", "\n").Split('\n', StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length > 100 || lines.Any(line => line.Split('|').Length > 12))
                    throw new InvalidDataException("A table can contain at most 100 rows and 12 columns.");
            }
        }
    }

    private static void CheckColor(string? color)
    {
        if (color is null || !Regex.IsMatch(color, "^#[0-9a-fA-F]{6}$"))
            throw new InvalidDataException("Colors must be six-digit hex values.");
    }

    private static void ValidateOptionalWebUrl(string? value, string label)
    {
        if (!string.IsNullOrWhiteSpace(value) &&
            (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
             (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)))
            throw new InvalidDataException($"{label} must use http or https.");
    }
}
