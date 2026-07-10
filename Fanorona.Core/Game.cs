using System.Text;
using System.Text.RegularExpressions;

namespace Fanorona.Core;

public enum GameOutcome
{
    WhiteWins,
    BlackWins,
    Draw,
}

/// <summary>
/// A full assistant session: move history with undo, repetition detection, outcome, and a
/// human-readable save format that replays from the initial position on load.
/// </summary>
public sealed partial class Game
{
    private readonly List<Position> _positions;
    private readonly List<ulong> _hashes;
    private readonly List<(Turn Turn, bool Lenient)> _moves = [];

    public Game() : this(Position.Initial)
    {
    }

    public Game(Position start)
    {
        _positions = [start];
        _hashes = [Zobrist.Compute(start)];
    }

    /// <summary>The side whose moves the assistant recommends (the human player in AC).</summary>
    public Player EngineSide { get; set; } = Player.White;

    public Position Position => _positions[^1];
    public IReadOnlyList<(Turn Turn, bool Lenient)> Moves => _moves;
    public IReadOnlyList<ulong> HistoryHashes => _hashes;
    public Turn? LastTurn => _moves.Count > 0 ? _moves[^1].Turn : null;

    public void Apply(Turn turn, bool lenient = false)
    {
        _moves.Add((turn, lenient));
        _positions.Add(Position.Apply(turn));
        _hashes.Add(Zobrist.Compute(_positions[^1]));
    }

    /// <summary>Takes back the last half-move. Returns false when at the initial position.</summary>
    public bool Undo()
    {
        if (_moves.Count == 0)
            return false;
        _moves.RemoveAt(_moves.Count - 1);
        _positions.RemoveAt(_positions.Count - 1);
        _hashes.RemoveAt(_hashes.Count - 1);
        return true;
    }

    /// <summary>Null while the game is still in progress.</summary>
    public GameOutcome? Outcome
    {
        get
        {
            var position = Position;
            if (position.White == 0)
                return GameOutcome.BlackWins;
            if (position.Black == 0)
                return GameOutcome.WhiteWins;
            if (_hashes.Count(h => h == _hashes[^1]) >= 3)
                return GameOutcome.Draw;
            if (MoveGenerator.LegalTurns(position).Count == 0)
                return position.ToMove == Player.White ? GameOutcome.BlackWins : GameOutcome.WhiteWins;
            return null;
        }
    }

    public string Serialize()
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Fanorona assistant game");
        sb.AppendLine($"Engine: {EngineSide}");
        for (var i = 0; i < _moves.Count; i++)
        {
            var (turn, lenient) = _moves[i];
            var color = i % 2 == 0 ? 'W' : 'B';
            sb.AppendLine($"{i / 2 + 1}. {color} {Notation.Format(turn)}{(lenient ? "!" : "")}");
        }
        return sb.ToString();
    }

    public static Game Deserialize(string text)
    {
        var game = new Game();
        var sawEngineLine = false;
        var lineNumber = 0;
        foreach (var rawLine in text.ReplaceLineEndings("\n").Split('\n'))
        {
            lineNumber++;
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
                continue;
            if (!sawEngineLine)
            {
                var engineMatch = EngineLine().Match(line);
                if (!engineMatch.Success)
                    throw new FormatException($"line {lineNumber}: expected 'Engine: White' or 'Engine: Black', got '{line}'.");
                game.EngineSide = Enum.Parse<Player>(engineMatch.Groups[1].Value, ignoreCase: true);
                sawEngineLine = true;
                continue;
            }
            var moveMatch = MoveLine().Match(line);
            if (!moveMatch.Success)
                throw new FormatException($"line {lineNumber}: expected a move like '1. W e2-e3A', got '{line}'.");
            var color = moveMatch.Groups[1].Value == "W" ? Player.White : Player.Black;
            if (color != game.Position.ToMove)
                throw new FormatException($"line {lineNumber}: it is {game.Position.ToMove}'s move, but the line says {color}.");
            var candidates = MoveGenerator.LenientTurns(game.Position);
            if (Notation.Parse(moveMatch.Groups[2].Value, candidates) is not ParseResult.Matched(var turn))
                throw new FormatException($"line {lineNumber}: '{moveMatch.Groups[2].Value}' is not a possible move in this position.");
            game.Apply(turn, MoveGenerator.IsLenientOnly(game.Position, turn));
        }
        if (!sawEngineLine)
            throw new FormatException("save file has no 'Engine:' line.");
        return game;
    }

    [GeneratedRegex(@"^Engine:\s*(White|Black)$", RegexOptions.IgnoreCase)]
    private static partial Regex EngineLine();

    [GeneratedRegex(@"^\d+\.\s+([WB])\s+(\S+?)!?$")]
    private static partial Regex MoveLine();
}
