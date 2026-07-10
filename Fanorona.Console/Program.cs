using System.Text;
using Fanorona.ConsoleApp;
using Fanorona.Core;
using Spectre.Console;

Console.OutputEncoding = Encoding.UTF8;

if (args is ["--demo"])
{
    // Non-interactive smoke test: engine plays both sides for a few turns.
    var game = new Game();
    var engine = new Engine();
    for (var i = 0; i < 4 && game.Outcome == null; i++)
    {
        var result = engine.FindBestTurn(game.Position, TimeSpan.FromMilliseconds(300), game.HistoryHashes);
        AnsiConsole.MarkupLine($"{game.Position.ToMove} plays [bold]{Notation.Format(result.BestTurn)}[/]");
        foreach (var step in MoveDescriber.DescribeSteps(result.BestTurn))
            AnsiConsole.MarkupLine($"  › {step}");
        game.Apply(result.BestTurn);
        AnsiConsole.Markup(BoardRenderer.Render(game.Position, game.LastTurn, flipped: false));
        AnsiConsole.WriteLine();
    }
    AnsiConsole.MarkupLine("[bold]Flipped view:[/]");
    AnsiConsole.Markup(BoardRenderer.Render(game.Position, game.LastTurn, flipped: true));
    return;
}

new AssistantApp().Run();
