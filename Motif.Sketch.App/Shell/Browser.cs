using Microsoft.JSInterop;

namespace Motif.Sketch.App;

/// <summary>The Web App's own browser plumbing (wwwroot/js/app.js). Synchronous where WebAssembly allows it.</summary>
public sealed class Browser(IJSRuntime js)
{
    private IJSInProcessObjectReference? module;

    public async Task Load() =>
        module ??= await js.InvokeAsync<IJSInProcessObjectReference>("import", "./js/app.js");

    private IJSInProcessObjectReference M => module ?? throw new InvalidOperationException("app.js isn't loaded yet.");

    public string Fragment() => M.Invoke<string>("fragment");
    public string PageUrl() => M.Invoke<string>("pageUrl");
    public void ClearFragment() => M.InvokeVoid("clearFragment");
    public void SetUnsaved(bool unsaved) => M.InvokeVoid("setUnsaved", unsaved);
    public void ListenForKeys<T>(DotNetObjectReference<T> target) where T : class => M.InvokeVoid("listenForKeys", target);

    public void ListenForWheel<T>(object element, DotNetObjectReference<T> target) where T : class =>
        M.InvokeVoid("listenForWheel", element, target);

    public void StopListening(object element) => M.InvokeVoid("stopListening", element);

    /// <summary>[left, top, width, height] of an element, in client pixels.</summary>
    public double[] Rect(object element) => M.Invoke<double[]>("rect", element);

    public void Capture(object element, long pointerId) => M.InvokeVoid("capture", element, pointerId);
    public string? HitAt(double clientX, double clientY) => M.Invoke<string?>("hitAt", clientX, clientY);
    public void Focus(string selector) => M.InvokeVoid("focus", selector);
    public string Theme() => M.Invoke<string>("theme");
    public void SetTheme(string theme) => M.InvokeVoid("setTheme", theme);
    public string? Preference(string key) => M.Invoke<string?>("preference", key);
    public void SetPreference(string key, string value) => M.InvokeVoid("setPreference", key, value);
    public bool[] WatchViewport<T>(DotNetObjectReference<T> target) where T : class => M.Invoke<bool[]>("watchViewport", target);
}
