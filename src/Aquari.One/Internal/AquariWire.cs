using System.Globalization;
using System.Text.Json;

namespace Aquari.One.Internal;

internal static class AquariWire
{
    internal static JsonElement Object(
        JsonElement parent,
        string name)
    {
        if (!parent.TryGetProperty(name, out var value) ||
            value.ValueKind != JsonValueKind.Object)
        {
            throw Malformed(name);
        }

        return value;
    }

    internal static JsonElement? OptionalObject(
        JsonElement parent,
        string name)
    {
        if (!parent.TryGetProperty(name, out var value) ||
            value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (value.ValueKind != JsonValueKind.Object)
        {
            throw Malformed(name);
        }

        return value;
    }

    internal static JsonElement.ArrayEnumerator Array(
        JsonElement parent,
        string name)
    {
        if (!parent.TryGetProperty(name, out var value) ||
            value.ValueKind != JsonValueKind.Array)
        {
            throw Malformed(name);
        }

        return value.EnumerateArray();
    }

    internal static string String(
        JsonElement parent,
        string name,
        bool allowEmpty = false)
    {
        if (!parent.TryGetProperty(name, out var value) ||
            value.ValueKind != JsonValueKind.String)
        {
            throw Malformed(name);
        }

        var text = value.GetString();

        if (text is null ||
            (!allowEmpty && text.Length == 0))
        {
            throw Malformed(name);
        }

        return text;
    }

    internal static string? OptionalString(
        JsonElement parent,
        string name)
    {
        if (!parent.TryGetProperty(name, out var value) ||
            value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (value.ValueKind != JsonValueKind.String)
        {
            throw Malformed(name);
        }

        return value.GetString();
    }

    internal static bool Boolean(
        JsonElement parent,
        string name)
    {
        if (!parent.TryGetProperty(name, out var value))
        {
            throw Malformed(name);
        }

        return value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => throw Malformed(name),
        };
    }

    internal static int Integer(
        JsonElement parent,
        string name)
    {
        if (!parent.TryGetProperty(name, out var value) ||
            value.ValueKind != JsonValueKind.Number ||
            !value.TryGetInt32(out var result))
        {
            throw Malformed(name);
        }

        return result;
    }

    internal static DateTimeOffset Date(
        JsonElement parent,
        string name)
    {
        var text = String(parent, name);

        if (!DateTimeOffset.TryParse(
                text,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out var result))
        {
            throw Malformed(name);
        }

        return result;
    }

    internal static DateTimeOffset? OptionalDate(
        JsonElement parent,
        string name)
    {
        var text = OptionalString(parent, name);

        if (text is null)
        {
            return null;
        }

        if (!DateTimeOffset.TryParse(
                text,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out var result))
        {
            throw Malformed(name);
        }

        return result;
    }

    internal static Uri Uri(
        JsonElement parent,
        string name)
    {
        var text = String(parent, name);

        if (!System.Uri.TryCreate(
                text,
                UriKind.Absolute,
                out var result))
        {
            throw Malformed(name);
        }

        return result;
    }

    internal static Uri? OptionalUri(
        JsonElement parent,
        string name)
    {
        var text = OptionalString(parent, name);

        if (text is null)
        {
            return null;
        }

        if (!System.Uri.TryCreate(
                text,
                UriKind.Absolute,
                out var result))
        {
            throw Malformed(name);
        }

        return result;
    }

    private static AquariTransportException Malformed(
        string name)
    {
        return new AquariTransportException(
            $"Aquari response field '{name}' is malformed.");
    }
}
