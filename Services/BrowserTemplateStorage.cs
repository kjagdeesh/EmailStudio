using Microsoft.JSInterop;

namespace EmailStudio.Services;

public sealed class BrowserTemplateStorage(IJSRuntime js) : ITemplateStorage
{
    public async Task<string?> ReadAsync() => await js.InvokeAsync<string?>("emailStudioStorage.read");

    public async Task WriteAsync(string? expected, string value) =>
        await js.InvokeVoidAsync("emailStudioStorage.write", expected, value);
}
