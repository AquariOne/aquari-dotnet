namespace Aquari.One.Internal;

internal static class AquariErrorFactory
{
    internal static AquariApiException Create(
        int statusCode,
        string? code,
        string message,
        IReadOnlyDictionary<string, IReadOnlyList<string>>? details,
        TimeSpan? retryAfter)
    {
        if (string.Equals(
                code,
                "RateLimitExceeded",
                StringComparison.Ordinal) ||
            statusCode == 429)
        {
            return new AquariRateLimitException(
                statusCode,
                code,
                message,
                details,
                retryAfter);
        }

        if (string.Equals(
                code,
                "QuotaExceeded",
                StringComparison.Ordinal))
        {
            return new AquariQuotaException(
                statusCode,
                code,
                message,
                details);
        }

        if (string.Equals(
                code,
                "ValidationFailed",
                StringComparison.Ordinal) ||
            statusCode == 400)
        {
            return new AquariValidationException(
                statusCode,
                code,
                message,
                details);
        }

        if (string.Equals(
                code,
                "Unauthorized",
                StringComparison.Ordinal) ||
            statusCode == 401)
        {
            return new AquariAuthenticationException(
                statusCode,
                code,
                message,
                details);
        }

        if (string.Equals(
                code,
                "Forbidden",
                StringComparison.Ordinal) ||
            statusCode == 403)
        {
            return new AquariAuthorizationException(
                statusCode,
                code,
                message,
                details);
        }

        if (statusCode == 404)
        {
            return new AquariNotFoundException(
                statusCode,
                code,
                message,
                details);
        }

        if (string.Equals(
                code,
                "Conflict",
                StringComparison.Ordinal) ||
            statusCode == 409)
        {
            return new AquariConflictException(
                statusCode,
                code,
                message,
                details);
        }

        return new AquariApiException(
            statusCode,
            code,
            message,
            details);
    }
}
