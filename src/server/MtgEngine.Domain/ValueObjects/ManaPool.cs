using MtgEngine.Shared.Enums;

namespace MtgEngine.Domain.ValueObjects;

public sealed class ManaPool
{
    public int White { get; private set; }
    public int Blue { get; private set; }
    public int Black { get; private set; }
    public int Red { get; private set; }
    public int Green { get; private set; }
    public int Colorless { get; private set; }

    public void Add(ManaColor color, int amount = 1)
    {
        switch (color)
        {
            case ManaColor.White: White += amount; break;
            case ManaColor.Blue: Blue += amount; break;
            case ManaColor.Black: Black += amount; break;
            case ManaColor.Red: Red += amount; break;
            case ManaColor.Green: Green += amount; break;
            case ManaColor.Colorless: Colorless += amount; break;
        }
    }

    public bool CanPay(Shared.Models.ManaCost cost)
    {
        if (White < cost.White || Blue < cost.Blue || Black < cost.Black ||
            Red < cost.Red || Green < cost.Green)
            return false;

        int remainingPool = (White - cost.White) + (Blue - cost.Blue) +
                            (Black - cost.Black) + (Red - cost.Red) +
                            (Green - cost.Green) + Colorless;

        return remainingPool >= cost.Generic;
    }

    public void Pay(Shared.Models.ManaCost cost)
    {
        White -= cost.White;
        Blue -= cost.Blue;
        Black -= cost.Black;
        Red -= cost.Red;
        Green -= cost.Green;

        int genericRemaining = cost.Generic;

        // Pay generic with colorless first, then any color
        int fromColorless = Math.Min(Colorless, genericRemaining);
        Colorless -= fromColorless;
        genericRemaining -= fromColorless;

        // Pay remaining generic from colored mana (arbitrary order)
        ManaColor[] order = [ManaColor.White, ManaColor.Blue, ManaColor.Black, ManaColor.Red, ManaColor.Green];
        foreach (var color in order)
        {
            if (genericRemaining <= 0) break;
            int available = GetAmount(color);
            int take = Math.Min(available, genericRemaining);
            Subtract(color, take);
            genericRemaining -= take;
        }
    }

    public void Clear()
    {
        White = Blue = Black = Red = Green = Colorless = 0;
    }

    private int GetAmount(ManaColor color) => color switch
    {
        ManaColor.White => White,
        ManaColor.Blue => Blue,
        ManaColor.Black => Black,
        ManaColor.Red => Red,
        ManaColor.Green => Green,
        ManaColor.Colorless => Colorless,
        _ => 0
    };

    private void Subtract(ManaColor color, int amount)
    {
        switch (color)
        {
            case ManaColor.White: White -= amount; break;
            case ManaColor.Blue: Blue -= amount; break;
            case ManaColor.Black: Black -= amount; break;
            case ManaColor.Red: Red -= amount; break;
            case ManaColor.Green: Green -= amount; break;
            case ManaColor.Colorless: Colorless -= amount; break;
        }
    }
}
