using System.Globalization;
using System.Text.Json;
using Aquari.One.Internal;
using Aquari.One.Transport;

namespace Aquari.One.Passport;

/// <summary>Provides connected and public Aquari Passport operations.</summary>
public sealed class AquariPassportClient
{
    private readonly AquariHttpTransport _transport;

    internal AquariPassportClient(
        AquariHttpTransport transport)
    {
        _transport = transport;
        Signals = new AquariPassportSignalsClient(transport);
    }

    /// <summary>Gets Passport signal operations.</summary>
    public AquariPassportSignalsClient Signals { get; }

    /// <summary>Loads the authenticated subject's connected Passport.</summary>
    public async Task<AquariPassport> GetAsync(
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        using var document =
            await _transport.GetJsonAsync(
                "/api/v1/passport",
                Bearer(accessToken),
                cancellationToken).ConfigureAwait(false);

        return ParsePassport(
            AquariWire.Object(
                document.RootElement,
                "passport"));
    }

    /// <summary>Loads a publicly visible Passport by handle.</summary>
    public async Task<AquariPublicPassport> GetPublicAsync(
        string handle,
        CancellationToken cancellationToken = default)
    {
        var value = RequiredValue(handle, nameof(handle));

        using var document =
            await _transport.GetJsonAsync(
                $"/api/v1/public/passports/{Uri.EscapeDataString(value)}",
                cancellationToken).ConfigureAwait(false);

        return ParsePublicPassport(document.RootElement);
    }

    internal static IReadOnlyDictionary<string, string> Bearer(
        string accessToken)
    {
        return new Dictionary<string, string>(
            StringComparer.OrdinalIgnoreCase)
        {
            ["Authorization"] =
                $"Bearer {RequiredValue(accessToken, nameof(accessToken))}",
        };
    }

