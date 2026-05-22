namespace SkyRoute.BusinessLogic.UnitTest.Documents;

using SkyRoute.BusinessLogic.Documents;
using Xunit;

public sealed class DocumentValidatorTests
{
    [Theory]
    [InlineData("12345", true)]
    [InlineData("123456789012", true)]
    [InlineData("1234", false)]            // too short
    [InlineData("1234567890123", false)]   // too long
    [InlineData("12A45", false)]           // contains a letter
    [InlineData("", false)]
    public void NationalId_ValidatesDigitsLength5To12(string input, bool expected)
    {
        var validator = new NationalIdValidator();
        Assert.Equal(expected, validator.IsValid(input));
    }

    [Theory]
    [InlineData("AB12345", true)]
    [InlineData("123456", true)]
    [InlineData("AB1234567", true)]      // 9 chars
    [InlineData("AB", false)]            // too short
    [InlineData("AB1234567890", false)]  // too long
    [InlineData("AB-1234", false)]       // non-alnum
    [InlineData("", false)]
    public void Passport_ValidatesAlphaNumeric6To9(string input, bool expected)
    {
        var validator = new PassportValidator();
        Assert.Equal(expected, validator.IsValid(input));
    }

    [Theory]
    [InlineData(true, DocumentKind.Passport)]
    [InlineData(false, DocumentKind.NationalId)]
    public void Factory_PicksValidatorByRouteType(bool isInternational, DocumentKind expected)
    {
        var factory = new DocumentValidatorFactory();
        Assert.Equal(expected, factory.For(isInternational).Kind);
    }
}
