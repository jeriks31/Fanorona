using Fanorona.Core;

namespace Fanorona.Web;

/// <summary>
/// Click-based move entry: the user clicks the piece that moved, then each point it moved to,
/// and the candidate turns (from <see cref="MoveGenerator.LenientTurns"/>) are filtered by that
/// prefix. When a clicked step could capture by both approach and withdrawal, <see cref="Pending"/>
/// holds the two variants until <see cref="ChooseKind"/> resolves it. <see cref="Completed"/> is
/// set once the prefix identifies exactly one turn.
/// </summary>
public sealed class MoveInput
{
    private List<Turn> _all = [];
    private readonly List<Segment> _prefix = [];

    public Cell? Origin { get; private set; }
    public (Segment Approach, Segment Withdrawal)? Pending { get; private set; }
    public Turn? Completed { get; private set; }

    public IReadOnlyList<Segment> Path => _prefix;

    public void Reset(Position position)
    {
        _all = MoveGenerator.LenientTurns(position);
        _prefix.Clear();
        Origin = null;
        Pending = null;
        Completed = null;
    }

    private Cell CurrentCell => _prefix.Count > 0 ? _prefix[^1].To : Origin!.Value;

    private IEnumerable<Turn> Candidates =>
        _all.Where(t => t.Origin == Origin!.Value && MatchesPrefix(t));

    private bool MatchesPrefix(Turn turn)
    {
        if (turn.Segments.Length < _prefix.Count)
            return false;
        for (var i = 0; i < _prefix.Count; i++)
        {
            if (turn.Segments[i] != _prefix[i])
                return false;
        }
        return true;
    }

    /// <summary>True mid-chain when the AC opponent could legally have stopped on the current cell.</summary>
    public bool CanStopHere =>
        Origin is not null && Pending is null && Completed is null && _prefix.Count > 0 &&
        Candidates.Any(t => t.Segments.Length == _prefix.Count);

    /// <summary>Cells worth highlighting: origins while idle, continuations + the current cell while entering.</summary>
    public IReadOnlyCollection<Cell> ClickableCells
    {
        get
        {
            if (Completed is not null || Pending is not null)
                return [];
            if (Origin is null)
                return _all.Select(t => t.Origin).Distinct().ToList();
            var k = _prefix.Count;
            var cells = Candidates.Where(t => t.Segments.Length > k)
                .Select(t => t.Segments[k].To).Distinct().ToList();
            cells.Add(CurrentCell); // stop-here mid-chain, deselect at the origin
            return cells;
        }
    }

    /// <summary>The partially entered move in notation, e.g. "d2-e3A-".</summary>
    public string PathNotation
    {
        get
        {
            if (Origin is not { } origin)
                return "";
            var text = origin.ToString();
            foreach (var segment in _prefix)
            {
                text += $"-{segment.To}" + segment.Kind switch
                {
                    CaptureKind.Approach => "A",
                    CaptureKind.Withdrawal => "W",
                    _ => "",
                };
            }
            return text;
        }
    }

    public ulong CapturedPreview
    {
        get
        {
            var mask = 0UL;
            foreach (var segment in _prefix)
                mask |= segment.Captured;
            return mask;
        }
    }

    public void Click(Cell cell)
    {
        if (Completed is not null || Pending is not null)
            return;
        if (Origin is null)
        {
            if (_all.Any(t => t.Origin == cell))
                Origin = cell;
            return;
        }
        var k = _prefix.Count;
        var hits = Candidates.Where(t => t.Segments.Length > k && t.Segments[k].To == cell).ToList();
        if (hits.Count == 0)
        {
            if (cell == CurrentCell)
            {
                if (k == 0)
                    Origin = null;
                else if (CanStopHere)
                    Finish();
            }
            else if (k == 0 && _all.Any(t => t.Origin == cell))
            {
                Origin = cell; // switch to another piece before any step is committed
            }
            return;
        }
        // A lenient candidate list contains a paika twin for every capture path. AC captures
        // automatically, so a click that matches a capture never means paika.
        if (hits.Any(t => t.Segments[k].Kind != CaptureKind.None))
            hits.RemoveAll(t => t.Segments[k].Kind == CaptureKind.None);
        var variants = hits.Select(t => t.Segments[k]).Distinct().ToList();
        if (variants.Count == 1)
            Commit(variants[0]);
        else
            Pending = (variants.Single(s => s.Kind == CaptureKind.Approach),
                variants.Single(s => s.Kind == CaptureKind.Withdrawal));
    }

    public void ChooseKind(CaptureKind kind)
    {
        var (approach, withdrawal) = Pending!.Value;
        Pending = null;
        Commit(kind == CaptureKind.Approach ? approach : withdrawal);
    }

    public void CancelPending() => Pending = null;

    /// <summary>Ends the chain on the current cell; only valid while <see cref="CanStopHere"/>.</summary>
    public void StopHere() => Finish();

    /// <summary>Steps one click back: pending choice, last segment, or the origin selection.</summary>
    public void UndoStep()
    {
        if (Completed is not null)
            return;
        if (Pending is not null)
            Pending = null;
        else if (_prefix.Count > 0)
            _prefix.RemoveAt(_prefix.Count - 1);
        else
            Origin = null;
    }

    private void Commit(Segment segment)
    {
        _prefix.Add(segment);
        // A paika is always a single step; a chain ends itself when nothing can extend it.
        if (segment.Kind == CaptureKind.None || !Candidates.Any(t => t.Segments.Length > _prefix.Count))
            Finish();
    }

    private void Finish() =>
        Completed = Candidates.Single(t => t.Segments.Length == _prefix.Count);
}
