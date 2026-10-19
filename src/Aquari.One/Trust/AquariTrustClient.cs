using System.Text.Json;
using Aquari.One.Internal;
using Aquari.One.Passport;
using Aquari.One.Transport;

namespace Aquari.One.Trust;

/// <summary>Provides Aquari trust-evaluation operations.</summary>
public sealed class AquariTrustClient
{
    private readonly AquariHttpTransport _transport;

    internal AquariTrustClient(
        AquariHttpTransport transport)
    {
        _transport = transport;
    }

    /// <summary>Evaluates trust for the authenticated subject.</summary>
    public async Task<AquariTrustEvaluation> EvaluateAsync(
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        using var document =
            await _transport.GetJsonAsync(
                "/api/v1/passport/trust/evaluation",
                AquariPassportClient.Bearer(accessToken),
                cancellationToken).ConfigureAwait(false);

        return ParseEvaluation(document.RootElement);
    }

    private static AquariTrustEvaluation ParseEvaluation(
        JsonElement root)
    {
        var outcomes = new List<AquariTrustOutcome>();

        foreach (var item in AquariWire.Array(root, "outcomes"))
        {
            var supports = new List<AquariTrustSupport>();

            foreach (var support in AquariWire.Array(item, "supports"))
            {
                supports.Add(ParseSupport(support));
            }

            outcomes.Add(
                new AquariTrustOutcome
                {
                    Type =
                        AquariWire.String(item, "type"),
                    Value =
                        AquariWire.String(item, "value"),
                    Supports = supports,
                });
        }

        return new AquariTrustEvaluation
        {
            EvaluationId =
                AquariWire.String(root, "evaluationId"),
            SubjectId =
                AquariWire.String(root, "subjectId"),
            ConsumerApplicationId =
                AquariWire.String(
                    root,
                    "consumerApplicationId"),
            EvaluatedAt =
                AquariWire.Date(root, "evaluatedAt"),
            Outcomes = outcomes,
        };
    }

    private static AquariTrustSupport ParseSupport(
        JsonElement root)
    {
        var signal =
            AquariWire.Object(root, "signal");
        var authority =
            AquariWire.Object(signal, "authority");
        var scope =
            AquariWire.OptionalObject(signal, "scope");

        return new AquariTrustSupport
        {
            MappingId =
                AquariWire.String(root, "mappingId"),
            MappingVersion =
                AquariWire.Integer(root, "mappingVersion"),
            TrustedIssuerId =
                AquariWire.String(
                    root,
                    "trustedIssuerId"),
            Signal =
                new AquariTrustSignal
                {
                    Id =
                        AquariWire.String(signal, "id"),
                    SignalFamily =
                        AquariWire.String(
                            signal,
                            "signalFamily"),
                    DefinitionId =
                        AquariWire.OptionalString(
                            signal,
                            "definitionId"),
                    Authority =
                        new AquariTrustAuthority
                        {
                            Type =
                                AquariWire.String(
                                    authority,
                                    "type"),
                            Id =
                                AquariWire.String(
                                    authority,
                                    "id"),
                        },
                    Scope =
                        scope is null
                            ? null
                            : new AquariTrustScope
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
                    IssuedAt =
                        AquariWire.Date(
                            signal,
                            "issuedAt"),
                    ExpiresAt =
                        AquariWire.OptionalDate(
                            signal,
                            "expiresAt"),
                },
        };
    }
}
