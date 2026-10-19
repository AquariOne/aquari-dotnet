namespace Aquari.One.Transport;

/// <summary>Controls retry behavior for an Aquari HTTP operation.</summary>
public enum AquariRetryMode
{
    /// <summary>Retry only naturally safe methods.</summary>
    Auto,

    /// <summary>Never retry automatically.</summary>
    Never,

    /// <summary>Retry because the write is protected by idempotency.</summary>
    Idempotent,
}
