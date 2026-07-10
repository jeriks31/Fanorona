using Microsoft.JSInterop;

namespace Fanorona.Web.Services;

/// <summary>
/// Persists the game in localStorage using the same text format as the console app's save
/// files. WASM JS interop is in-process, so the calls are synchronous.
/// </summary>
public sealed class SaveStore(IJSInProcessRuntime js)
{
    private const string Key = "fanorona.save";

    public string? Load() => js.Invoke<string?>("localStorage.getItem", Key);

    public void Save(string text) => js.InvokeVoid("localStorage.setItem", Key, text);
}
