using System.Text;

namespace Fanorona.Core;

public abstract record ParseResult
{
    public sealed record Matched(Turn Turn) : ParseResult;

    public sealed record Ambiguous(IReadOnlyList<Turn> Options) : ParseResult;

    public sealed record NoMatch(string Reason) : ParseResult;
}

/// <summary>
/// Move notation: cells a1..i5, segments separated by optional dashes, each capturing
/// destination optionally suffixed with A (approach) or W (withdrawal). Canonical form
/// always includes dashes and suffixes, e.g. "d2-e3A-f4W"; a paika move is "d2-e3".
/// </summary>
public static class Notation
{
    public static string Format(Turn turn)
    {
        var sb = new StringBuilder(turn.Segments[0].From.ToString());
        foreach (var segment in turn.Segments)
        {
            sb.Append('-').Append(segment.To);
            if (segment.Kind == CaptureKind.Approach)
                sb.Append('A');
            else if (segment.Kind == CaptureKind.Withdrawal)
                sb.Append('W');
        }
        return sb.ToString();
    }

    /// <summary>
    /// Matches user input against a list of candidate turns rather than re-deriving rules:
    /// missing dashes and suffixes are tolerated, and an omitted suffix that leaves several
    /// candidates standing is reported as Ambiguous for the UI to resolve.
    /// </summary>
    public static ParseResult Parse(string input, IReadOnlyList<Turn> candidates)
    {
        var steps = new List<(Cell Cell, CaptureKind? Kind)>();
        var text = input.Trim().ToLowerInvariant();
        var i = 0;
        while (i < text.Length)
        {
            var c = text[i];
            if (c is '-' or ' ' or ',' or '>')
            {
                i++;
                continue;
            }
            // 'a' can start the cell a1..a5 in dashless input like "b2a3"; a real suffix
            // is never followed by a rank digit, so the cell reading wins.
            if (i + 1 < text.Length && text[i + 1] is >= '1' and <= '5'
                && Cell.TryParse(text.AsSpan(i, 2), out var cell))
            {
                steps.Add((cell, null));
                i += 2;
                continue;
            }
            if (c is 'a' or 'w')
            {
                if (steps.Count < 2)
                    return new ParseResult.NoMatch($"'{input}' puts a capture marker on the starting point.");
                if (steps[^1].Kind != null)
                    return new ParseResult.NoMatch($"'{input}' has two capture markers on one point.");
                steps[^1] = (steps[^1].Cell, c == 'a' ? CaptureKind.Approach : CaptureKind.Withdrawal);
                i++;
                continue;
            }
            return new ParseResult.NoMatch($"couldn't read '{input}' — expected points like e2 or a chain like d2-e3A-f4W.");
        }
        if (steps.Count < 2)
            return new ParseResult.NoMatch("a move needs at least two points, e.g. e2-e3.");

        var matches = new List<Turn>();
        foreach (var candidate in candidates)
        {
            if (Matches(candidate, steps))
                matches.Add(candidate);
        }
        // A lenient candidate list contains a paika twin for every capture path. AC
        // captures automatically, so a path that matches a capture never means paika.
        if (matches.Any(m => m.IsCapture))
            matches.RemoveAll(m => !m.IsCapture);
        return matches.Count switch
        {
            0 => new ParseResult.NoMatch($"'{input}' is not a possible move here."),
            1 => new ParseResult.Matched(matches[0]),
            _ => new ParseResult.Ambiguous(matches),
        };
    }

    private static bool Matches(Turn turn, List<(Cell Cell, CaptureKind? Kind)> steps)
    {
        if (turn.Segments.Length != steps.Count - 1 || turn.Origin != steps[0].Cell)
            return false;
        for (var i = 0; i < turn.Segments.Length; i++)
        {
            var (cell, kind) = steps[i + 1];
            if (turn.Segments[i].To != cell || (kind != null && turn.Segments[i].Kind != kind))
                return false;
        }
        return true;
    }
}
