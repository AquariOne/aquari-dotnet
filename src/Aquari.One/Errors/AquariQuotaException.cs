using System.Globalization;

namespace Aquari.One;

/// <summary>
/// Represents an Aquari quota enforcement response.
/// </summary>
public sealed class AquariQuotaException : AquariApiException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AquariQuotaException"/> class.
    /// </summary>
    public AquariQuotaException(
        int statusCode,
        string? code,
        string message,
        IReadOnlyDictionary<string, IReadOnlyList<string>>? details = null)
        : base(statusCode, code, message, details)
    {
        Resource = First(details, "resource");
        ScopeType = First(details, "scopeType");
        ScopeId = First(details, "scopeId");
        Current = Number(details, "current");
        Requested = Number(details, "requested");
        Limit = Number(details, "limit");
        Allowance = Number(details, "allowance");
        Grace = Number(details, "grace");
        PeriodFrom = Date(details, "periodFrom");
        PeriodTo = Date(details, "periodTo");
        Enforcement = First(details, "enforcement");
    }

    /// <summary>Gets the quota resource name.</summary>
    public string? Resource { get; }

    /// <summary>Gets the quota scope type.</summary>
    public string? ScopeType { get; }

    /// <summary>Gets the quota scope identifier.</summary>
    public string? ScopeId { get; }

    /// <summary>Gets the current measured usage.</summary>
    public double? Current { get; }

    /// <summary>Gets the requested additional usage.</summary>
    public double? Requested { get; }

    /// <summary>Gets the configured limit.</summary>
    public double? Limit { get; }

    /// <summary>Gets the configured allowance.</summary>
    public double? Allowance { get; }

    /// <summary>Gets the configured grace amount.</summary>
    public double? Grace { get; }

    /// <summary>Gets the beginning of the quota period.</summary>
    public DateTimeOffset? PeriodFrom { get; }

    /// <summary>Gets the end of the quota period.</summary>
    public DateTimeOffset? PeriodTo { get; }

    /// <summary>Gets the quota enforcement mode.</summary>
    public string? Enforcement { get; }

    private static string? First(
        IReadOnlyDictionary<string, IReadOnlyList<string>>? details,
        string key)
    {
        if (details is null ||
            !details.TryGetValue(key, out var values) ||
            values.Count == 0)
        {
            return null;
        }

        return values[0];
    }

    private static double? Number(
        IReadOnlyDictionary<string, IReadOnlyList<string>>? details,
        string key)
    {
        var value = First(details, key);

        return double.TryParse(
            value,
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out var parsed)
            ? parsed
            : null;
    }

    private static DateTimeOffset? Date(
        IReadOnlyDictionary<string, IReadOnlyList<string>>? details,
        string key)
    {
        var value = First(details, key);

        return DateTimeOffset.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind,
            out var parsed)
            ? parsed
            : null;
    }
}
