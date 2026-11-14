using EmailStudio.Models;
using System.Security.Cryptography;
using System.Text;

namespace EmailStudio.Services;

public static class StarterTemplates
{
    private static readonly Dictionary<string, string[]> Groups = new()
    {
        ["Reports"] = ["Weekly performance", "Monthly business review", "Quarterly highlights", "Annual summary", "Marketing campaign report", "Website analytics", "Product adoption", "Customer satisfaction", "Team progress", "Revenue snapshot"],
        ["Stocks"] = ["Market open briefing", "Closing bell", "Portfolio overview", "Stock watchlist", "Dividend update", "Market movers", "Earnings preview", "Sector performance", "Investment outlook", "Risk summary"],
        ["Tables"] = ["Sales leaderboard", "Inventory status", "Order summary", "Invoice breakdown", "Project milestones", "Event schedule", "Team directory", "Subscription usage", "Expense overview", "Delivery tracker"],
        ["Newsletters"] = ["Company news", "Community digest", "Product updates", "Industry roundup", "Founder letter", "Weekly reading", "Partner news", "Editorial picks", "Events digest", "Behind the scenes"],
        ["Business"] = ["Welcome aboard", "Meeting follow-up", "Project proposal", "Client check-in", "Service announcement", "Customer onboarding", "Feedback request", "Partnership update", "Quarterly planning", "Team announcement"],
        ["Forecasts"] = ["Revenue forecast", "Demand projection", "Budget outlook", "Pipeline forecast", "Growth projection", "Traffic forecast", "Capacity planning", "Sales outlook", "Expense projection", "Hiring forecast"]
    };

    private static readonly Dictionary<string, string[]> AdditionalGroups = new()
    {
        ["Reports"] = ["Executive dashboard", "Sales pulse", "Campaign recap", "Operations review", "Customer health report", "Growth scorecard", "Audience insights", "Product usage report", "Support performance", "Retention analysis"],
        ["Stocks"] = ["Weekly market review", "ETF watch", "Portfolio allocation", "Company earnings recap", "Global market update", "Income investing", "Equity research", "Bond market brief", "Economic indicators", "Investor letter"],
        ["Tables"] = ["Account activity", "Project task list", "Purchase order", "Pricing comparison", "Membership summary", "Booking confirmation", "Resource allocation", "Payment schedule", "Service status", "Training timetable"],
        ["Newsletters"] = ["The monthly edit", "New arrivals", "Community spotlight", "Upcoming events", "Seasonal update", "Expert insights", "The Friday roundup", "Stories we love", "Team journal", "Inside the studio"],
        ["Business"] = ["Client welcome", "Order confirmation", "Thank you note", "Consultation reminder", "Proposal follow-up", "Account update", "Launch announcement", "Renewal reminder", "Customer milestone", "Workshop invitation"],
        ["Forecasts"] = ["Quarterly sales projection", "Cash flow outlook", "Customer growth forecast", "Resource demand forecast", "Market scenario", "Subscriber outlook", "Supply forecast", "Campaign projection", "Revenue scenario", "Workforce planning"]
    };

    public static List<EmailTemplate> Create() => CreateGroup(Groups, 0).Concat(CreateAdditional()).ToList();

    public static List<EmailTemplate> CreateAdditional() => CreateGroup(AdditionalGroups, 60);

    private static List<EmailTemplate> CreateGroup(Dictionary<string, string[]> groups, int startIndex)
    {
        var result = new List<EmailTemplate>();
        var index = startIndex;
        foreach (var (category, names) in groups)
        foreach (var name in names)
        {
            var theme = TemplateStore.Themes[index % TemplateStore.Themes.Length];
            var story = StoryFor(category, name);
            var template = new EmailTemplate
            {
                Name = name,
                Category = category,
                Description = DescriptionFor(category),
                BrandName = "ABC Company",
                BrandSignature = "The ABC Company team",
                BackgroundColor = theme.Background,
                SurfaceColor = theme.Surface,
                TextColor = theme.Text,
                AccentColor = theme.Accent
            };
            if (startIndex > 0)
                template.Id = new Guid(SHA256.HashData(Encoding.UTF8.GetBytes($"EmailStudio starter v2:{category}:{name}"))[..16]);

            EmailBlock Block(string type, string title, string content = "", string url = "") => new()
            {
                Type = type,
                Title = title,
                Content = content,
                Url = url,
                BackgroundColor = theme.Surface,
                TextColor = type == "button" ? TemplateStore.ContrastText(theme.Accent) : theme.Text,
                AccentColor = theme.Accent
            };

            template.Blocks.Add(Block("header", name, category.ToUpperInvariant()));
            var image = Block("image", $"A visual for {name.ToLowerInvariant()}");
            var introduction = Block("text", story.Title, story.Copy);
            if (index % 2 == 0) { template.Blocks.Add(image); template.Blocks.Add(introduction); }
            else { template.Blocks.Add(introduction); template.Blocks.Add(image); }

            EmailBlock? chart = null;
            if (category is "Reports" or "Stocks" or "Forecasts")
            {
                var chartType = category == "Forecasts" ? "forecast chart" :
                    new[] { "bar chart", "comparison chart", "line chart", "area chart", "pie chart", "doughnut chart" }[index % 6];
                chart = Block(chartType, story.ChartTitle, ChartData(category, index));
            }

            EmailBlock? table = null;
            if (category is "Tables" or "Reports" or "Stocks" or "Forecasts")
            {
                table = Block("table", story.TableTitle, TableData(category));
                table.TableStyle = new[] { "classic", "striped", "accent" }[index % 3];
            }
            if (index % 3 == 0 && table is not null) template.Blocks.Add(table);
            if (chart is not null) template.Blocks.Add(chart);
            if (index % 3 != 0 && table is not null) template.Blocks.Add(table);
            if (index % 3 == 1) template.Blocks.Add(Block("divider", ""));
            if (index % 3 == 2)
                template.Blocks.Add(Block("text", "What to keep in mind", "Add a useful takeaway or a short personal note here before your call to action."));

            template.Blocks.Add(Block("button", story.Button));
            template.Blocks.Add(Block("footer", "A note from our team",
                "You are receiving this note because you signed up for ABC Company updates. You can update your preferences or unsubscribe at any time."));
            result.Add(template);
            index++;
        }

        return result;
    }

