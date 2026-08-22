using Microsoft.Extensions.Options;

namespace Pingen.Client.Options;

/// <summary>
/// Rejects a Pingen configuration that cannot authenticate.
/// </summary>
public class PingenOptionsValidator : IValidateOptions<PingenOptions>
{
    /// <summary>
    /// Fails when the client credentials are missing.
    /// </summary>
    public ValidateOptionsResult Validate(string? name, PingenOptions options)
    {
        List<string> failures = [];
        if (string.IsNullOrWhiteSpace(options.ClientId)) failures.Add($"{nameof(PingenOptions.ClientId)} is required.");
        if (string.IsNullOrWhiteSpace(options.ClientSecret)) failures.Add($"{nameof(PingenOptions.ClientSecret)} is required.");

        return failures.Count is 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}
