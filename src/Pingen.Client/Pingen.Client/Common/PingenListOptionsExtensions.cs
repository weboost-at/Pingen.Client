namespace Pingen.Client.Common;

/// <summary>The adjustments the services make to the list options handed to them.</summary>
public static class PingenListOptionsExtensions
{
    extension(PingenListOptions? options)
    {
        /// <summary>The same options with <see cref="PingenListOptions.Sort"/> dropped, for the endpoints that accept no sort at all.</summary>
        public PingenListOptions? WithoutSort() => options is null ? null : options with { Sort = null };
    }
}
