namespace Pingen.Client.Common.JsonApi;

/// <summary>
/// Turns the JSON:API list envelope into the page type the services hand out.
/// </summary>
public static class ListDocumentExtensions
{
    extension<TResource>(ListDocument<TResource> document)
    {
        /// <summary>
        /// The page this document describes, carrying its resources, links and counters.
        /// </summary>
        public PingenList<TResource> ToList() =>
            new(
                Data: document.Data,
                Links: document.Links,
                Meta: document.Meta
            );
    }
}
