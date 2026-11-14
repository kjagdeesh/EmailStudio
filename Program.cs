using EmailStudio.Components;
using EmailStudio.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<Routes>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");
builder.Services.AddSingleton<ITemplateStorage, BrowserTemplateStorage>();
builder.Services.AddSingleton<TemplateStore>();
await builder.Build().RunAsync();
