using Fanorona.Core;
using Spectre.Console;

namespace Fanorona.ConsoleApp;

public sealed class AssistantApp
{
    private const string DefaultSavePath = "fanorona.save";

    private enum CommandResult
    {
        NotACommand,
        Stay,     // handled; keep prompting at the same board
        Refresh,  // handled and state changed; redraw from the main loop
    }

    private readonly Engine _engine = new();
    private Game _game = new();
    private TimeSpan _thinkTime = TimeSpan.FromSeconds(1.5);
    private bool _flipped;
    private bool _quit;

    public void Run()
    {
        AnsiConsole.Write(new Rule("[bold yellow]Fanorona Assistant — Assassin's Creed[/]"));
        AnsiConsole.MarkupLine("[grey]Relay the AC opponent's moves here and play the recommended replies in the game.[/]");
        AnsiConsole.MarkupLine("[grey]Type [/][bold]help[/][grey] at any prompt for commands.[/]");
        AnsiConsole.WriteLine();
        StartNewOrLoad();
        while (!_quit)
        {
            RenderBoard();
            if (_game.Outcome is { } outcome)
            {
                AnnounceOutcome(outcome);
                if (!AnsiConsole.Confirm("Start a new game?"))
                    return;
                _game = new Game();
                ChooseSide();
                continue;
            }
            if (_game.Position.ToMove == _game.EngineSide)
                EngineTurn();
            else
                OpponentTurn();
        }
    }

    private void StartNewOrLoad()
    {
        var choice = AnsiConsole.Prompt(new SelectionPrompt<string>()
            .Title("Start")
            .AddChoices("New game", "Load saved game"));
        if (choice == "Load saved game")
        {
            var path = AnsiConsole.Prompt(new TextPrompt<string>("Save file path:")
                .DefaultValue(DefaultSavePath));
            if (TryLoad(path))
                return;
            AnsiConsole.MarkupLine("[grey]Starting a new game instead.[/]");
        }
        ChooseSide();
    }

    private void ChooseSide()
    {
        var side = AnsiConsole.Prompt(new SelectionPrompt<string>()
            .Title("Which color do [bold]you[/] play in Assassin's Creed? (White moves first)")
            .AddChoices("White", "Black"));
        _game.EngineSide = side == "White" ? Player.White : Player.Black;
    }

    private void RenderBoard()
    {
        const string white = "[bold white]●[/]";
        const string black = "[bold orange1]●[/]";
        AnsiConsole.WriteLine();
        AnsiConsole.Markup(BoardRenderer.Render(_game.Position, _game.LastTurn, _flipped));
        var (you, opponent) = _game.EngineSide == Player.White
            ? (white + " White", black + " Black")
            : (black + " Black", white + " White");
        AnsiConsole.MarkupLine(
            $"[grey]you:[/] {you}   [grey]AC opponent:[/] {opponent}   " +
            $"[grey]pieces[/] {_game.Position.PieceCount(Player.White)}{white} [grey]/[/] " +
            $"{_game.Position.PieceCount(Player.Black)}{black}   " +
            $"[grey]move {_game.Moves.Count / 2 + 1}, {_game.Position.ToMove} to play[/]");
        if (_game.LastTurn is { } last)
        {
            // The piece that just moved belongs to the side that is no longer to move.
            var landed = _game.Position.ToMove == Player.Black
                ? "[bold white on green]●[/]"
                : "[bold orange1 on green]●[/]";
            AnsiConsole.MarkupLine(
                $"[grey]last move ({Notation.Format(last)}):[/] {landed} [grey]landed here[/]   " +
                "[bold green]·[/] [grey]its path[/]   [bold red]x[/] [grey]just captured[/]");
        }
        AnsiConsole.WriteLine();
    }

