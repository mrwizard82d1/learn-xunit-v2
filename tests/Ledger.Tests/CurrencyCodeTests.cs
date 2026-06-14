namespace Ledger.Tests;

public class CurrencyCodeTests
{
    [Fact]
    public void SmokeTest()
    {
        Assert.Equal(4, 2 + 2);
    }

    [Fact]
    public void Construct_WithThreeLetterUpperCaseCode_ValueIsTheCode()
    {
        var code = new CurrencyCode("USD");
        Assert.Equal("USD", code.Value);
    }

    [Theory]
    [InlineData("ars", "ARS")]
    [InlineData(" ARS\t", "ARS")]
    [InlineData("aRs", "ARS")]
    public void Construct_WithVariantData_ValueIsNormalized(string input, string expected)
    {
        var code = new CurrencyCode(input);
        Assert.Equal(expected, code.Value);
    }

    [Theory]
    [InlineData("")]        // empty
    [InlineData(" \f ")]    // whitespace
    [InlineData(null)]      // null
    [InlineData("OM")]      // too short
    [InlineData("OMRR")]    // too long
    [InlineData("OM1")]     // numeric character
    [InlineData("OM|")]     // special character / punctuation
    public void Construct_WithInvalidCurrencyCode_ThrowsArgumentException(string? input)
    {
#pragma warning disable CS8604 // Possible null reference argument.
        Assert.Throws<ArgumentException>(() => new CurrencyCode(input));
#pragma warning restore CS8604 // Possible null reference argument.
    }

    [Fact]
    public void Construct_WithCurrencyCode_ToStringReturnCurrencyCodeValue()
    {
        var toTest = new CurrencyCode("QAR");
        
        Assert.Equal("QAR", toTest.Value);
    }
}