    internal static string RequiredValue(
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

    internal static AquariPassportSignal ParseSignal(
        JsonElement root)
    {
        var issuer =
            AquariWire.Object(root, "issuer");
        var scope =
            AquariWire.OptionalObject(root, "scope");
        var sourceType =
            AquariWire.String(root, "sourceType");
        var presentationType =
            AquariWire.String(root, "type");

        return new AquariPassportSignal
        {
            Id =
                AquariWire.String(root, "id"),
            SubjectId =
                AquariWire.String(root, "subjectId"),
            Family =
                ResolveFamily(
                    sourceType,
                    presentationType),
            SourceType = sourceType,
            DefinitionId =
                AquariWire.OptionalString(
                    root,
                    "definitionId"),
            PresentationType = presentationType,
            Name =
                AquariWire.String(root, "name"),
            Description =
                AquariWire.String(
                    root,
                    "description",
                    allowEmpty: true),
            ImageUri =
                AquariWire.OptionalUri(root, "imageUrl"),
            Issuer =
                new AquariPassportSignalIssuer
                {
                    Type =
                        AquariWire.String(issuer, "type"),
                    Id =
                        AquariWire.String(issuer, "id"),
                    Name =
                        AquariWire.String(issuer, "name"),
                    LogoUri =
                        AquariWire.OptionalUri(
                            issuer,
                            "logoUrl"),
                },
            IssuedAt =
                AquariWire.Date(root, "issuedAt"),
            ExpiresAt =
                AquariWire.OptionalDate(
                    root,
                    "expiresAt"),
            Visibility =
                AquariWire.String(root, "visibility"),
            Verified =
                AquariWire.Boolean(root, "verified"),
            OccurredAt =
                AquariWire.OptionalDate(
                    root,
                    "occurredAt"),
            Scope =
                scope is null
                    ? null
                    : new AquariPassportSignalScope
                    {
                        Type =
                            AquariWire.String(
                                scope.Value,
                                "type"),
                        Id =
                            AquariWire.String(
                                scope.Value,
                                "id"),
                    },
        };
    }

    private static AquariPassport ParsePassport(
        JsonElement root)
    {
        var profile =
            AquariWire.Object(root, "profile");
        var signals =
            AquariWire.Object(root, "signals");

        return new AquariPassport
        {
            Id =
                AquariWire.String(root, "id"),
            PassportNumber =
                AquariWire.String(
                    root,
                    "passportNumber"),
            SubjectId =
                AquariWire.String(root, "subjectId"),
            Profile =
                new AquariPassportProfile
                {
                    DisplayName =
                        AquariWire.String(
                            profile,
                            "displayName",
                            allowEmpty: true),
                    Handle =
                        AquariWire.String(
                            profile,
                            "handle"),
                    AvatarUri =
                        AquariWire.OptionalUri(
                            profile,
                            "avatarUrl"),
                    PublicUri =
                        AquariWire.Uri(
                            profile,
                            "publicUrl"),
                    Verified =
                        AquariWire.Boolean(
                            profile,
                            "verified"),
                    Visibility =
                        AquariWire.String(
                            profile,
                            "visibility"),
                },
            Signals =
                new AquariPassportSignalSummary
                {
                    Total =
                        AquariWire.Integer(
                            signals,
                            "total"),
                },
        };
    }

    private static AquariPublicPassport ParsePublicPassport(
        JsonElement root)
    {
        var signals =
            new List<AquariPublicPassportSignal>();

        foreach (var item in AquariWire.Array(root, "signals"))
        {
            var scope =
                AquariWire.OptionalObject(item, "scope");

            signals.Add(
                new AquariPublicPassportSignal
                {
                    Id =
                        AquariWire.String(item, "id"),
                    Type =
                        AquariWire.String(item, "type"),
                    Name =
                        AquariWire.String(item, "name"),
                    Description =
                        AquariWire.String(
                            item,
                            "description",
                            allowEmpty: true),
                    ImageUri =
                        AquariWire.OptionalUri(
                            item,
                            "imageUrl"),
                    Issuer =
                        AquariWire.String(
                            item,
                            "issuer"),
                    IssuerLogoUri =
                        AquariWire.OptionalUri(
                            item,
                            "issuerLogoUrl"),
                    IssuedAt =
                        AquariWire.Date(
                            item,
                            "issuedAt"),
                    ExpiresAt =
                        AquariWire.OptionalDate(
                            item,
                            "expiresAt"),
                    Verified =
                        AquariWire.Boolean(
                            item,
                            "verified"),
                    OccurredAt =
                        AquariWire.OptionalDate(
                            item,
                            "occurredAt"),
                    Scope =
                        scope is null
                            ? null
                            : new AquariPassportSignalScope
                            {
                                Type =
                                    AquariWire.String(
                                        scope.Value,
                                        "type"),
                                Id =
                                    AquariWire.String(
                                        scope.Value,
                                        "id"),
                            },
                });
        }

        return new AquariPublicPassport
        {
            DisplayName =
                AquariWire.String(
                    root,
                    "displayName",
                    allowEmpty: true),
            Handle =
                AquariWire.String(root, "handle"),
            AvatarUri =
                AquariWire.OptionalUri(
                    root,
                    "avatarUrl"),
            PublicUri =
                AquariWire.Uri(root, "publicUrl"),
            Verified =
                AquariWire.Boolean(root, "verified"),
            Signals = signals,
        };
    }

    private static AquariPassportSignalFamily ResolveFamily(
        string sourceType,
        string presentationType)
    {
        return (
            sourceType,
            presentationType) switch
        {
            ("badge", "badge") =>
                AquariPassportSignalFamily.Badge,
            ("passport_stamp", "stamp") =>
                AquariPassportSignalFamily.Stamp,
            ("credential", "credential") =>
                AquariPassportSignalFamily.Credential,
            ("user_entitlement", "credential") =>
                AquariPassportSignalFamily.Entitlement,
            ("app_membership", "role") =>
                AquariPassportSignalFamily.Role,
            ("app_membership", "membership") =>
                AquariPassportSignalFamily.Membership,
            _ =>
                AquariPassportSignalFamily.Unknown,
        };
    }
}

/// <summary>Provides filtered and paginated Passport signal operations.</summary>
public sealed class AquariPassportSignalsClient
{
    private readonly AquariHttpTransport _transport;