    private void EngineTurn()
    {
        var result = AnsiConsole.Status().Start($"Thinking ({_thinkTime.TotalSeconds:0.#}s)...",
            _ => _engine.FindBestTurn(_game.Position, _thinkTime, _game.HistoryHashes));
        AnsiConsole.MarkupLine(
            $"Play in AC: [bold green]{Notation.Format(result.BestTurn)}[/]  " +
            $"[grey](depth {result.DepthReached}, {DescribeScore(result.Score)}, {result.Nodes:N0} nodes)[/]");
        foreach (var step in MoveDescriber.DescribeSteps(result.BestTurn))
            AnsiConsole.MarkupLine($"  [green]›[/] {step}");
        while (!_quit)
        {
            var input = Prompt("Press Enter once played — or type the move you played instead");
            if (input.Length == 0)
            {
                _game.Apply(result.BestTurn);
                return;
            }
            switch (HandleCommand(input))
            {
                case CommandResult.Stay:
                    continue;
                case CommandResult.Refresh:
                    return;
            }
            if (TryApplyMove(input, opponentMove: false))
                return;
        }
    }

    private void OpponentTurn()
    {
        while (!_quit)
        {
            var input = Prompt($"Enter the opponent's ({_game.EngineSide.Opponent()}) move " +
                "[grey]— e.g. b4-c3-b2[/]");
            if (input.Length == 0)
                continue;
            switch (HandleCommand(input))
            {
                case CommandResult.Stay:
                    continue;
                case CommandResult.Refresh:
                    return;
            }
            if (TryApplyMove(input, opponentMove: true))
                return;
        }
    }

    private bool TryApplyMove(string input, bool opponentMove)
    {
        var candidates = MoveGenerator.LenientTurns(_game.Position);
        Turn turn;
        switch (Notation.Parse(input, candidates))
        {
            case ParseResult.NoMatch(var reason):
                AnsiConsole.MarkupLine($"[red]{Markup.Escape(reason)}[/] [grey]Type 'moves' to list the legal moves.[/]");
                return false;
            case ParseResult.Ambiguous(var options):
                turn = AnsiConsole.Prompt(new SelectionPrompt<Turn>()
                    .Title("That matches more than one move — which was it?")
                    .UseConverter(MoveDescriber.Summary)
                    .AddChoices(options));
                break;
            case ParseResult.Matched(var matched):
                turn = matched;
                break;
            default:
                return false;
        }
        var lenient = MoveGenerator.IsLenientOnly(_game.Position, turn);
        if (lenient)
        {
            var question = opponentMove
                ? "That move ignores a mandatory capture — AC's AI does that sometimes. Accept it?"
                : "That move ignores a mandatory capture (illegal in standard Fanorona). Record it anyway?";
            if (!AnsiConsole.Confirm(question, defaultValue: opponentMove))
                return false;
        }
        _game.Apply(turn, lenient);
        AnsiConsole.MarkupLine($"Recorded: [bold]{MoveDescriber.Summary(turn)}[/]");
        return true;
    }

    private CommandResult HandleCommand(string input)
    {
        var command = input.Split(' ', 2)[0].ToLowerInvariant();
        var argument = input.Length > command.Length ? input[command.Length..].Trim() : "";
        switch (command)
        {
            case "help":
                PrintHelp();
                return CommandResult.Stay;
            case "moves":
                PrintMoves();
                return CommandResult.Stay;
            case "hint":
                PrintHint();
                return CommandResult.Stay;
            case "flip":
                _flipped = !_flipped;
                return CommandResult.Refresh;
            case "undo":
                AnsiConsole.MarkupLine(_game.Undo()
                    ? "[green]Took back the last half-move.[/]"
                    : "[red]Nothing to undo.[/]");
                return CommandResult.Refresh;
            case "save":
                Save(argument.Length > 0 ? argument : DefaultSavePath);
                return CommandResult.Stay;
            case "load":
                TryLoad(argument.Length > 0 ? argument : DefaultSavePath);
                return CommandResult.Refresh;
            case "think":
                if (double.TryParse(argument, out var seconds) && seconds is >= 0.1 and <= 60)
                {
                    _thinkTime = TimeSpan.FromSeconds(seconds);
                    AnsiConsole.MarkupLine($"[green]Think time set to {seconds:0.#}s.[/]");
                }
                else
                {
                    AnsiConsole.MarkupLine("[red]Usage: think <seconds between 0.1 and 60>.[/]");
                }
                return CommandResult.Stay;
            case "new":
                if (AnsiConsole.Confirm("Discard the current game and start over?", defaultValue: false))
                {
                    _game = new Game();
                    ChooseSide();
                }
                return CommandResult.Refresh;
            case "quit" or "exit":
                _quit = true;
                return CommandResult.Refresh;
            default:
                return CommandResult.NotACommand;
        }
    }

