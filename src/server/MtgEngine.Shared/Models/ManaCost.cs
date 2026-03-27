using MtgEngine.Shared.Enums;

namespace MtgEngine.Shared.Models;

public sealed class ManaCost
{
    public int Generic { get; init; }
    public int White { get; init; }
    public int Blue { get; init; }
    public int Black { get; init; }
    public int Red { get; init; }
    public int Green { get; init; }

    public int TotalCost => Generic + White + Blue + Black + Red + Green;

    public static ManaCost Parse(string cost)
    {
        if (string.IsNullOrWhiteSpace(cost))
            return new ManaCost();

        int generic = 0;
        int white = 0, blue = 0, black = 0, red = 0, green = 0;

        foreach (char c in cost)
        {
            switch (c)
            {
                case 'W': white++; break;
                case 'U': blue++; break;
                case 'B': black++; break;
                case 'R': red++; break;
                case 'G': green++; break;
                default:
                    if (char.IsDigit(c))
                        generic = generic * 10 + (c - '0');
                    break;
            }
        }

        return new ManaCost
        {
            Generic = generic,
            White = white,
            Blue = blue,
            Black = black,
            Red = red,
            Green = green
        };
    }
}
