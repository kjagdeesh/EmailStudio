namespace EmailStudio.Services;

public interface ITemplateStorage
{
    Task<string?> ReadAsync();
    Task WriteAsync(string? expected, string value);
}
