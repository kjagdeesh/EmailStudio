using System.Globalization;
using System.Net;
using System.Text;
using EmailStudio.Models;

namespace EmailStudio.Services;

public static class EmailRenderer
{
    private static string E(string? value) => WebUtility.HtmlEncode(value ?? "");
    private static string Lines(string? value) => E(value).Replace("\r\n", "\n").Replace("\n", "<br>");
    public static string Render(EmailTemplate t)
    {
        TemplateStore.Validate(t);
        var html = new StringBuilder();
        html.Append($"""<!doctype html><html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>{E(t.Name)}</title></head><body style="margin:0;padding:24px 12px;background:{t.BackgroundColor};font-family:Arial,sans-serif;color:{t.TextColor}"><table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="border-collapse:collapse"><tr><td align="center"><table role="presentation" width="600" cellspacing="0" cellpadding="0" style="width:100%;max-width:600px;background:{t.SurfaceColor};border-collapse:collapse">""");
        foreach (var b in t.Blocks)
        {
            html.Append($"""<tr><td style="padding:24px 32px;background:{b.BackgroundColor};color:{b.TextColor};font-size:15px;line-height:1.6">""");
            switch (b.Type)
            {
                case "header":
                    html.Append($"""<table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="border-collapse:collapse;margin-bottom:28px"><tr><td valign="middle">""");
                    if (!string.IsNullOrWhiteSpace(t.BrandLogoUrl))
                        html.Append($"""<img src="{E(t.BrandLogoUrl)}" alt="{E(t.BrandName)}" width="40" height="40" style="display:inline-block;width:40px;height:40px;vertical-align:middle;border-radius:10px;margin-right:10px">""");
                    else
                        html.Append($"""<span role="img" aria-label="{E(t.BrandName)} logo" style="display:inline-block;width:40px;height:40px;line-height:40px;text-align:center;vertical-align:middle;border-radius:11px;background:{b.AccentColor};color:{TemplateStore.ContrastText(b.AccentColor)};font-size:13px;font-weight:bold;letter-spacing:-.4px;margin-right:10px">{E(BrandInitials(t.BrandName))}</span>""");
                    if (!string.IsNullOrWhiteSpace(t.BrandWebsiteUrl))
                        html.Append($"""<a href="{E(t.BrandWebsiteUrl)}" style="vertical-align:middle;color:{b.TextColor};font-size:14px;font-weight:bold;text-decoration:none">{E(t.BrandName)}</a>""");
                    else
                        html.Append($"""<span style="vertical-align:middle;color:{b.TextColor};font-size:14px;font-weight:bold">{E(t.BrandName)}</span>""");
                    html.Append("</td><td align=\"right\" style=\"font-size:11px;letter-spacing:1px;color:" + b.AccentColor + "\">" + Lines(b.Content) + "</td></tr></table>");
                    html.Append($"""<h1 style="font-size:34px;line-height:1.15;letter-spacing:-.7px;margin:0;color:{b.TextColor}">{E(b.Title)}</h1>""");
                    break;
                case "text":
                    if (b.Title.Length > 0) html.Append($"""<h2 style="font-size:21px;line-height:1.3;margin:0 0 12px;color:{b.TextColor}">{E(b.Title)}</h2>""");
                    html.Append($"<div>{Lines(b.Content)}</div>");
                    break;
                case "button":
                    if (!string.IsNullOrWhiteSpace(b.Url))
                        html.Append($"""<a href="{E(b.Url)}" style="display:inline-block;background:{b.AccentColor};color:{b.TextColor};padding:12px 22px;text-decoration:none;font-weight:bold;border-radius:4px">{E(b.Title)}</a>""");
                    else
                        html.Append($"""<span style="display:inline-block;background:{b.AccentColor};color:{b.TextColor};padding:12px 22px;font-weight:bold;border-radius:4px">{E(b.Title)}</span>""");
                    break;
                case "image":
                    if (!string.IsNullOrWhiteSpace(b.Url))
                        html.Append($"""<img src="{E(b.Url)}" alt="{E(b.Title)}" width="536" style="display:block;width:100%;max-width:536px;height:auto;border-radius:16px">""");
                    else
                        RenderImagePlaceholder(html, b, t.BrandName);
                    break;
                case "divider": html.Append($"""<hr style="border:0;border-top:1px solid {b.AccentColor};margin:0">"""); break;
                case "footer":
                    html.Append($"""<div style="padding:20px 0 15px;border-top:1px solid {b.AccentColor}"><span style="font-size:13px">{E(b.Title)}</span><br><strong style="display:inline-block;margin-top:5px;font-size:16px">{E(t.BrandSignature)}</strong></div>""");
                    if (!string.IsNullOrWhiteSpace(t.BrandWebsiteUrl))
                        html.Append($"""<div style="font-size:12px;margin:0 0 12px"><a href="{E(t.BrandWebsiteUrl)}" style="color:{b.AccentColor}">Visit {E(t.BrandName)}</a></div>""");
                    html.Append($"""<div style="font-size:12px;line-height:1.6;color:{b.TextColor};opacity:.72">{Lines(b.Content)}</div>""");
                    break;
                case "table": RenderTable(html, b); break;
                case "bar chart":
                case "comparison chart":
                case "forecast chart":
                case "line chart":
                case "area chart":
                case "pie chart":
                case "doughnut chart": RenderChart(html, b); break;
            }

            html.Append("</td></tr>");
        }
        return html.Append("</table></td></tr></table></body></html>").ToString();
    }

