using FluentAssertions;
using Microsoft.Extensions.Options;
using Pingen.Client.Options;

namespace Pingen.Client.Tests.Options;

public class PingenOptionsValidatorTests
{
    [Fact]
    public void When_both_credentials_are_set_Validate_succeeds()
    {
        // Act
        var result = Validate(new() { ClientId = "id", ClientSecret = "secret" });

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    [Theory]
    [InlineData(null, "secret", "ClientId is required.")]
    [InlineData("", "secret", "ClientId is required.")]
    [InlineData("   ", "secret", "ClientId is required.")]
    [InlineData("id", null, "ClientSecret is required.")]
    [InlineData("id", "", "ClientSecret is required.")]
    public void When_a_credential_is_missing_Validate_fails_naming_it(string? clientId, string? clientSecret, string expected)
    {
        // Act
        var result = Validate(new() { ClientId = clientId, ClientSecret = clientSecret });

        // Assert
        result.Failed.Should().BeTrue();
        result.Failures.Should().ContainSingle().Which.Should().Be(expected);
    }

    [Fact]
    public void When_nothing_is_configured_Validate_reports_both_credentials()
    {
        // Act
        var result = Validate(new());

        // Assert
        result.Failures.Should().HaveCount(2);
    }

    private static ValidateOptionsResult Validate(PingenOptions options) => new PingenOptionsValidator().Validate(name: null, options);
}
