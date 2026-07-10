using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.JSInterop;
using Fanorona.Web;
using Fanorona.Web.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddSingleton<GameSession>();
builder.Services.AddSingleton<EngineService>();
builder.Services.AddSingleton(sp => new SaveStore((IJSInProcessRuntime)sp.GetRequiredService<IJSRuntime>()));

await builder.Build().RunAsync();
