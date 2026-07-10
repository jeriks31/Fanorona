# Fanorona Assistant for Assassin's Creed

An assistant for beating the Fanorona minigame AI in the Assassin's Creed games. You relay
the AC opponent's moves into the app; a game-tree search engine replies with the strongest
move for you to play back in the game.

This is not a standalone Fanorona game — it is a copilot that mirrors the AC board.

It comes in two flavors: a web app (Blazor WebAssembly — everything runs in the browser,
enter moves by clicking the board) and a console app (type moves in notation). Both share
the same engine and the same save format.

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download)

## Build and run

```
dotnet build
dotnet run --project Fanorona.Web       # web app, then open the printed localhost URL
dotnet run --project Fanorona.Console   # console app
```

Run the test suite with `dotnet test`. A non-interactive smoke test (engine plays itself for
a few turns) is available via `dotnet run --project Fanorona.Console -- --demo`.

## Web app deployment

Pushing to `main` deploys the web app to GitHub Pages via `.github/workflows/deploy.yml`
(one-time setup: repository Settings → Pages → Source = **GitHub Actions**). The workflow
publishes with AOT compilation for engine speed and rewrites the base href to `/Fanorona/`,
so the repository must be named `Fanorona`. The web app auto-saves to the browser's
localStorage after every move and offers to resume on the next visit; its save text is
interchangeable with the console app's save files.

## Using it alongside Assassin's Creed

1. Start a Fanorona game in AC and start this app.
2. Pick the color **you** play in AC (White moves first).
3. The board is shown with files `a`–`i` left to right and ranks `1`–`5` bottom to top;
   both sides use `●`, White in white and Black in yellow (true black would be invisible on
   dark terminals). White starts on ranks 1–2, Black on ranks 4–5. If the AC camera shows the
   board the other way around, type `flip` to rotate the view 180° (coordinates stay the same).
4. On your turn the app prints the recommended move with step-by-step instructions
   ("Move d2 to e3 — approach, removing f4, g5"). Play it in AC and press Enter. If you
   misclicked and played something else, type the move you actually played instead.
5. On the opponent's turn, enter the move the AC AI made.
6. Repeat until the game ends.

## Move notation

A move is the starting point followed by each point the piece lands on, e.g. `e2-e3`.
Capturing moves take an `A` (approach) or `W` (withdrawal) suffix on the destination:
`d3-e3A`. Multi-capture chains list every landing point: `d2-e3A-f4W`.

Input is forgiving: dashes are optional (`d3e3a`), case doesn't matter, and the `A`/`W`
suffix can be omitted — if the result is ambiguous (the same movement can capture by
approach *or* withdrawal), the app lists the options and asks which one happened.

## Commands

Available at any prompt:

| Command | Effect |
| --- | --- |
| `moves` | list all legal moves in the current position |
| `hint` | show what the engine would play for the side to move |
| `undo` | take back the last entered half-move (repeat to go further back) |
| `flip` | rotate the board view 180° to match the AC camera |
| `save [file]` | save the game (default `fanorona.save`, a human-readable move list) |
| `load [file]` | restore a saved game |
| `think <seconds>` | set the engine's time budget (default 1.5 s) |
| `new` | discard the game and start over |
| `quit` | exit |

## Rules implemented

Standard Fanorona (Fanoron-Tsivy, 9×5 board):

- Pieces move along drawn lines to an adjacent empty intersection. Points where
  file + rank is even are *strong* and have diagonals; the rest have only
  horizontal/vertical lines.
- **Approach** captures remove the contiguous enemy line the piece moves toward;
  **withdrawal** captures remove the line it moves away from. A movement that could do both
  captures exactly one — you choose.
- Capturing is **mandatory** when possible (but any capture is fine — there is no
  most-pieces requirement).
- A capturing piece may keep capturing in the same turn: each continuation must capture,
  may not land on any point the piece already occupied this turn, and may not move in the
  same direction twice in a row. Stopping early is allowed.
- A side with no pieces (or no legal move) loses. Threefold repetition is reported as a
  draw so the assistant never loops.

### The AC leniency quirk

The Assassin's Creed games do not make their AI respect mandatory captures. When you enter an
opponent move that skips a capture, the app flags it and asks you to confirm rather than
rejecting it. Your own recommended moves are always legal under standard rules, which makes
them legal in AC too.

## Project layout

| Project | Contents |
| --- | --- |
| `Fanorona.Core` | rules, board geometry, bitboard game state, move generation, notation, save format, and an iterative-deepening alpha-beta search with a transposition table — no dependencies |
| `Fanorona.Web` | the Blazor WebAssembly UI: click-based move entry and an SVG board that draws the recommended move |
| `Fanorona.Console` | the Spectre.Console UI |
| `Fanorona.Tests` | xUnit tests for geometry, capture rules, chain constraints, notation, persistence, engine sanity, and the web app's click-entry state machine |