    public static bool TryUpgradeLegacy(EmailTemplate original, out EmailTemplate? upgraded)
    {
        upgraded = null;
        if (string.IsNullOrWhiteSpace(original.Name) || string.IsNullOrWhiteSpace(original.Category) ||
            !Groups.TryGetValue(original.Category, out var names) ||
            original.BrandName != "Your brand" ||
            original.BrandSignature != "Your team" ||
            !string.IsNullOrEmpty(original.BrandLogoUrl) ||
            !string.IsNullOrEmpty(original.BrandWebsiteUrl) ||
            original.Description != $"A ready-to-customize {original.Name.ToLowerInvariant()} email for your {original.Category.ToLowerInvariant()} workflow.")
            return false;

        var localIndex = Array.IndexOf(names, original.Name);
        if (localIndex < 0 || original.Blocks is null || original.Blocks.Any(block => block is null)) return false;
        var legacyBrandLine = original.Category.ToUpperInvariant() + " / YOUR COMPANY";
        var headerContent = original.Blocks.FirstOrDefault()?.Content;
        if (headerContent != original.Category.ToUpperInvariant() && headerContent != legacyBrandLine) return false;
        var index = localIndex;
        foreach (var category in Groups.TakeWhile(item => item.Key != original.Category))
            index += category.Value.Length;

        var expected = new List<(string Type, string Title, string Content, string Url)>
        {
            ("header", original.Name, headerContent!, "")
        };
        expected.Add(("text", "A clearer picture, delivered.",
            $"Hello,\n\nHere's your {original.Name.ToLowerInvariant()}. Replace this sample copy and data with the details that matter to your audience.", ""));
        if (original.Category is "Reports" or "Stocks" or "Forecasts")
        {
            var chartType = original.Category == "Forecasts" ? "forecast chart" :
                new[] { "bar chart", "comparison chart", "line chart", "area chart", "pie chart", "doughnut chart" }[index % 6];
            var chartContent = original.Category switch
            {
                "Forecasts" => "Jan,42\nFeb,55\nMar,64\nApr,72,forecast\nMay,81,forecast",
                "Stocks" => "Technology,78\nHealthcare,62\nEnergy,45\nFinancials,54",
                _ => "Week 1,42\nWeek 2,58\nWeek 3,67\nWeek 4,79"
            };
            expected.Add((chartType, original.Category == "Stocks" ? "Performance at a glance" : "Trends at a glance", chartContent, ""));
        }
        if (original.Category is "Tables" or "Reports" or "Stocks" or "Forecasts")
            expected.Add(("table", "The details", "Metric | Current | Previous\nTotal | 12,480 | 10,920\nChange | +14% | +8%\nStatus | On track | On track", ""));
        expected.Add(("button", "View the full update", "", "https://example.com"));
        expected.Add(("footer", "Your company", "You're receiving this email because you opted in. Update your preferences anytime.", ""));

        if (original.Blocks.Count != expected.Count ||
            original.Blocks.Where((block, i) => block.Type != expected[i].Type || block.Title != expected[i].Title ||
                block.Content != expected[i].Content || block.Url != expected[i].Url).Any())
            return false;

        var replacement = Create().First(t => t.Category == original.Category && t.Name == original.Name);
        replacement.Id = original.Id;
        replacement.UpdatedUtc = original.UpdatedUtc;
        foreach (var block in replacement.Blocks)
        {
            var previous = original.Blocks.FirstOrDefault(candidate => candidate.Type == block.Type);
            if (previous is null) continue;
            block.BackgroundColor = previous.BackgroundColor;
            block.TextColor = previous.TextColor;
            block.AccentColor = previous.AccentColor;
        }
        upgraded = replacement;
        return true;
    }

