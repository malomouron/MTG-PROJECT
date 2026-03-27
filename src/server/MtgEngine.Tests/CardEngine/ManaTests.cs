using MtgEngine.Domain.ValueObjects;
using MtgEngine.Shared.Enums;
using MtgEngine.Shared.Models;

namespace MtgEngine.Tests.CardEngine;

public class ManaPoolTests
{
    [Fact]
    public void Add_IncreasesCorrectColor()
    {
        ManaPool pool = new ManaPool();
        pool.Add(ManaColor.Red, 3);

        Assert.Equal(3, pool.Red);
        Assert.Equal(0, pool.Blue);
    }

    [Fact]
    public void CanPay_ExactMana_ReturnsTrue()
    {
        ManaPool pool = new ManaPool();
        pool.Add(ManaColor.Red, 2);
        pool.Add(ManaColor.Green, 1);

        ManaCost cost = ManaCost.Parse("1RR"); // 1 generic + 2 red

        Assert.True(pool.CanPay(cost));
    }

    [Fact]
    public void CanPay_NotEnoughColoredMana_ReturnsFalse()
    {
        ManaPool pool = new ManaPool();
        pool.Add(ManaColor.Red, 1);

        ManaCost cost = ManaCost.Parse("RR"); // 2 red

        Assert.False(pool.CanPay(cost));
    }

    [Fact]
    public void CanPay_GenericCanBePaidWithAnyColor()
    {
        ManaPool pool = new ManaPool();
        pool.Add(ManaColor.Green, 5);

        ManaCost cost = ManaCost.Parse("3"); // 3 generic

        Assert.True(pool.CanPay(cost));
    }

    [Fact]
    public void Pay_ReducesMana()
    {
        ManaPool pool = new ManaPool();
        pool.Add(ManaColor.Red, 3);

        ManaCost cost = ManaCost.Parse("1R");
        pool.Pay(cost);

        Assert.Equal(1, pool.Red);
    }

    [Fact]
    public void Clear_ZerosAll()
    {
        ManaPool pool = new ManaPool();
        pool.Add(ManaColor.Red, 3);
        pool.Add(ManaColor.Blue, 2);

        pool.Clear();

        Assert.Equal(0, pool.Red);
        Assert.Equal(0, pool.Blue);
    }
}

public class ManaCostParseTests
{
    [Fact]
    public void Parse_SimpleColoredCost()
    {
        ManaCost cost = ManaCost.Parse("R");
        Assert.Equal(1, cost.Red);
        Assert.Equal(0, cost.Generic);
    }

    [Fact]
    public void Parse_GenericAndColored()
    {
        ManaCost cost = ManaCost.Parse("3RR");
        Assert.Equal(3, cost.Generic);
        Assert.Equal(2, cost.Red);
    }

    [Fact]
    public void Parse_MultiColor()
    {
        ManaCost cost = ManaCost.Parse("2WUB");
        Assert.Equal(2, cost.Generic);
        Assert.Equal(1, cost.White);
        Assert.Equal(1, cost.Blue);
        Assert.Equal(1, cost.Black);
    }

    [Fact]
    public void Parse_EmptyString()
    {
        ManaCost cost = ManaCost.Parse("");
        Assert.Equal(0, cost.TotalCost);
    }

    [Fact]
    public void TotalCost_SumsAllMana()
    {
        ManaCost cost = ManaCost.Parse("3RG");
        Assert.Equal(5, cost.TotalCost);
    }
}
