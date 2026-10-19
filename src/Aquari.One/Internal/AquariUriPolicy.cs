namespace Aquari.One.Internal;

internal static class AquariUriPolicy
{
    internal static Uri NormalizeBaseUri(
        Uri value,
        string label)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (!value.IsAbsoluteUri)
        {
            throw new AquariConfigurationException(
                $"Aquari {label} must be an absolute URI.");
        }

        RequireSecureHttp(value, label);

        if (!string.IsNullOrEmpty(value.UserInfo))
        {
            throw new AquariConfigurationException(
                $"Aquari {label} must not contain credentials.");
        }

        if (!string.IsNullOrEmpty(value.Query) ||
            !string.IsNullOrEmpty(value.Fragment))
        {
            throw new AquariConfigurationException(
                $"Aquari {label} must not contain a query string or fragment.");
        }

        var builder = new UriBuilder(value);
        var path = builder.Path;

        if (!path.EndsWith('/'))
        {
            builder.Path = path + "/";
        }

        return builder.Uri;
    }

    internal static void ValidateRedirectUri(Uri value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (!value.IsAbsoluteUri)
        {
            throw new AquariConfigurationException(
                "Aquari redirect URI must be absolute.");
        }

        RequireSecureHttp(value, "redirect URI");

        if (!string.IsNullOrEmpty(value.UserInfo) ||
            !string.IsNullOrEmpty(value.Fragment))
        {
            throw new AquariConfigurationException(
                "Aquari redirect URI contains unsupported URI components.");
        }
    }

    internal static Uri ReadSecureEndpoint(
        string value,
        string fieldName)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var endpoint))
        {
            throw new AquariConfigurationException(
                $"Aquari OIDC discovery field '{fieldName}' must be an absolute URI.");
        }

        RequireSecureHttp(
            endpoint,
            $"OIDC discovery field '{fieldName}'");

        if (!string.IsNullOrEmpty(endpoint.UserInfo) ||
            !string.IsNullOrEmpty(endpoint.Fragment))
        {
            throw new AquariConfigurationException(
                $"Aquari OIDC discovery field '{fieldName}' contains unsupported URI components.");
        }

        return endpoint;
    }

    private static void RequireSecureHttp(
        Uri value,
        string label)
    {
        if (string.Equals(
                value.Scheme,
                Uri.UriSchemeHttps,
                StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (string.Equals(
                value.Scheme,
                Uri.UriSchemeHttp,
                StringComparison.OrdinalIgnoreCase) &&
            IsLocalDevelopmentHost(value.Host))
        {
            return;
        }

        throw new AquariConfigurationException(
            $"Aquari {label} must use HTTPS outside local development.");
    }

    private static bool IsLocalDevelopmentHost(string host)
    {
        return string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(host, "127.0.0.1", StringComparison.Ordinal) ||
            string.Equals(host, "::1", StringComparison.Ordinal);
    }
}
