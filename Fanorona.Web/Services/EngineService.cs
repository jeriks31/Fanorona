using System.Diagnostics;
using Fanorona.Core;

namespace Fanorona.Web.Services;

/// <summary>
/// Runs the engine on the single-threaded WASM UI thread without freezing the page: one
/// <see cref="Engine.FindBestTurn"/> call per depth, yielding to the browser between calls.
/// The engine's transposition table persists across calls, so each call replays the shallower
/// depths almost for free and only the newest depth costs real time.
/// </summary>
public sealed class EngineService
{
    private readonly Engine _engine = new();
    private CancellationTokenSource? _cts;

    /// <summary>
    /// Reports each completed depth through <paramref name="onDepthCompleted"/> and returns the
    /// final result, or null when the search was cancelled (the last reported result stands).
    /// </summary>
    public async Task<SearchResult?> AnalyzeAsync(Position position, TimeSpan budget,
        IReadOnlyList<ulong> historyHashes, Action<SearchResult> onDepthCompleted)
    {
        _cts?.Cancel();
        var cts = _cts = new CancellationTokenSource();
        var clock = Stopwatch.StartNew();
        SearchResult? result = null;
        for (var depth = 1; depth <= 64; depth++)
        {
            var remaining = budget - clock.Elapsed;
            if (result is not null && remaining <= TimeSpan.Zero)
                break;
            var iteration = _engine.FindBestTurn(position, remaining, historyHashes, cts.Token, maxDepth: depth);
            if (cts.IsCancellationRequested)
                return null;
            result = iteration;
            onDepthCompleted(iteration);
            if (iteration.DepthReached < depth)
                break; // the budget ran out mid-iteration
            if (Math.Abs(iteration.Score) > 900_000)
                break; // forced result; deeper search cannot improve it
            await Task.Delay(1); // yield so the browser repaints between depths
            if (cts.IsCancellationRequested)
                return null;
        }
        return result;
    }

    public void Cancel() => _cts?.Cancel();
}
