namespace Pingen.Client.Options;

/// <summary>The Pingen deployment a client talks to.</summary>
public enum PingenEnvironment
{
    /// <summary>The live deployment - letters are printed, mailed and billed.</summary>
    Production,

    /// <summary>The staging deployment - nothing is delivered and outcomes are simulated through filename suffixes.</summary>
    Staging,
}

/// <summary>The default hosts of each Pingen deployment.</summary>
public static class PingenEnvironmentExtensions
{
    extension(PingenEnvironment environment)
    {
        /// <summary>The API host of the deployment, used unless <see cref="PingenOptions.BaseAddress"/> overrides it.</summary>
        public Uri ApiAddress => environment switch
        {
            PingenEnvironment.Staging => new("https://api-staging.pingen.com/"),
            _ => new("https://api.pingen.com/"),
        };

        /// <summary>The identity host of the deployment issuing access tokens, used unless <see cref="PingenOptions.IdentityAddress"/> overrides it.</summary>
        public Uri IdentityAddress => environment switch
        {
            PingenEnvironment.Staging => new("https://identity-staging.pingen.com/"),
            _ => new("https://identity.pingen.com/"),
        };
    }
}