    public static bool UpgradeDefaultBrand(EmailTemplate original)
    {
        if (original.BrandName != "Northstar" || original.BrandSignature != "The Northstar team" ||
            !string.IsNullOrEmpty(original.BrandLogoUrl) || !string.IsNullOrEmpty(original.BrandWebsiteUrl))
            return false;
        original.BrandName = "ABC Company";
        original.BrandSignature = "The ABC Company team";
        foreach (var block in original.Blocks)
        {
            if (block.Title == "A good note from Northstar") block.Title = "A good note from ABC Company";
            if (block.Content == "You are receiving this note because you signed up for Northstar updates. You can update your preferences or unsubscribe at any time.")
                block.Content = "You are receiving this note because you signed up for ABC Company updates. You can update your preferences or unsubscribe at any time.";
            if (block.Content == "Asset | Change | Watch\nNorthstar A | +2.4% | Active\nNorthstar B | +1.1% | Review\nSample only | — | Verify")
                block.Content = TableData("Stocks");
        }
        return true;
    }

    private static string DescriptionFor(string category) => category switch
    {
        "Reports" => "A polished, insight-led update with clear takeaways and example metrics.",
        "Stocks" => "A market-ready briefing layout. Replace the sample figures with verified data.",
        "Tables" => "A clean, scannable format for sharing structured information by email.",
        "Newsletters" => "An editorial-style note with a strong opening, a hero visual, and a clear next step.",
        "Business" => "A thoughtful, professional message for customer and team communication.",
        "Forecasts" => "A planning update with clearly labeled sample projections you can replace.",
        _ => "A flexible, branded email design ready for your content."
    };

    private static (string Title, string Copy, string ChartTitle, string TableTitle, string Button) StoryFor(string category, string name) => category switch
    {
        "Reports" => ("The story behind the numbers",
            $"Here is your {name.ToLowerInvariant()} at a glance. Start with the headline, then use the chart and summary table to show what changed, why it matters, and what your team should do next. Replace every sample metric before sending.",
            "Performance over time", "Key indicators", "Read the full report"),
        "Stocks" => ("Your market briefing",
            $"A concise {name.ToLowerInvariant()} for your next market update. Share a clear point of view, highlight the figures that matter, and link to your source research. All figures in this starter are illustrative—not investment advice—and must be verified before sending.",
            "Market snapshot · sample data", "Watchlist · sample data", "Open the research"),
        "Tables" => ("Everything, at a glance",
            $"Use this {name.ToLowerInvariant()} to make detailed information easy to scan. Keep labels short, bring the most important rows to the top, and add context before the reader reaches the data.",
            "Summary", "Example details", "View all details"),
        "Newsletters" => ("A good note from ABC Company",
            $"Welcome to {name.ToLowerInvariant()}. Share one useful idea, a short story, or an update your readers will be glad to receive. Keep the introduction personal and make the next step easy to find.",
            "This edition", "In this issue", "Explore the story"),
        "Business" => ("A note, made personal",
            $"Use this {name.ToLowerInvariant()} to give your reader the context they need and a clear next step. Add the details that matter to them, keep the tone human, and make it simple to reply.",
            "What happens next", "Important details", "Continue the conversation"),
        "Forecasts" => ("Plan for what comes next",
            $"Share the assumptions behind your {name.ToLowerInvariant()} before presenting the outlook. Mark projected values clearly, explain what could change, and replace the illustrative figures with your own approved forecast.",
            "Actuals and planning scenario", "Planning assumptions", "Review the full outlook"),
        _ => ("A thoughtful update",
            $"A clear, branded email layout for {name.ToLowerInvariant()}. Add the story, details, and next step your audience needs.",
            "At a glance", "Key details", "Learn more")
    };

    private static string ChartData(string category, int index) => category switch
    {
        "Forecasts" => "Jan,42\nFeb,55\nMar,64\nApr,72,forecast\nMay,81,forecast",
        "Stocks" => "Technology,78\nHealthcare,62\nEnergy,45\nFinancials,54",
        _ when index % 3 == 0 => "Week 1,42\nWeek 2,58\nWeek 3,67\nWeek 4,79",
        _ when index % 3 == 1 => "Organic,48\nEmail,27\nSocial,16\nOther,9",
        _ => "Baseline,52\nCurrent,68\nTarget,84"
    };

    private static string TableData(string category) => category switch
    {
        "Stocks" => "Asset | Change | Watch\nExample A | +2.4% | Active\nExample B | +1.1% | Review\nSample only | — | Verify",
        "Forecasts" => "Period | Actual | Outlook\nJanuary | 42 | —\nFebruary | 55 | —\nMarch | — | 64",
        "Reports" => "Indicator | Current | Previous\nEngagement | 68% | 61%\nConversions | 14% | 11%\nTop channel | Email | Search",
        _ => "Item | Status | Owner\nExample one | In progress | Jordan\nExample two | On track | Riley\nNext step | Planned | Team"
    };
}
