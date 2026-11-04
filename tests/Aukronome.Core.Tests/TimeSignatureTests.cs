using Aukronome.Core.Models;
using Xunit;

namespace Aukronome.Core.Tests;

public class TimeSignatureTests
{
    [Theory]
    [InlineData("4/4", 4, 4)]
    [InlineData("6/8", 6, 8)]
    [InlineData("8/8", 8, 8)]
    [InlineData("3/4", 3, 4)]
    [InlineData("7/8", 7, 8)]
    [InlineData("12/8", 12, 8)]
    [InlineData("5/4", 5, 4)]
    [InlineData("9/8", 9, 8)]
    [InlineData("2/2", 2, 2)]
    [InlineData("6 8", 6, 8)]
    [InlineData("8-8", 8, 8)]
    public void TryParse_ParsesValidSignatures(string input, int expectedNum, int expectedDen)
    {
        bool success = TimeSignature.TryParse(input, out var sig);
        Assert.True(success);
        Assert.Equal(expectedNum, sig.Numerator);
        Assert.Equal(expectedDen, sig.Denominator);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("invalid")]
    [InlineData("0/4")]
    [InlineData("4/0")]
    [InlineData("999/4")]
    public void TryParse_RejectsInvalidInput(string input)
    {
        bool success = TimeSignature.TryParse(input, out _);
        Assert.False(success);
    }

    [Fact]
    public void CreateDefaultAccents_CreatesCorrectAccentsForCompoundAndOddMeters()
    {
        // 4/4: Beat 1 accented
        var fourFour = MetronomeParameters.CreateDefaultAccents(4, 4);
        Assert.Equal(4, fourFour.Length);
        Assert.Equal(AccentLevel.Accent, fourFour[0]);
        Assert.Equal(AccentLevel.Normal, fourFour[1]);
        Assert.Equal(AccentLevel.Normal, fourFour[2]);
        Assert.Equal(AccentLevel.Normal, fourFour[3]);

        // 6/8: 3 + 3 (Beats 1 and 4 accented)
        var sixEight = MetronomeParameters.CreateDefaultAccents(6, 8);
        Assert.Equal(6, sixEight.Length);
        Assert.Equal(AccentLevel.Accent, sixEight[0]);
        Assert.Equal(AccentLevel.Normal, sixEight[1]);
        Assert.Equal(AccentLevel.Normal, sixEight[2]);
        Assert.Equal(AccentLevel.Accent, sixEight[3]);
        Assert.Equal(AccentLevel.Normal, sixEight[4]);
        Assert.Equal(AccentLevel.Normal, sixEight[5]);

        // 8/8: 3 + 3 + 2 (Beats 1, 4, and 7 accented)
        var eightEight = MetronomeParameters.CreateDefaultAccents(8, 8);
        Assert.Equal(8, eightEight.Length);
        Assert.Equal(AccentLevel.Accent, eightEight[0]);
        Assert.Equal(AccentLevel.Normal, eightEight[1]);
        Assert.Equal(AccentLevel.Normal, eightEight[2]);
        Assert.Equal(AccentLevel.Accent, eightEight[3]);
        Assert.Equal(AccentLevel.Normal, eightEight[4]);
        Assert.Equal(AccentLevel.Normal, eightEight[5]);
        Assert.Equal(AccentLevel.Accent, eightEight[6]);
        Assert.Equal(AccentLevel.Normal, eightEight[7]);

        // 7/8: 2 + 2 + 3 (Beats 1, 3, and 5 accented)
        var sevenEight = MetronomeParameters.CreateDefaultAccents(7, 8);
        Assert.Equal(7, sevenEight.Length);
        Assert.Equal(AccentLevel.Accent, sevenEight[0]);
        Assert.Equal(AccentLevel.Normal, sevenEight[1]);
        Assert.Equal(AccentLevel.Accent, sevenEight[2]);
        Assert.Equal(AccentLevel.Normal, sevenEight[3]);
        Assert.Equal(AccentLevel.Accent, sevenEight[4]);
        Assert.Equal(AccentLevel.Normal, sevenEight[5]);
        Assert.Equal(AccentLevel.Normal, sevenEight[6]);
    }
}
