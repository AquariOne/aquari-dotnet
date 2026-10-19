using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Aquari.One.Internal;

namespace Aquari.One.Transport;

/// <summary>Provides the shared Aquari JSON HTTP transport.</summary>
public sealed class AquariHttpTransport
{
    private readonly HttpClient _httpClient;
    private readonly Uri _baseUri;
    private readonly TimeSpan _timeout;
    private readonly int _maxRetries;
    private readonly TimeSpan _maxRetryDelay;

    /// <summary>Initializes an Aquari transport.</summary>
    public AquariHttpTransport(
        HttpClient httpClient,
        Uri baseUri,
        TimeSpan timeout,
        int maxRetries,
        TimeSpan maxRetryDelay)
    {
        ArgumentNullException.ThrowIfNull(httpClient);

        _httpClient = httpClient;
        _baseUri = AquariUriPolicy.NormalizeBaseUri(baseUri, "base URI");
        _timeout = timeout;
        _maxRetries = maxRetries;
        _maxRetryDelay = maxRetryDelay;
    }

    /// <summary>Sends a JSON GET request.</summary>
    public Task<JsonDocument> GetJsonAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        return GetJsonAsync(path, null, cancellationToken);
    }

    /// <summary>Sends a JSON GET request with explicit headers.</summary>
    public Task<JsonDocument> GetJsonAsync(
        string path,
        IReadOnlyDictionary<string, string>? headers,
        CancellationToken cancellationToken = default)
    {
        return SendJsonAsync(
            path,
            HttpMethod.Get,
            null,
            headers,
            AquariRetryMode.Auto,
            cancellationToken);
    }

    /// <summary>Sends a JSON request.</summary>
    public async Task<JsonDocument> SendJsonAsync(
        string path,
        HttpMethod method,
        object? json = null,
        IReadOnlyDictionary<string, string>? headers = null,
        AquariRetryMode retryMode = AquariRetryMode.Auto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(method);

        var uri = ResolveRequestUri(path);
        var serialized = json is null
            ? null
            : JsonSerializer.Serialize(json);

        var attemptIndex = 0;

        while (true)
        {
            using var timeoutSource =
                new CancellationTokenSource(_timeout);
            using var linkedSource =
                CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken,
                    timeoutSource.Token);

            try
            {
                using var request =
                    new HttpRequestMessage(method, uri);

                request.Headers.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));

                if (headers is not null)
                {
                    foreach (var pair in headers)
                    {
                        if (!request.Headers.TryAddWithoutValidation(
                                pair.Key,
                                pair.Value))
                        {
                            throw new AquariConfigurationException(
                                $"Aquari request header '{pair.Key}' is invalid.");
                        }
                    }
                }

                if (serialized is not null)
                {
                    request.Content =
                        new StringContent(
                            serialized,
                            Encoding.UTF8,
                            "application/json");
                }

                using var response =
                    await _httpClient.SendAsync(
                        request,
                        HttpCompletionOption.ResponseHeadersRead,
                        linkedSource.Token).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    var text =
                        await response.Content.ReadAsStringAsync(
                            linkedSource.Token).ConfigureAwait(false);

                    if (string.IsNullOrWhiteSpace(text))
                    {
                        return JsonDocument.Parse("{}");
                    }

                    try
                    {
                        return JsonDocument.Parse(text);
                    }
                    catch (JsonException exception)
                    {
                        throw new AquariTransportException(
                            "Aquari returned malformed JSON.",
                            innerException: exception);
                    }
                }

                var error =
                    await ReadApiErrorAsync(
                        response,
                        cancellationToken).ConfigureAwait(false);

                if (ShouldRetry(
                        method,
                        retryMode,
                        response.StatusCode,
                        attemptIndex))
                {
                    var delay =
                        GetRetryDelay(
                            response.Headers.RetryAfter,
                            attemptIndex);

                    if (delay <= _maxRetryDelay)
                    {
                        attemptIndex++;
                        await DelayAsync(delay, cancellationToken)
                            .ConfigureAwait(false);
                        continue;
                    }
                }

                throw error;
            }
            catch (AquariApiException)
            {
                throw;
            }
            catch (AquariTransportException)
            {
                throw;
            }
            catch (OperationCanceledException exception)
                when (cancellationToken.IsCancellationRequested)
            {
                throw new AquariTransportException(
                    "Aquari request was cancelled.",
                    isCancelled: true,
                    innerException: exception);
            }
            catch (OperationCanceledException exception)
            {
                if (ShouldRetry(method, retryMode, null, attemptIndex))
                {
                    var delay = GetBackoffDelay(attemptIndex);

                    if (delay <= _maxRetryDelay)
                    {
                        attemptIndex++;
                        await DelayAsync(delay, cancellationToken)
                            .ConfigureAwait(false);
                        continue;
                    }
                }

                throw new AquariTimeoutException(_timeout, exception);
            }
            catch (HttpRequestException exception)
            {
                if (ShouldRetry(method, retryMode, null, attemptIndex))
                {
                    var delay = GetBackoffDelay(attemptIndex);

                    if (delay <= _maxRetryDelay)
                    {
                        attemptIndex++;
                        await DelayAsync(delay, cancellationToken)
                            .ConfigureAwait(false);
                        continue;
                    }
                }

                throw new AquariTransportException(
                    "Aquari request failed before a valid API response was received.",
                    innerException: exception);
            }
        }
    }

    private Uri ResolveRequestUri(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new AquariConfigurationException(
                "Aquari request path cannot be empty.");
        }

        if (path.StartsWith("//", StringComparison.Ordinal) ||
            path.Contains('\\'))
        {
            throw new AquariConfigurationException(
                "Aquari request path must not use a network-path reference or backslash.");
        }

        var rootRelative = path.StartsWith('/');

        if (!rootRelative &&
            Uri.TryCreate(path, UriKind.Absolute, out _))
        {
            throw new AquariConfigurationException(
                "Aquari request path must be relative to the configured base URI.");
        }

        var uri = new Uri(_baseUri, path);

        if (!string.Equals(
                uri.Scheme,
                _baseUri.Scheme,
                StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(
                uri.Host,
                _baseUri.Host,
                StringComparison.OrdinalIgnoreCase) ||
            uri.Port != _baseUri.Port)
        {
            throw new AquariConfigurationException(
                "Aquari request cannot target another origin.");
        }

        if (!string.IsNullOrEmpty(uri.Fragment))
        {
            throw new AquariConfigurationException(
                "Aquari request path must not contain a fragment.");
        }

        return uri;
    }

    private bool ShouldRetry(
        HttpMethod method,
        AquariRetryMode retryMode,
        HttpStatusCode? statusCode,
        int attemptIndex)
    {
        if (attemptIndex >= _maxRetries ||
            retryMode == AquariRetryMode.Never)
        {
            return false;
        }

        var safe =
            retryMode == AquariRetryMode.Idempotent ||
            method == HttpMethod.Get ||
            method == HttpMethod.Head;

        if (!safe)
        {
            return false;
        }

        return statusCode is null ||
            statusCode is
                HttpStatusCode.TooManyRequests or
                HttpStatusCode.BadGateway or
                HttpStatusCode.ServiceUnavailable or
                HttpStatusCode.GatewayTimeout;
    }

    private static TimeSpan GetRetryDelay(
        RetryConditionHeaderValue? retryAfter,
        int attemptIndex)
    {
        if (retryAfter?.Delta is { } delta)
        {
            return delta < TimeSpan.Zero ? TimeSpan.Zero : delta;
        }

        if (retryAfter?.Date is { } date)
        {
            var delay = date - DateTimeOffset.UtcNow;
            return delay < TimeSpan.Zero ? TimeSpan.Zero : delay;
        }

        return GetBackoffDelay(attemptIndex);
    }

    private static TimeSpan GetBackoffDelay(int attemptIndex)
    {
        var baseMilliseconds = 250d * Math.Pow(2d, attemptIndex);
        var jitter = 0.5d + (Random.Shared.NextDouble() * 0.5d);

        return TimeSpan.FromMilliseconds(
            Math.Round(baseMilliseconds * jitter));
    }

    private static async Task DelayAsync(
        TimeSpan delay,
        CancellationToken cancellationToken)
    {
        if (delay <= TimeSpan.Zero)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return;
        }

        try
        {
            await Task.Delay(delay, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException exception)
            when (cancellationToken.IsCancellationRequested)
        {
            throw new AquariTransportException(
                "Aquari request was cancelled.",
                isCancelled: true,
                innerException: exception);
        }
    }

    private static async Task<AquariApiException> ReadApiErrorAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        var text =
            await response.Content.ReadAsStringAsync(cancellationToken)
                .ConfigureAwait(false);

        string? code = null;
        string? message = null;
        Dictionary<string, IReadOnlyList<string>>? details = null;

        if (!string.IsNullOrEmpty(text))
        {
            try
            {
                using var document = JsonDocument.Parse(text);
                var root = document.RootElement;

                if (root.ValueKind == JsonValueKind.Object &&
                    root.TryGetProperty("error", out var error) &&
                    error.ValueKind == JsonValueKind.Object)
                {
                    if (error.TryGetProperty("code", out var codeElement) &&
                        codeElement.ValueKind == JsonValueKind.String)
                    {
                        code = codeElement.GetString();
                    }

                    if (error.TryGetProperty("message", out var messageElement) &&
                        messageElement.ValueKind == JsonValueKind.String)
                    {
                        message = messageElement.GetString();
                    }

                    if (error.TryGetProperty("details", out var detailsElement))
                    {
                        details = ReadErrorDetails(detailsElement);
                    }
                }
            }
            catch (JsonException)
            {
                // HTTP status still maps to a typed Aquari exception.
            }
        }

        message ??=
            !string.IsNullOrWhiteSpace(response.ReasonPhrase)
                ? response.ReasonPhrase
                : $"Aquari API request failed with HTTP {(int)response.StatusCode}.";

        return AquariErrorFactory.Create(
            (int)response.StatusCode,
            code,
            message,
            details,
            GetRetryDelayOrNull(response.Headers.RetryAfter));
    }

    private static Dictionary<string, IReadOnlyList<string>>?
        ReadErrorDetails(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var result =
            new Dictionary<string, IReadOnlyList<string>>(
                StringComparer.Ordinal);

        foreach (var property in value.EnumerateObject())
        {
            if (property.Value.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            var items = new List<string>();

            foreach (var item in property.Value.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.String)
                {
                    items.Clear();
                    break;
                }

                var text = item.GetString();

                if (text is not null)
                {
                    items.Add(text);
                }
            }

            if (items.Count > 0)
            {
                result[property.Name] = items;
            }
        }

        return result.Count == 0 ? null : result;
    }

    private static TimeSpan? GetRetryDelayOrNull(
        RetryConditionHeaderValue? retryAfter)
    {
        return retryAfter is null
            ? null
            : GetRetryDelay(retryAfter, 0);
    }
}
