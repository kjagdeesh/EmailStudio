namespace EmailStudio.Models;

public sealed class EmailTemplate
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "Untitled template";
    public string Category { get; set; } = "Custom";
    public string Description { get; set; } = "";
    public string BrandName { get; set; } = "Your brand";
    public string BrandSignature { get; set; } = "Your team";
    public string BrandLogoUrl { get; set; } = "";
    public string BrandWebsiteUrl { get; set; } = "";
    public string BackgroundColor { get; set; } = "#f3f5f9";
    public string SurfaceColor { get; set; } = "#ffffff";
    public string TextColor { get; set; } = "#182033";
    public string AccentColor { get; set; } = "#6658ed";
    public List<EmailBlock> Blocks { get; set; } = [];
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
}

public sealed class EmailBlock
{
    public string Type { get; set; } = "text";
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
    public string Url { get; set; } = "";
    public string BackgroundColor { get; set; } = "#ffffff";
    public string TextColor { get; set; } = "#182033";
    public string AccentColor { get; set; } = "#6658ed";
    public string TableStyle { get; set; } = "classic";
}

public record EmailTheme(string Name, string Background, string Surface, string Text, string Accent);