    private static string BrandInitials(string brandName)
    {
        var words = brandName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (brandName == "ABC Company") return "ABC";
        return words.Length > 1
            ? string.Concat(words.Take(2).Select(word => char.ToUpperInvariant(word[0])))
            : brandName[..Math.Min(2, brandName.Length)].ToUpperInvariant();
    }

    private static void RenderImagePlaceholder(StringBuilder html, EmailBlock block, string brandName)
    {
        var placeholder = Mix(block.AccentColor, "#ffffff", .12);
        var ink = "#182033";
        html.Append($"""
            <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="border-collapse:collapse">
            <tr><td bgcolor="{placeholder}" height="205" valign="middle" style="height:205px;padding:28px 32px;background:{placeholder};border-radius:16px">
            <div style="font-size:10px;font-weight:bold;letter-spacing:2px;color:{block.AccentColor}">{E(brandName)} &nbsp; / &nbsp; IMAGE PREVIEW</div>
            <div style="font-size:23px;line-height:1.25;font-weight:bold;color:{ink};margin-top:14px">{E(block.Title)}</div>
            <div style="font-size:12px;line-height:1.5;color:{ink};margin-top:10px">Add an image URL to replace this branded placeholder.</div>
            </td></tr></table>
            """);
    }

    private static void RenderTable(StringBuilder html, EmailBlock b)
    {
        html.Append($"""<h2 style="font-size:19px;margin:0 0 14px;color:{b.TextColor}">{E(b.Title)}</h2><table width="100%" cellspacing="0" cellpadding="0" style="border-collapse:separate;border-spacing:0;border:1px solid {Mix(b.AccentColor, b.BackgroundColor, .25)};border-radius:12px;font-size:13px;overflow:hidden">""");
        var rows = b.Content.Replace("\r\n", "\n").Split('\n', StringSplitOptions.RemoveEmptyEntries).Take(100).ToArray();
        for (var i = 0; i < rows.Length; i++)
        {
            html.Append("<tr>");
            var background = b.TableStyle switch
            {
                "accent" when i == 0 => b.AccentColor,
                "striped" when i % 2 == 1 => Mix(b.AccentColor, b.BackgroundColor, .09),
                "classic" when i == 0 => Mix(b.AccentColor, b.BackgroundColor, .12),
                _ => b.BackgroundColor
            };
            var text = b.TableStyle == "accent" && i == 0 ? TemplateStore.ContrastText(b.AccentColor) : b.TextColor;
            foreach (var cell in rows[i].Split('|').Take(12))
                html.Append($"""<td style="padding:11px 12px;background:{background};color:{text};border-bottom:1px solid {Mix(b.AccentColor, b.BackgroundColor, .18)};{(i == 0 ? "font-weight:bold;" : "")}">{E(cell.Trim())}</td>""");
            html.Append("</tr>");
        }
        html.Append("</table>");
    }

    private static void RenderChart(StringBuilder html, EmailBlock b)
    {
        var rows = b.Content.Replace("\r\n", "\n").Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Split(','))
            .Where(parts => parts.Length >= 2 && double.TryParse(parts[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var v) && double.IsFinite(v) && v >= 0)
            .Take(50).Select(parts => (Label: parts[0].Trim(), Value: double.Parse(parts[1].Trim(), CultureInfo.InvariantCulture), Forecast: parts.Length > 2 && parts[2].Trim().Equals("forecast", StringComparison.OrdinalIgnoreCase))).ToList();
        var max = rows.Count == 0 ? 0 : rows.Max(r => r.Value);
        html.Append($"""<h2 style="font-size:19px;margin:0 0 14px">{E(b.Title)}</h2>""");
        if (rows.Count > 0 && b.Type is "line chart" or "area chart")
            RenderLineVisual(html, b, rows.Select(r => r.Value).ToList(), max);
        else if (rows.Count > 0 && b.Type is "pie chart" or "doughnut chart")
            RenderPieVisual(html, b, rows.Select(r => r.Value).ToList(), max);
        html.Append("""<table width="100%" cellspacing="0" cellpadding="4" style="font-size:13px;border-collapse:collapse">""");
        for (var index = 0; index < rows.Count; index++)
        {
            var row = rows[index];
            var width = max > 0 ? Math.Clamp((int)Math.Round(row.Value / max * 100), 0, 100) : 0;
            var forecast = b.Type == "forecast chart" && row.Forecast;
            html.Append($"""<tr><td width="25%">{E(row.Label)}{(forecast ? " (forecast)" : "")}</td>""");
            if (b.Type is "bar chart" or "comparison chart" or "forecast chart")
                html.Append($"""<td width="60%"><table role="presentation" width="100%" cellspacing="0" cellpadding="0"><tr><td width="{width}%" style="height:12px;background:{SliceColor(b.AccentColor, forecast ? 2 : index)};border-radius:6px"></td><td></td></tr></table></td>""");
            else if (b.Type is "pie chart" or "doughnut chart")
                html.Append($"""<td><span style="display:inline-block;width:12px;height:12px;background:{SliceColor(b.AccentColor, index)}"></span></td>""");
            html.Append($"""<td align="right">{row.Value.ToString("0.##", CultureInfo.InvariantCulture)}</td></tr>""");
        }
        if (rows.Count == 0) html.Append("<tr><td>Add chart data as label,value, one row per line.</td></tr>");
        html.Append("</table>");
    }