    private void PrintHelp()
    {
        var table = new Table().Border(TableBorder.Rounded).AddColumns("Command", "Effect");
        table.AddRow("e2-e3A", "a move: from-to, with A (approach) or W (withdrawal) when capturing");
        table.AddRow("d2-e3A-f4W", "a multi-capture chain; suffixes optional when unambiguous");
        table.AddRow("moves", "list all legal moves in this position");
        table.AddRow("hint", "show what the engine would play for the side to move");
        table.AddRow("undo", "take back the last entered half-move");
        table.AddRow("flip", "rotate the board 180° to match the AC camera");
        table.AddRow("save / load [[file]]", $"save or restore the game (default: {DefaultSavePath})");
        table.AddRow("think <seconds>", "set the engine's time budget");
        table.AddRow("new", "start a new game");
        table.AddRow("quit", "exit");
        AnsiConsole.Write(table);
    }

    private void PrintMoves()
    {
        var turns = MoveGenerator.LegalTurns(_game.Position);
        AnsiConsole.MarkupLine($"[bold]{turns.Count}[/] legal move(s) for {_game.Position.ToMove}:");
        foreach (var turn in turns)
            AnsiConsole.MarkupLine($"  {MoveDescriber.Summary(turn)}");
        if (turns[0].IsCapture)
            AnsiConsole.MarkupLine(
                "[grey]Captures are mandatory. If the AC opponent plays a plain move anyway, enter it and confirm.[/]");
    }

    private void PrintHint()
    {
        var result = AnsiConsole.Status().Start($"Thinking ({_thinkTime.TotalSeconds:0.#}s)...",
            _ => _engine.FindBestTurn(_game.Position, _thinkTime, _game.HistoryHashes));
        AnsiConsole.MarkupLine(
            $"Best for {_game.Position.ToMove}: [bold green]{Notation.Format(result.BestTurn)}[/]  " +
            $"[grey](depth {result.DepthReached}, {DescribeScore(result.Score)})[/]");
    }

    private void Save(string path)
    {
        try
        {
            File.WriteAllText(path, _game.Serialize());
            AnsiConsole.MarkupLine($"[green]Saved to {Markup.Escape(Path.GetFullPath(path))}.[/]");
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            AnsiConsole.MarkupLine($"[red]Couldn't save: {Markup.Escape(e.Message)}[/]");
        }
    }

    private bool TryLoad(string path)
    {
        try
        {
            _game = Game.Deserialize(File.ReadAllText(path));
            AnsiConsole.MarkupLine(
                $"[green]Loaded {_game.Moves.Count} half-move(s); you play {_game.EngineSide}.[/]");
            return true;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or FormatException)
        {
            AnsiConsole.MarkupLine($"[red]Couldn't load: {Markup.Escape(e.Message)}[/]");
            return false;
        }
    }

    private void AnnounceOutcome(GameOutcome outcome)
    {
        var message = outcome switch
        {
            GameOutcome.Draw => "[yellow bold]Draw by threefold repetition.[/]",
            GameOutcome.WhiteWins when _game.EngineSide == Player.White => "[green bold]You won![/]",
            GameOutcome.BlackWins when _game.EngineSide == Player.Black => "[green bold]You won![/]",
            _ => "[red bold]The opponent won.[/]",
        };
        AnsiConsole.MarkupLine(message);
    }

    private string DescribeScore(int score)
    {
        var you = _game.Position.ToMove == _game.EngineSide;
        if (score > 900_000)
            return $"forced win for {(you ? "you" : "the opponent")} in {(Evaluation.Win - score + 1) / 2} move(s)";
        if (score < -900_000)
            return $"forced loss for {(you ? "you" : "the opponent")} in {(Evaluation.Win + score + 1) / 2} move(s)";
        return $"eval {score / 100.0:+0.0;-0.0;+0.0} pieces";
    }

    private static string Prompt(string title) =>
        AnsiConsole.Prompt(new TextPrompt<string>($"[bold]{title}[/]:").AllowEmpty()).Trim();
}
