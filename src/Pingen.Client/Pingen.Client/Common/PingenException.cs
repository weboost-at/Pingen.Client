using System.Net;

namespace Pingen.Client.Common;

/// <summary>Thrown when the Pingen API answers a request with an error status.</summary>
public class PingenException(
    HttpStatusCode statusCode,
    IReadOnlyList<PingenError> errors,
    string? requestId = null,
    TimeSpan? retryAfter = null
) : Exception(Describe(statusCode, errors))
{
    /// <summary>The status code of the failed response.</summary>
    public HttpStatusCode StatusCode { get; } = statusCode;

    /// <summary>The errors the API reported, empty when the body was not a JSON:API error document.</summary>
    public IReadOnlyList<PingenError> Errors { get; } = errors;

    /// <summary>The <c>X-Request-Id</c> of the failed response, worth quoting to Pingen support.</summary>
    public string? RequestId { get; } = requestId;

    /// <summary>How long to wait before retrying, set when Pingen rate-limited the request.</summary>
    public TimeSpan? RetryAfter { get; } = retryAfter;

    private static string Describe(HttpStatusCode statusCode, IReadOnlyList<PingenError> errors) => errors switch
    {
        [{ } first, ..] => $"Pingen request failed with {(int)statusCode} {statusCode}: {first.Title ?? first.Detail ?? first.Code}",
        _ => $"Pingen request failed with {(int)statusCode} {statusCode}",
    };
}
