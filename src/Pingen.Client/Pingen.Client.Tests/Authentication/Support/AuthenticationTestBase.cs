using Pingen.Client.Authentication;

namespace Pingen.Client.Tests.Authentication.Support;

public abstract class AuthenticationTestBase : TestBase
{
    protected AuthenticationTestData Authentication => Data.Authentication;

    protected PingenAccessTokens Tokens => Resolve<PingenAccessTokens>();
}
