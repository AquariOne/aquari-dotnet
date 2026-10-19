using System.Net.Http.Headers;
using System.Text.Json;

namespace Aquari.One.Transport;

internal sealed class AquariOAuthHttpClient
{
    private readonly HttpClient _httpClient;
    private readonly TimeSpan _timeout;

    internal AquariOAuthHttpClient(
        HttpClient httpClient,
        TimeSpan timeout)
    {
        _httpClient = httpClient;
        _timeout = timeout;
    }

    internal async Task<JsonDocument> PostFormAsync(
        Uri endpoint,
        IEnumerable<KeyValuePair<string, string>> parameters,
        string fallbackCode,
        string operation,
        CancellationToken cancellationToken)
    {
        using var request =
            new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = new FormUrlEncodedContent(parameters),
            };

        request.Headers.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

        return await SendJsonAsync(
            request,
            fallbackCode,
            operation,
            cancellationToken).ConfigureAwait(false);
    }

    internal async Task<JsonDocument> GetJsonAsync(
        Uri endpoint,
        string? bearerToken,
        string fallbackCode,
        string operation,
        CancellationToken cancellationToken)
    {
        using var request =
            new HttpRequestMessage(HttpMethod.Get, endpoint);

        request.Headers.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

        if (bearerToken is not null)
        {
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", bearerToken);
        }

        return await SendJsonAsync(
            request,
            fallbackCode,
            operation,
            cancellationToken).ConfigureAwait(false);
    }

    internal async Task<string> GetTextAsync(
        Uri endpoint,
        string fallbackCode,
        string operation,
        CancellationToken cancellationToken)
    {
        using var request =
            new HttpRequestMessage(HttpMethod.Get, endpoint);

        using var response =
            await SendAsync(request, cancellationToken)
                .ConfigureAwait(false);

        var text =
            await response.Content.ReadAsStringAsync(cancellationToken)
                .ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw ReadOAuthError(
                text,
                fallbackCode,
                operation,
                (int)response.StatusCode);
        }

        return text;
    }

    private async Task<JsonDocument> SendJsonAsync(
        HttpRequestMessage request,
        string fallbackCode,
        string operation,
        CancellationToken cancellationToken)
    {
        using var response =
            await SendAsync(request, cancellationToken)
                .ConfigureAwait(false);

        var text =
            await response.Content.ReadAsStringAsync(cancellationToken)
                .ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw ReadOAuthError(
                text,
                fallbackCode,
                operation,
                (int)response.StatusCode);
        }

        try
        {
            return JsonDocument.Parse(text);
        }
        catch (JsonException)
        {
            throw new AquariOAuthException(
                fallbackCode,
                $"Aquari {operation} returned malformed JSON.");
        }
    }

    private async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        using var timeoutSource =
            new CancellationTokenSource(_timeout);
        using var linkedSource =
            CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                timeoutSource.Token);

        try
        {
            return await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                linkedSource.Token).ConfigureAwait(false);
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
            throw new AquariTimeoutException(_timeout, exception);
        }
        catch (HttpRequestException exception)
        {
            throw new AquariTransportException(
                "Aquari OAuth request failed before a valid response was received.",
                innerException: exception);
        }
    }

    private static AquariOAuthException ReadOAuthError(
        string text,
        string fallbackCode,
        string operation,
        int statusCode)
    {
        if (!string.IsNullOrEmpty(text))
        {
            try
            {
                using var document = JsonDocument.Parse(text);
                var root = document.RootElement;

                if (root.ValueKind == JsonValueKind.Object &&
                    root.TryGetProperty("error", out var error) &&
                    error.ValueKind == JsonValueKind.String)
                {
                    var code = error.GetString();

                    if (!string.IsNullOrEmpty(code))
                    {
                        var description =
                            ReadOptionalString(
                                root,
                                "error_description");

                        return new AquariOAuthException(
                            code,
                            description ??
                                $"Aquari returned OAuth error '{code}'.",
                            description);
                    }
                }
            }
            catch (JsonException)
            {
                // Use the operation-specific fallback.
            }
        }

        return new AquariOAuthException(
            fallbackCode,
            $"Aquari {operation} failed with HTTP {statusCode}.");
    }

    private static string? ReadOptionalString(
        JsonElement root,
        string name)
    {
        if (!root.TryGetProperty(name, out var value) ||
            value.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        var text = value.GetString();
        return string.IsNullOrEmpty(text) ? null : text;
    }
}
