using Aquari.One.Auth;
using Aquari.One.Configuration;
using Aquari.One.Internal;

namespace Aquari.One.Tests;

public sealed class AuthorizationFlowTests
{
    [Fact]
    public async Task BeginAuthorizationBuildsSecurePkceRequest()
    {
        using var handler =
            new StubHttpMessageHandler(
                (_, _) =>
                    Task.FromResult(
                        TestDiscoveryDocument.Success()));

        using var client =
            CreateClient(
                new HttpClient(handler));

        var result =
            await client.Auth.BeginAuthorizationAsync(
                AquariAuthorizationPrompt.Consent);

        Assert.Equal(
            1,
            result.Transaction.Version);
        Assert.Matches(
            "^[A-Za-z0-9_-]{43}$",
            result.Transaction.State);
        Assert.Matches(
            "^[A-Za-z0-9_-]{43}$",
            result.Transaction.Nonce);
        Assert.Matches(
            "^[A-Za-z0-9_-]{64}$",
            result.Transaction.CodeVerifier);

        var query =
            result.AuthorizationUri.Query;

        Assert.Contains(
            "response_type=code",
            query,
            StringComparison.Ordinal);
        Assert.Contains(
            "code_challenge_method=S256",
            query,
            StringComparison.Ordinal);
        Assert.Contains(
            "prompt=consent",
            query,
            StringComparison.Ordinal);
        Assert.Contains(
            "scope=openid%20profile%20passport.read",
            query,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task MinimalClientDoesNotGainExtraScopes()
    {
        using var handler =
            new StubHttpMessageHandler(
                (_, _) =>
                    Task.FromResult(
                        TestDiscoveryDocument.Success()));

        using var client =
            new AquariClient(
                new AquariClientOptions
                {
                    Issuer =
                        new Uri(
                            "https://dev.api.aquari.one/"),
                    ClientId = "minimal",
                    RedirectUri =
                        new Uri(
                            "https://app.example/callback"),
                    Scopes = ["openid"],
                    MaxRetries = 0,
                },
                new HttpClient(handler));

        var result =
            await client.Auth.BeginAuthorizationAsync();

        Assert.Contains(
            "scope=openid",
            result.AuthorizationUri.Query,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            "profile",
            result.AuthorizationUri.Query,
            StringComparison.Ordinal);
    }

    [Fact]
    public void ValidCallbackIsAccepted()
    {
        var transaction =
            Transaction();

        var callback =
            new Uri(
                $"{transaction.RedirectUri.AbsoluteUri}?code=code-1&state={transaction.State}&iss={Uri.EscapeDataString(transaction.Issuer.AbsoluteUri)}");

        var parsed =
            AquariAuthorizationFlow.ParseCallback(
                callback,
                transaction,
                issuerParameterRequired: true);

        Assert.Equal(
            "code-1",
            parsed.Code);
        Assert.Equal(
            transaction.State,
            parsed.State);
    }

    [Fact]
    public void StateMismatchIsRejected()
    {
        var transaction =
            Transaction();

        var callback =
            new Uri(
                $"{transaction.RedirectUri.AbsoluteUri}?code=code-1&state=wrong&iss={Uri.EscapeDataString(transaction.Issuer.AbsoluteUri)}");

        var error =
            Assert.Throws<AquariOAuthException>(
                () =>
                    AquariAuthorizationFlow.ParseCallback(
                        callback,
                        transaction,
                        issuerParameterRequired: true));

        Assert.Equal(
            "state_mismatch",
            error.Code);
    }

    [Fact]
    public void DuplicateStateIsRejected()
    {
        var transaction =
            Transaction();

        var callback =
            new Uri(
                $"{transaction.RedirectUri.AbsoluteUri}?code=code-1&state={transaction.State}&state={transaction.State}&iss={Uri.EscapeDataString(transaction.Issuer.AbsoluteUri)}");

        var error =
            Assert.Throws<AquariOAuthException>(
                () =>
                    AquariAuthorizationFlow.ParseCallback(
                        callback,
                        transaction,
                        issuerParameterRequired: true));

        Assert.Equal(
            "invalid_callback",
            error.Code);
    }

    [Fact]
    public void MissingAdvertisedIssuerIsRejected()
    {
        var transaction =
            Transaction();

        var callback =
            new Uri(
                $"{transaction.RedirectUri.AbsoluteUri}?code=code-1&state={transaction.State}");

        var error =
            Assert.Throws<AquariOAuthException>(
                () =>
                    AquariAuthorizationFlow.ParseCallback(
                        callback,
                        transaction,
                        issuerParameterRequired: true));

        Assert.Equal(
            "missing_issuer",
            error.Code);
    }

    [Fact]
    public void RedirectOriginMismatchIsRejected()
    {
        var transaction =
            Transaction();

        var callback =
            new Uri(
                $"https://evil.example/auth/callback?code=code-1&state={transaction.State}&iss={Uri.EscapeDataString(transaction.Issuer.AbsoluteUri)}");

        var error =
            Assert.Throws<AquariOAuthException>(
                () =>
                    AquariAuthorizationFlow.ParseCallback(
                        callback,
                        transaction,
                        issuerParameterRequired: true));

        Assert.Equal(
            "invalid_callback",
            error.Code);
    }

    [Fact]
    public void ExpiredTransactionIsRejected()
    {
        var transaction =
            Transaction(
                DateTimeOffset.UtcNow.AddMinutes(-16));

        var error =
            Assert.Throws<AquariOAuthException>(
                () =>
                    AquariAuthorizationFlow.ValidateTransaction(
                        transaction,
                        transaction.Issuer,
                        transaction.ClientId,
                        transaction.RedirectUri,
                        transaction.Scopes,
                        DateTimeOffset.UtcNow));

        Assert.Equal(
            "expired_transaction",
            error.Code);
    }

    [Fact]
    public void TransactionBoundToDifferentClientIsRejected()
    {
        var transaction =
            Transaction();

        var error =
            Assert.Throws<AquariOAuthException>(
                () =>
                    AquariAuthorizationFlow.ValidateTransaction(
                        transaction,
                        transaction.Issuer,
                        "other-client",
                        transaction.RedirectUri,
                        transaction.Scopes,
                        DateTimeOffset.UtcNow));

        Assert.Equal(
            "invalid_transaction",
            error.Code);
    }

    private static AquariClient CreateClient(
        HttpClient httpClient)
    {
        return new AquariClient(
            new AquariClientOptions
            {
                Issuer =
                    new Uri(
                        "https://dev.api.aquari.one/"),
                ClientId = "client",
                RedirectUri =
                    new Uri(
                        "https://app.example/callback"),
                Scopes =
                    ["openid", "profile", "passport.read"],
                MaxRetries = 0,
            },
            httpClient);
    }

    private static AquariAuthorizationTransaction Transaction(
        DateTimeOffset? createdAt = null)
    {
        return new AquariAuthorizationTransaction
        {
            Issuer =
                new Uri(
                    "https://dev.api.aquari.one/"),
            ClientId = "client",
            RedirectUri =
                new Uri(
                    "https://app.example/callback"),
            Scopes =
                ["openid", "profile"],
            CodeVerifier =
                new string('A', 64),
            State =
                new string('B', 43),
            Nonce =
                new string('C', 43),
            CreatedAt =
                createdAt ??
                DateTimeOffset.UtcNow,
        };
    }
}
