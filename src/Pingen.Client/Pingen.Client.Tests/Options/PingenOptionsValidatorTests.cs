using FluentAssertions;
using Pingen.Client.Options;

namespace Pingen.Client.Tests.Options;

public class PingenOptionsValidatorTests : UnitTestBase
{
    private readonly PingenOptionsValidator _validator = new();

    [Theory]
    [InlineData("id", "secret")]
    [InlineData("Zürich-Client", "pässwört-ß")]
    [InlineData("клиент", "秘密")]
    [InlineData(" padded ", " x")]
    public void Given_Both_Credentials_Validation_Succeeds(string id, string secret)
    {
        // Act
        var result = _validator.Validate(name: null, new() { ClientId = id, ClientSecret = secret });

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    [Theory]
    [InlineData(null, "secret", "ClientId is required.")]
    [InlineData("", "secret", "ClientId is required.")]
    [InlineData("   ", "secret", "ClientId is required.")]
    [InlineData("\t\r\n", "sécret", "ClientId is required.")]
    [InlineData("  ", "секрет", "ClientId is required.")]
    [InlineData("id", null, "ClientSecret is required.")]
    [InlineData("id", "", "ClientSecret is required.")]
    [InlineData("Zürich", " ", "ClientSecret is required.")]
    [InlineData("клиент", "　", "ClientSecret is required.")]
    public void Given_A_Blank_Credential_Validation_Fails_Naming_It(string? id, string? secret, string failure)
    {
        // Act
        var result = _validator.Validate(name: null, new() { ClientId = id, ClientSecret = secret });

        // Assert
        result.Failed.Should().BeTrue();
        result.Failures.Should().Equal(failure);
    }

    [Fact]
    public void Given_Nothing_Configured_Validation_Reports_Both_Credentials()
    {
        // Act
        var result = _validator.Validate(name: null, new());

        // Assert
        result.Failures.Should().Equal("ClientId is required.", "ClientSecret is required.");
    }
}
