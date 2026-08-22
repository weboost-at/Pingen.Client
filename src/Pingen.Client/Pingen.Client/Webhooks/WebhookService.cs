using Pingen.Client.Common;
using Pingen.Client.Common.JsonApi;

namespace Pingen.Client.Webhooks;

/// <summary>The webhook subscriptions of an organisation - <see cref="PingenWebhook"/> handles the payloads they deliver.</summary>
public class WebhookService(PingenClient client)
{
    private const string WebhookType = "webhooks";

    /// <summary>Lists one page of the organisation's webhooks - this endpoint does not sort, so <see cref="PingenListOptions.Sort"/> is ignored.</summary>
    public async Task<PingenList<Webhook>> ListAsync(Guid organisationId, PingenListOptions? options = null, CancellationToken cancellationToken = default) =>
        (await client.GetAsync<ListDocument<Webhook>>(WebhooksPath(organisationId), WithoutSort(options), cancellationToken)).ToList();

    /// <summary>Subscribes to a category of events.</summary>
    public async Task<Webhook> CreateAsync(
        Guid organisationId,
        WebhookCreateOptions options,
        PingenRequestOptions? requestOptions = null,
        CancellationToken cancellationToken = default
    )
    {
        var document = await client.SendAsync<SingleDocument<Webhook>>(
            method: HttpMethod.Post,
            path: WebhooksPath(organisationId),
            body: RequestDocument.For(WebhookType, options),
            requestOptions: requestOptions,
            cancellationToken: cancellationToken
        );

        return document.Data;
    }

    /// <summary>Fetches a single webhook.</summary>
    public async Task<Webhook> GetAsync(Guid organisationId, Guid webhookId, CancellationToken cancellationToken = default) =>
        (await client.GetAsync<SingleDocument<Webhook>>($"{WebhooksPath(organisationId)}/{webhookId}", cancellationToken)).Data;

    /// <summary>Cancels a subscription.</summary>
    public Task DeleteAsync(Guid organisationId, Guid webhookId, CancellationToken cancellationToken = default) =>
        client.SendAsync(
            method: HttpMethod.Delete,
            path: $"{WebhooksPath(organisationId)}/{webhookId}",
            body: null,
            requestOptions: null,
            cancellationToken: cancellationToken
        );

    private static string WebhooksPath(Guid organisationId) => $"organisations/{organisationId}/webhooks";

    private static PingenListOptions? WithoutSort(PingenListOptions? options) => options is null ? null : options with { Sort = null };
}
