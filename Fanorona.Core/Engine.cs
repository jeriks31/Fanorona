using System.Diagnostics;

namespace Fanorona.Core;

public sealed record SearchResult(Turn BestTurn, int Score, int DepthReached, long Nodes, TimeSpan Elapsed);

/// <summary>
/// Iterative-deepening negamax with alpha-beta pruning and a transposition table.
/// Depth is counted in full turns (a whole capture chain is one ply). Deterministic:
/// same position and reached depth always yield the same move.
/// </summary>
public sealed class Engine
{
    private const int Infinity = int.MaxValue / 2;
    private const int MateThreshold = Evaluation.Win - 4096;
    private const int MaxDepth = 64;
    private const int TTSizeBits = 20;
    private const byte FlagExact = 0, FlagLower = 1, FlagUpper = 2;

    private struct TTEntry
    {
        public ulong Key;
        public short Depth;
        public byte Flag;
        public int Score;
        public Turn? Best;
    }

    private readonly TTEntry[] _table = new TTEntry[1 << TTSizeBits];
    private readonly ulong[] _path = new ulong[MaxDepth + 8];
    private readonly Stopwatch _clock = new();
    private HashSet<ulong> _history = [];
    private TimeSpan _budget;
    private CancellationToken _ct;
    private bool _abortAllowed;
    private long _nodes;

    private sealed class SearchAbortedException : Exception;

    /// <summary>
    /// Finds the strongest turn for the side to move. The position must have at least one
    /// legal turn (the game is not over). <paramref name="historyHashes"/> are the Zobrist
    /// hashes of all positions played so far, used to score repetitions as draws.
    /// </summary>
    public SearchResult FindBestTurn(Position position, TimeSpan budget,
        IReadOnlyList<ulong> historyHashes, CancellationToken ct = default, int maxDepth = MaxDepth)
    {
        _history = [.. historyHashes];
        _budget = budget;
        _ct = ct;
        _nodes = 0;
        _clock.Restart();
        _path[0] = Zobrist.Compute(position);

        var rootMoves = MoveGenerator.LegalTurns(position);
        Debug.Assert(rootMoves.Count > 0);
        var scored = rootMoves.Select(t => (Turn: t, Score: t.CapturedCount)).ToList();

        Turn best = scored[0].Turn;
        var bestScore = 0;
        var depthReached = 0;
        for (var depth = 1; depth <= maxDepth; depth++)
        {
            _abortAllowed = depth > 1;
            // Previous iteration's scores order this one: best move first tightens alpha fast.
            scored.Sort((a, b) => b.Score.CompareTo(a.Score));
            try
            {
                var alpha = -Infinity;
                for (var i = 0; i < scored.Count; i++)
                {
                    var child = position.Apply(scored[i].Turn);
                    var score = -Negamax(child, depth - 1, -Infinity, -alpha, 1);
                    scored[i] = (scored[i].Turn, score);
                    if (score > alpha)
                        alpha = score;
                }
            }
            catch (SearchAbortedException)
            {
                break;
            }
            var top = scored.MaxBy(s => s.Score);
            (best, bestScore, depthReached) = (top.Turn, top.Score, depth);
            if (bestScore > MateThreshold || bestScore < -MateThreshold)
                break; // forced result found; deeper search cannot improve it
            if (_clock.Elapsed > budget * 0.4)
                break; // the next iteration would not finish anyway
        }
        return new SearchResult(best, bestScore, depthReached, _nodes, _clock.Elapsed);
    }

    private int Negamax(Position position, int depth, int alpha, int beta, int ply)
    {
        if ((++_nodes & 4095) == 0 && _abortAllowed
            && (_clock.Elapsed > _budget || _ct.IsCancellationRequested))
            throw new SearchAbortedException();

        if (position.Pieces(position.ToMove) == 0)
            return -(Evaluation.Win - ply);

        var hash = Zobrist.Compute(position);
        if (IsRepetition(hash, ply))
            return 0;
        if (depth == 0)
            return Evaluation.Evaluate(position);

        ref var entry = ref _table[hash & ((1UL << TTSizeBits) - 1)];
        Turn? ttMove = null;
        if (entry.Key == hash)
        {
            ttMove = entry.Best;
            if (entry.Depth >= depth)
            {
                var ttScore = ScoreFromTT(entry.Score, ply);
                switch (entry.Flag)
                {
                    case FlagExact:
                        return ttScore;
                    case FlagLower when ttScore > alpha:
                        alpha = ttScore;
                        break;
                    case FlagUpper when ttScore < beta:
                        beta = ttScore;
                        break;
                }
                if (alpha >= beta)
                    return ttScore;
            }
        }

        var moves = MoveGenerator.LegalTurns(position);
        if (moves.Count == 0)
            return -(Evaluation.Win - ply); // no legal turn loses

        moves.Sort((a, b) => OrderKey(b, ttMove).CompareTo(OrderKey(a, ttMove)));

        _path[ply] = hash;
        var best = -Infinity;
        Turn? bestMove = null;
        var flag = FlagUpper;
        foreach (var move in moves)
        {
            var score = -Negamax(position.Apply(move), depth - 1, -beta, -alpha, ply + 1);
            if (score > best)
            {
                best = score;
                bestMove = move;
            }
            if (best > alpha)
            {
                alpha = best;
                flag = FlagExact;
            }
            if (alpha >= beta)
            {
                flag = FlagLower;
                break;
            }
        }

        entry = new TTEntry
        {
            Key = hash,
            Depth = (short)depth,
            Flag = flag,
            Score = ScoreToTT(best, ply),
            Best = bestMove,
        };
        return best;
    }

    private bool IsRepetition(ulong hash, int ply)
    {
        for (var i = 0; i < ply; i++)
        {
            if (_path[i] == hash)
                return true;
        }
        return _history.Contains(hash);
    }

    private static int OrderKey(Turn turn, Turn? ttMove) =>
        turn.Equals(ttMove) ? int.MaxValue : turn.CapturedCount;

    // Mate scores are stored relative to the storing node so they stay correct when the
    // entry is reused at a different ply.
    private static int ScoreToTT(int score, int ply) =>
        score > MateThreshold ? score + ply : score < -MateThreshold ? score - ply : score;

    private static int ScoreFromTT(int score, int ply) =>
        score > MateThreshold ? score - ply : score < -MateThreshold ? score + ply : score;
}
