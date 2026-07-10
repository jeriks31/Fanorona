using Fanorona.Core;

namespace Fanorona.Web.Services;

/// <summary>
/// The current game, shared by all components. <see cref="Changed"/> fires on every
/// game-state mutation (not on view preferences like flipping).
/// </summary>
public sealed class GameSession
{
    public Game Game { get; private set; } = new();

    public event Action? Changed;

    public Position Position => Game.Position;
    public bool IsEngineTurn => Game.Position.ToMove == Game.EngineSide;

    public void NewGame(Player engineSide)
    {
        Game = new Game { EngineSide = engineSide };
        Changed?.Invoke();
    }

    public void LoadGame(Game game)
    {
        Game = game;
        Changed?.Invoke();
    }

    public void ApplyTurn(Turn turn, bool lenient)
    {
        Game.Apply(turn, lenient);
        Changed?.Invoke();
    }

    public void Undo()
    {
        if (Game.Undo())
            Changed?.Invoke();
    }
}
