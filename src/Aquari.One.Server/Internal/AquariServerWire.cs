using System.Text.Json;

namespace Aquari.One.Server.Internal;

internal static class AquariServerWire
{
    internal static T Deserialize<T>(
        JsonElement root,
        string label)
    {
        try
        {
            var value =
                root.Deserialize<T>(Options);

            return value ??
                throw new AquariTransportException(
                    $"Aquari {label} response is empty.");
        }
        catch (JsonException exception)
        {
            throw new AquariTransportException(
                $"Aquari {label} response is malformed.",
                innerException: exception);
        }
    }

    internal static IReadOnlyDictionary<string, string> Bearer(
        string apiKey)
    {
        return new Dictionary<string, string>(
            StringComparer.OrdinalIgnoreCase)
        {
            ["Authorization"] =
                $"Bearer {Required(apiKey, nameof(apiKey))}",
        };
    }

    internal static string Required(
        string value,
        string label)
    {
        if (string.IsNullOrEmpty(value) ||
            !string.Equals(
                value,
                value.Trim(),
                StringComparison.Ordinal))
        {
            throw new AquariConfigurationException(
                $"Aquari {label} must be a non-empty normalized value.");
        }

        return value;
    }

    internal static string Idempotency(
        string? value)
    {
        var result =
            value ??
            Guid.NewGuid().ToString();

        if (result.Length is < 1 or > 200 ||
            !string.Equals(
                result,
                result.Trim(),
                StringComparison.Ordinal))
        {
            throw new AquariConfigurationException(
                "Aquari idempotency key must be 1 through 200 normalized characters.");
        }

        return result;
    }

    private static JsonSerializerOptions Options { get; } =
        new(JsonSerializerDefaults.Web);
}