    internal AquariPassportSignalsClient(
        AquariHttpTransport transport)
    {
        _transport = transport;
    }

    /// <summary>Lists connected Passport signals.</summary>
    public async Task<AquariPassportSignalPage> ListAsync(
        string accessToken,
        AquariPassportSignalListOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??=
            new AquariPassportSignalListOptions();

        if (options.Limit is < 1 or > 100)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                "Aquari Passport signal limit must be from 1 through 100.");
        }

        var query = new List<string>();

        Add(query, "type", options.Type);
        Add(query, "issuer_id", options.IssuerId);
        Add(query, "definition_id", options.DefinitionId);

        if (options.Limit is not null)
        {
            query.Add(
                $"limit={options.Limit.Value.ToString(CultureInfo.InvariantCulture)}");
        }

        Add(query, "cursor", options.Cursor);

        var path =
            query.Count == 0
                ? "/api/v1/passport/signals"
                : $"/api/v1/passport/signals?{string.Join("&", query)}";

        using var document =
            await _transport.GetJsonAsync(
                path,
                AquariPassportClient.Bearer(accessToken),
                cancellationToken).ConfigureAwait(false);

        var items =
            new List<AquariPassportSignal>();

        foreach (var item in AquariWire.Array(
                     document.RootElement,
                     "items"))
        {
            items.Add(
                AquariPassportClient.ParseSignal(item));
        }

        var page =
            AquariWire.Object(
                document.RootElement,
                "page");

        return new AquariPassportSignalPage
        {
            Items = items,
            NextCursor =
                AquariWire.OptionalString(
                    page,
                    "nextCursor"),
            HasMore =
                AquariWire.Boolean(
                    page,
                    "hasMore"),
        };
    }

    /// <summary>Gets one connected Passport signal.</summary>
    public async Task<AquariPassportSignal> GetAsync(
        string signalId,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        var id =
            AquariPassportClient.RequiredValue(
                signalId,
                nameof(signalId));

        using var document =
            await _transport.GetJsonAsync(
                $"/api/v1/passport/signals/{Uri.EscapeDataString(id)}",
                AquariPassportClient.Bearer(accessToken),
                cancellationToken).ConfigureAwait(false);

        return AquariPassportClient.ParseSignal(
            AquariWire.Object(
                document.RootElement,
                "signal"));
    }

    /// <summary>Iterates all connected Passport signal pages.</summary>
    public async IAsyncEnumerable<AquariPassportSignal> IterateAsync(
        string accessToken,
        AquariPassportSignalListOptions? options = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation]
        CancellationToken cancellationToken = default)
    {
        options ??=
            new AquariPassportSignalListOptions();

        var cursor = options.Cursor;

        while (true)
        {
            var page =
                await ListAsync(
                    accessToken,
                    new AquariPassportSignalListOptions
                    {
                        Type = options.Type,
                        IssuerId = options.IssuerId,
                        DefinitionId = options.DefinitionId,
                        Limit = options.Limit,
                        Cursor = cursor,
                    },
                    cancellationToken).ConfigureAwait(false);

            foreach (var item in page.Items)
            {
                yield return item;
            }

            if (!page.HasMore ||
                page.NextCursor is null)
            {
                yield break;
            }

            cursor = page.NextCursor;
        }
    }

    private static void Add(
        List<string> query,
        string name,
        string? value)
    {
        if (value is null)
        {
            return;
        }

        var normalized =
            AquariPassportClient.RequiredValue(
                value,
                name);

        query.Add(
            $"{Uri.EscapeDataString(name)}={Uri.EscapeDataString(normalized)}");
    }
}
