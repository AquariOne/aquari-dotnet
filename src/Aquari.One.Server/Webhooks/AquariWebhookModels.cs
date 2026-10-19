using System.Text.Json;

namespace Aquari.One.Server.Webhooks;

/// <summary>Identifies webhook verification failure reasons.</summary>
public enum AquariWebhookVerificationFailure
{
    /// <summary>Timestamp header missing.</summary>
    MissingTimestamp,

    /// <summary>Timestamp malformed.</summary>
    InvalidTimestamp,

    /// <summary>Timestamp outside accepted tolerance.</summary>
    TimestampOutsideTolerance,

    /// <summary>Signature header missing.</summary>
    MissingSignature,

    /// <summary>Signature format malformed.</summary>
    InvalidSignatureFormat,

    /// <summary>Signature did not match.</summary>
    SignatureMismatch,
}

/// <summary>Represents webhook verification failure.</summary>
public sealed class AquariWebhookVerificationException : AquariException
{
    /// <summary>Initializes a webhook verification exception.</summary>
    public AquariWebhookVerificationException(
        AquariWebhookVerificationFailure reason,
        string message)
        : base(message)
    {
        Reason = reason;
    }

    /// <summary>Gets the verification failure reason.</summary>
    public AquariWebhookVerificationFailure Reason { get; }
}

/// <summary>Represents malformed verified webhook JSON.</summary>
public sealed class AquariWebhookPayloadException : AquariException
{
    /// <summary>Initializes a webhook payload exception.</summary>
    public AquariWebhookPayloadException(
        string message,
        Exception? innerException = null)
        : base(message, innerException)
    {
    }
}

/// <summary>Represents a verified Aquari webhook envelope.</summary>
public sealed class AquariWebhookEvent
{
    /// <summary>Gets the event identifier.</summary>
    public required string Id { get; init; }

    /// <summary>Gets the event type.</summary>
    public required string Type { get; init; }

    /// <summary>Gets the schema version.</summary>
    public required string Version { get; init; }

    /// <summary>Gets the creation time.</summary>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>Gets the organization identifier.</summary>
    public required string OrganizationId { get; init; }

    /// <summary>Gets the application identifier.</summary>
    public required string ApplicationId { get; init; }

    /// <summary>Gets the environment identifier.</summary>
    public required string EnvironmentId { get; init; }

    /// <summary>Gets the verified raw event payload.</summary>
    public required JsonElement Data { get; init; }

    /// <summary>
    /// Gets typed data for a known event, or null for an unknown event.
    /// webhook.test preserves its payload as JsonElement.
    /// </summary>
    public object? TypedData { get; init; }

    /// <summary>Gets whether this SDK version recognizes the event type.</summary>
    public required bool Known { get; init; }
}

/// <summary>Configures webhook verification.</summary>
public sealed class AquariWebhookVerificationOptions
{
    /// <summary>Gets the webhook signing secret.</summary>
    public required string Secret { get; init; }

    /// <summary>Gets the request headers.</summary>
    public required IReadOnlyDictionary<string, string> Headers { get; init; }

    /// <summary>Gets the exact raw request body bytes.</summary>
    public required ReadOnlyMemory<byte> RawBody { get; init; }

    /// <summary>Gets the allowed timestamp tolerance.</summary>
    public TimeSpan Tolerance { get; init; } = TimeSpan.FromMinutes(5);

    /// <summary>Gets the reference clock time.</summary>
    public DateTimeOffset? Now { get; init; }
}
