using System.Numerics;

namespace Fanorona.Core;

/// <summary>Deterministic Zobrist hashing (fixed seed, so engine output is reproducible).</summary>
public static class Zobrist
{
    private static readonly ulong[] WhiteKeys = new ulong[Board.Cells];
    private static readonly ulong[] BlackKeys = new ulong[Board.Cells];
    private static readonly ulong BlackToMoveKey;

    static Zobrist()
    {
        var state = 0x_F4_A7_C1_5E_9E_37_79_B9UL;
        for (var cell = 0; cell < Board.Cells; cell++)
        {
            WhiteKeys[cell] = SplitMix64(ref state);
            BlackKeys[cell] = SplitMix64(ref state);
        }
        BlackToMoveKey = SplitMix64(ref state);
    }

    public static ulong Compute(in Position position)
    {
        var hash = position.ToMove == Player.Black ? BlackToMoveKey : 0;
        for (var bits = position.White; bits != 0; bits &= bits - 1)
            hash ^= WhiteKeys[BitOperations.TrailingZeroCount(bits)];
        for (var bits = position.Black; bits != 0; bits &= bits - 1)
            hash ^= BlackKeys[BitOperations.TrailingZeroCount(bits)];
        return hash;
    }

    private static ulong SplitMix64(ref ulong state)
    {
        state += 0x9E3779B97F4A7C15UL;
        var z = state;
        z = (z ^ z >> 30) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ z >> 27) * 0x94D049BB133111EBUL;
        return z ^ z >> 31;
    }
}
