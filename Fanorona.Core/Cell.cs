namespace Fanorona.Core;

/// <summary>A board intersection, indexed 0..44 as rank * 9 + file. "a1" is index 0, "i5" is 44.</summary>
public readonly record struct Cell(int Index)
{
    public int File => Index % Board.Files;
    public int Rank => Index / Board.Files;
    public ulong Bit => 1UL << Index;

    public static Cell At(int file, int rank) => new(rank * Board.Files + file);

    public override string ToString() => $"{(char)('a' + File)}{Rank + 1}";

    public static bool TryParse(ReadOnlySpan<char> text, out Cell cell)
    {
        if (text.Length == 2 && text[0] is >= 'a' and <= 'i' && text[1] is >= '1' and <= '5')
        {
            cell = At(text[0] - 'a', text[1] - '1');
            return true;
        }
        cell = default;
        return false;
    }
}
