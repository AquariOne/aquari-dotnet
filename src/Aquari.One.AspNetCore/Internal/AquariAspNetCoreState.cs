using System.Security.Cryptography;
using System.Text.Json;
using Aquari.One.AspNetCore.Session;
using Microsoft.AspNetCore.Http;

namespace Aquari.One.AspNetCore.Internal;

internal static class AquariAspNetCoreState
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    internal static string TransactionKey(
        string id)
    {
        return $"aquari:transaction:{id}";
    }

    internal static string SessionKey(
        string id)
    {
        return $"aquari:session:{id}";
    }

    internal static string CreateOpaqueId()
    {
        return Convert.ToHexString(
                RandomNumberGenerator.GetBytes(16))
            .ToLowerInvariant();
    }

    internal static string NormalizeReturnTo(
        string? value)
    {
        var raw =
            value ??
            "/";

        if (!raw.StartsWith(
                '/'.ToString(),
                StringComparison.Ordinal) ||
            raw.StartsWith(
                "//",
                StringComparison.Ordinal) ||
            raw.Contains('\\'))
        {
            throw new AquariConfigurationException(
                "Aquari returnTo must be a local application path.");
        }

        if (!Uri.TryCreate(
                new Uri("https://aquari.local/"),
                raw,
                out var target) ||
            !string.Equals(
                target.Host,
                "aquari.local",
                StringComparison.Ordinal))
        {
            throw new AquariConfigurationException(
                "Aquari returnTo must stay on the current application origin.");
        }

        return target.PathAndQuery + target.Fragment;
    }

    internal static string Serialize<T>(
        T value)
    {
        return JsonSerializer.Serialize(
            value,
            JsonOptions);
    }

    internal static T Deserialize<T>(
        string value,
        string label)
    {
        try
        {
            var parsed =
                JsonSerializer.Deserialize<T>(
                    value,
                    JsonOptions);

            return parsed ??
                throw new AquariTransportException(
                    $"Aquari {label} state is empty.");
        }
        catch (JsonException exception)
        {
            throw new AquariTransportException(
                $"Aquari {label} state is malformed.",
                innerException: exception);
        }
    }

    internal static void AppendCookie(
        HttpResponse response,
        string name,
        string value,
        TimeSpan maxAge,
        bool secure,
        string? domain = null)
    {
        response.Cookies.Append(
            name,
            value,
            new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.Lax,
                Secure = secure,
                Path = "/",
                MaxAge = maxAge,
                Domain = domain,
                IsEssential = true,
            });
    }

    internal static void DeleteCookie(
        HttpResponse response,
        string name,
        bool secure,
        string? domain = null)
    {
        response.Cookies.Delete(
            name,
            new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.Lax,
                Secure = secure,
                Path = "/",
                Domain = domain,
                IsEssential = true,
            });
    }

    internal static Uri BuildCallbackUri(
        HttpRequest request,
        Uri configuredRedirectUri)
    {
        var builder =
            new UriBuilder(
                configuredRedirectUri)
            {
                Query =
                    request.QueryString.HasValue
                        ? request.QueryString.Value![1..]
                        : string.Empty,
            };

        return builder.Uri;
    }

    internal static async Task<AquariStoredTransaction?> ReadTransactionAsync(
        IAquariSessionStore store,
        string key,
        CancellationToken cancellationToken)
    {
        var raw =
            await store.GetAsync(
                key,
                cancellationToken).ConfigureAwait(false);

        if (raw is null)
        {
            return null;
        }

        var stored =
            Deserialize<AquariStoredTransaction>(
                raw,
                "authorization transaction");

        if (stored.Version !=
            AquariStoredTransaction.CurrentVersion)
        {
            throw new AquariTransportException(
                "Aquari authorization transaction version is unsupported.");
        }

        return stored;
    }

    internal static async Task<AquariStoredSession?> ReadSessionAsync(
        IAquariSessionStore store,
        string key,
        CancellationToken cancellationToken)
    {
        var raw =
            await store.GetAsync(
                key,
                cancellationToken).ConfigureAwait(false);

        if (raw is null)
        {
            return null;
        }

        var stored =
            Deserialize<AquariStoredSession>(
                raw,
                "session");

        if (stored.Version !=
            AquariStoredSession.CurrentVersion)
        {
            throw new AquariTransportException(
                "Aquari session version is unsupported.");
        }

        return stored;
    }
}
