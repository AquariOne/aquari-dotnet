using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Aquari.One.Server.Webhooks;

/// <summary>Verifies Aquari webhooks before payload trust.</summary>
public static partial class AquariWebhookVerifier
{
    private static readonly JsonSerializerOptions KnownPayloadJsonOptions =
        new(JsonSerializerDefaults.Web);

    private static readonly HashSet<string> KnownTypes =
        new(
            new[]
            {
                "webhook.test",
                "application.connection.created",
                "application.connection.revoked",
                "application.updated",
                "organization.member.added",
                "organization.member.updated",
                "organization.member.removed",
                "organization.ownership.transferred",
                "organization.domain.verified",
                "organization.domain.revoked",
                "passport.visibility.updated",
                "passport.signal.visibility.updated",
            },
            StringComparer.Ordinal);

    /// <summary>Verifies and parses an Aquari webhook.</summary>
    public static AquariWebhookEvent Verify(
        AquariWebhookVerificationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.Secret) ||
            !string.Equals(
                options.Secret,
                options.Secret.Trim(),
                StringComparison.Ordinal))
        {
            throw new AquariConfigurationException(
                "Aquari webhook secret must be a non-empty normalized value.");
        }

        if (options.Tolerance < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                "Aquari webhook tolerance must not be negative.");
        }

        var timestampText =
            Header(
                options.Headers,
                "Aquari-Timestamp");

        if (timestampText is null)
        {
            throw Failure(
                AquariWebhookVerificationFailure.MissingTimestamp,
                "Aquari webhook timestamp is missing.");
        }

        if (!long.TryParse(
                timestampText,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var timestamp) ||
            timestamp < 0)
        {
            throw Failure(
                AquariWebhookVerificationFailure.InvalidTimestamp,
                "Aquari webhook timestamp is invalid.");
        }

        var now =
            options.Now ??
            DateTimeOffset.UtcNow;

        var eventTime =
            DateTimeOffset.FromUnixTimeSeconds(timestamp);

        if ((now - eventTime).Duration() >
            options.Tolerance)
        {
            throw Failure(
                AquariWebhookVerificationFailure.TimestampOutsideTolerance,
                "Aquari webhook timestamp is outside the accepted tolerance.");
        }

        var signature =
            Header(
                options.Headers,
                "Aquari-Signature");

        if (signature is null)
        {
            throw Failure(
                AquariWebhookVerificationFailure.MissingSignature,
                "Aquari webhook signature is missing.");
        }

        var match =
            SignaturePattern().Match(signature);

        if (!match.Success)
        {
            throw Failure(
                AquariWebhookVerificationFailure.InvalidSignatureFormat,
                "Aquari webhook signature format is invalid.");
        }

        var provided =
            Convert.FromHexString(
                match.Groups[1].Value);

        using var hmac =
            new HMACSHA256(
                Encoding.UTF8.GetBytes(
                    options.Secret));

        var prefix =
            Encoding.UTF8.GetBytes(
                $"{timestampText}.");

        var body =
            options.RawBody.ToArray();

        var signed =
            new byte[prefix.Length + body.Length];

        Buffer.BlockCopy(
            prefix,
            0,
            signed,
            0,
            prefix.Length);

        Buffer.BlockCopy(
            body,
            0,
            signed,
            prefix.Length,
            body.Length);

        var expected =
            hmac.ComputeHash(signed);

        if (provided.Length != expected.Length ||
            !CryptographicOperations.FixedTimeEquals(
                provided,
                expected))
        {
            throw Failure(
                AquariWebhookVerificationFailure.SignatureMismatch,
                "Aquari webhook signature does not match the payload.");
        }

        JsonDocument document;

        try
        {
            document =
                JsonDocument.Parse(
                    options.RawBody);
        }
        catch (JsonException exception)
        {
            throw new AquariWebhookPayloadException(
                "Aquari webhook body is not valid JSON.",
                exception);
        }

        using (document)
        {
            var root =
                document.RootElement;

            if (root.ValueKind != JsonValueKind.Object ||
                !root.TryGetProperty("data", out var dataEnvelope) ||
                dataEnvelope.ValueKind != JsonValueKind.Object ||
                !dataEnvelope.TryGetProperty(
                    "object",
                    out var payload))
            {
                throw new AquariWebhookPayloadException(
                    "Aquari webhook envelope is malformed.");
            }

            var type =
                RequiredString(root, "type");

            return new AquariWebhookEvent
            {
                Id =
                    RequiredString(root, "id"),
                Type = type,
                Version =
                    RequiredString(root, "version"),
                CreatedAt =
                    RequiredDate(root, "created_at"),
                OrganizationId =
                    RequiredString(
                        root,
                        "organization_id"),
                ApplicationId =
                    RequiredString(
                        root,
                        "application_id"),
                EnvironmentId =
                    RequiredString(
                        root,
                        "environment_id"),
                Data =
                    payload.Clone(),
                TypedData =
                    ParseKnownData(
                        type,
                        payload),
                Known =
                    KnownTypes.Contains(type),
            };
        }
    }

    private static object? ParseKnownData(
        string type,
        JsonElement payload)
    {
        try
        {
            return type switch
            {
                "webhook.test" =>
                    payload.Clone(),
                "application.connection.created" =>
                    payload.Deserialize<AquariApplicationConnectionCreatedData>(KnownPayloadJsonOptions),
                "application.connection.revoked" =>
                    payload.Deserialize<AquariApplicationConnectionRevokedData>(KnownPayloadJsonOptions),
                "application.updated" =>
                    payload.Deserialize<AquariApplicationUpdatedData>(KnownPayloadJsonOptions),
                "organization.member.added" =>
                    payload.Deserialize<AquariOrganizationMemberAddedData>(KnownPayloadJsonOptions),
                "organization.member.updated" =>
                    payload.Deserialize<AquariOrganizationMemberUpdatedData>(KnownPayloadJsonOptions),
                "organization.member.removed" =>
                    payload.Deserialize<AquariOrganizationMemberRemovedData>(KnownPayloadJsonOptions),
                "organization.ownership.transferred" =>
                    payload.Deserialize<AquariOrganizationOwnershipTransferredData>(KnownPayloadJsonOptions),
                "organization.domain.verified" =>
                    payload.Deserialize<AquariOrganizationDomainVerifiedData>(KnownPayloadJsonOptions),
                "organization.domain.revoked" =>
                    payload.Deserialize<AquariOrganizationDomainRevokedData>(KnownPayloadJsonOptions),
                "passport.visibility.updated" =>
                    payload.Deserialize<AquariPassportVisibilityUpdatedData>(KnownPayloadJsonOptions),
                "passport.signal.visibility.updated" =>
                    payload.Deserialize<AquariPassportSignalVisibilityUpdatedData>(KnownPayloadJsonOptions),
                _ => null,
            };
        }
        catch (JsonException exception)
        {
            throw new AquariWebhookPayloadException(
                $"Aquari known webhook payload for '{type}' is malformed.",
                exception);
        }
    }

    private static string? Header(
        IReadOnlyDictionary<string, string> headers,
        string name)
    {
        foreach (var pair in headers)
        {
            if (string.Equals(
                    pair.Key,
                    name,
                    StringComparison.OrdinalIgnoreCase))
            {
                return string.IsNullOrEmpty(pair.Value)
                    ? null
                    : pair.Value;
            }
        }

        return null;
    }

    private static string RequiredString(
        JsonElement root,
        string name)
    {
        if (!root.TryGetProperty(name, out var value) ||
            value.ValueKind != JsonValueKind.String ||
            string.IsNullOrEmpty(value.GetString()))
        {
            throw new AquariWebhookPayloadException(
                $"Aquari webhook field '{name}' is malformed.");
        }

        return value.GetString()!;
    }

    private static DateTimeOffset RequiredDate(
        JsonElement root,
        string name)
    {
        var text = RequiredString(root, name);

        if (!DateTimeOffset.TryParse(
                text,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out var result))
        {
            throw new AquariWebhookPayloadException(
                $"Aquari webhook field '{name}' is malformed.");
        }

        return result;
    }

    private static AquariWebhookVerificationException Failure(
        AquariWebhookVerificationFailure reason,
        string message)
    {
        return new AquariWebhookVerificationException(
            reason,
            message);
    }

    [GeneratedRegex(
        "^v1=([0-9a-f]{64})$",
        RegexOptions.CultureInvariant)]
    private static partial Regex SignaturePattern();
}