    private static string F(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);

    private static void RenderLineVisual(StringBuilder html, EmailBlock b, List<double> values, double max)
    {
        var points = values.Select((value, i) => (X: values.Count == 1 ? 250.0 : 20.0 + 460.0 * i / (values.Count - 1), Y: 150.0 - (max > 0 ? value / max * 130.0 : 0))).ToList();
        var coordinates = string.Join(" ", points.Select(p => $"{F(p.X)},{F(p.Y)}"));
        html.Append($"""<svg xmlns="http://www.w3.org/2000/svg" role="img" aria-label="{E(b.Title)}" viewBox="0 0 500 170" width="500" style="display:block;width:100%;max-width:500px;height:auto">""");
        html.Append($"""<line x1="20" y1="150" x2="480" y2="150" stroke="{b.TextColor}" stroke-opacity=".25"/>""");
        if (b.Type == "area chart" && points.Count > 1)
            html.Append($"""<polygon points="20,150 {coordinates} 480,150" fill="{b.AccentColor}" fill-opacity=".25"/>""");
        if (points.Count > 1)
            html.Append($"""<polyline points="{coordinates}" fill="none" stroke="{b.AccentColor}" stroke-width="3"/>""");
        foreach (var point in points)
            html.Append($"""<circle cx="{F(point.X)}" cy="{F(point.Y)}" r="4" fill="{b.AccentColor}"/>""");
        html.Append("</svg>");
    }

    private static void RenderPieVisual(StringBuilder html, EmailBlock b, List<double> values, double max)
    {
        html.Append($"""<svg xmlns="http://www.w3.org/2000/svg" role="img" aria-label="{E(b.Title)}" viewBox="0 0 120 120" width="180" height="180" style="display:block;max-width:100%;height:auto;margin:0 auto">""");
        var total = max > 0 ? values.Sum(v => v / max) : 0;
        if (total == 0) html.Append($"""<circle cx="60" cy="60" r="50" fill="{b.BackgroundColor}" stroke="{b.AccentColor}"/>""");
        var angle = -Math.PI / 2;
        for (var i = 0; i < values.Count && total > 0; i++)
        {
            var fraction = (values[i] / max) / total;
            if (fraction <= 0) continue;
            var end = angle + fraction * Math.PI * 2;
            var x1 = 60 + 50 * Math.Cos(angle); var y1 = 60 + 50 * Math.Sin(angle);
            var x2 = 60 + 50 * Math.Cos(end); var y2 = 60 + 50 * Math.Sin(end);
            var color = SliceColor(b.AccentColor, i);
            if (fraction >= 0.999999999)
                html.Append($"""<circle cx="60" cy="60" r="50" fill="{color}"/>""");
            else
                html.Append($"""<path d="M 60 60 L {F(x1)} {F(y1)} A 50 50 0 {(fraction > .5 ? 1 : 0)} 1 {F(x2)} {F(y2)} Z" fill="{color}"/>""");
            angle = end;
        }
        if (b.Type == "doughnut chart")
            html.Append($"""<circle cx="60" cy="60" r="27" fill="{b.BackgroundColor}"/>""");
        html.Append("</svg>");
    }

    private static string SliceColor(string accent, int index)
    {
        var factor = new[] { 1.0, .78, .60, .43, .28, .15 }[index % 6];
        var r = Convert.ToInt32(accent.Substring(1, 2), 16);
        var g = Convert.ToInt32(accent.Substring(3, 2), 16);
        var b = Convert.ToInt32(accent.Substring(5, 2), 16);
        return $"#{(int)(255 - (255 - r) * factor):x2}{(int)(255 - (255 - g) * factor):x2}{(int)(255 - (255 - b) * factor):x2}";
    }

    private static string Mix(string foreground, string background, double amount)
    {
        static int Channel(string color, int offset) => Convert.ToInt32(color.Substring(offset, 2), 16);
        int Blend(int offset) => (int)Math.Round(Channel(foreground, offset) * amount + Channel(background, offset) * (1 - amount));
        return $"#{Blend(1):x2}{Blend(3):x2}{Blend(5):x2}";
    }
}
