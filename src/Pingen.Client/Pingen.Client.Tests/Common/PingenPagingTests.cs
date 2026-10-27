using FluentAssertions;
using Pingen.Client.Common;
using Pingen.Client.Tests.Common.Support;

namespace Pingen.Client.Tests.Common;

public class PingenPagingTests : UnitTestBase
{
    private readonly PageRecorder _pages = new();

    [Fact]
    public async Task When_pages_follow_each_other_EnumerateAsync_yields_every_item_in_order()
    {
        // Arrange
        _pages
            .Enqueue(Page(number: 1, lastPage: 3, "Zürich", "Genève"))
            .Enqueue(Page(number: 2, lastPage: 3, "Bern"))
            .Enqueue(Page(number: 3, lastPage: 3, "Lugano", "Chur"));

        // Act
        var items = await PingenPaging.EnumerateAsync(_pages.FetchPageAsync, cancellationToken: Ct).ToListAsync(Ct);

        // Assert
        items.Should().Equal("Zürich", "Genève", "Bern", "Lugano", "Chur");
    }

    [Fact]
    public async Task When_the_current_page_is_the_last_EnumerateAsync_fetches_no_further()
    {
        // Arrange
        _pages
            .Enqueue(Page(number: 1, lastPage: 2, "Zürich"))
            .Enqueue(Page(number: 2, lastPage: 2, "Genève"))
            .Enqueue(Page(number: 3, lastPage: 3, "Bern"));

        // Act
        await PingenPaging.EnumerateAsync(_pages.FetchPageAsync, cancellationToken: Ct).ToListAsync(Ct);

        // Assert
        _pages.Requests.Should().HaveCount(2);
    }

    [Fact]
    public async Task When_the_current_page_is_past_the_last_EnumerateAsync_fetches_no_further()
    {
        // Arrange
        _pages.Enqueue(Page(number: 3, lastPage: 2, "Zürich")).Enqueue(Page(number: 4, lastPage: 4, "Genève"));

        // Act
        await PingenPaging.EnumerateAsync(_pages.FetchPageAsync, cancellationToken: Ct).ToListAsync(Ct);

        // Assert
        _pages.Requests.Should().HaveCount(1);
    }

    [Fact]
    public async Task When_a_page_is_empty_EnumerateAsync_stops_though_its_meta_claims_more()
    {
        // Arrange
        _pages.Enqueue(Page(number: 1, lastPage: 3)).Enqueue(Page(number: 2, lastPage: 3, "Zürich"));

        // Act
        await PingenPaging.EnumerateAsync(_pages.FetchPageAsync, cancellationToken: Ct).ToListAsync(Ct);

        // Assert
        _pages.Requests.Should().HaveCount(1);
    }

    [Fact]
    public async Task When_a_page_has_no_meta_EnumerateAsync_takes_it_for_the_whole_collection()
    {
        // Arrange
        _pages
            .Enqueue(new(Data: ["Zürich", "Genève"], Links: null, Meta: null))
            .Enqueue(Page(number: 2, lastPage: 2, "Bern"));

        // Act
        var items = await PingenPaging.EnumerateAsync(_pages.FetchPageAsync, cancellationToken: Ct).ToListAsync(Ct);

        // Assert
        items.Should().Equal("Zürich", "Genève");
        _pages.Requests.Should().HaveCount(1);
    }

    [Fact]
    public async Task When_a_page_is_done_EnumerateAsync_asks_for_the_next_and_keeps_every_other_option()
    {
        // Arrange
        _pages.Enqueue(Page(number: 2, lastPage: 3, "Zürich")).Enqueue(Page(number: 3, lastPage: 3, "Genève"));

        var options = new PingenListOptions
        {
            PageNumber = 2,
            PageLimit = 1,
            Sort = "-created_at",
            Filter = PingenFilter.Where("status", "sent"),
            Search = "Zürich AG",
            Include = "organisation",
            Language = "de-DE",
            Fields = new Dictionary<string, string> { ["letters"] = "status" }
        };

        // Act
        await PingenPaging.EnumerateAsync(_pages.FetchPageAsync, options, Ct).ToListAsync(Ct);

        // Assert
        _pages.Requests.Should().Equal(options, options with { PageNumber = 3 });
    }

    [Fact]
    public async Task When_no_options_are_given_EnumerateAsync_starts_from_the_defaults()
    {
        // Arrange
        _pages.Enqueue(Page(number: 1, lastPage: 2, "Zürich")).Enqueue(Page(number: 2, lastPage: 2, "Genève"));

        // Act
        await PingenPaging.EnumerateAsync(_pages.FetchPageAsync, null, Ct).ToListAsync(Ct);

        // Assert
        _pages.Requests.Should().Equal(new PingenListOptions(), new PingenListOptions { PageNumber = 2 });
    }

    [Fact]
    public async Task When_a_token_is_passed_EnumerateAsync_hands_it_to_every_fetch()
    {
        // Arrange
        _pages.Enqueue(Page(number: 1, lastPage: 2, "Zürich")).Enqueue(Page(number: 2, lastPage: 2, "Genève"));

        // Act
        await PingenPaging.EnumerateAsync(_pages.FetchPageAsync, cancellationToken: Ct).ToListAsync(CancellationToken.None);

        // Assert
        _pages.Tokens.Should().Equal(Ct, Ct);
    }

    [Fact]
    public async Task When_the_enumerator_gets_a_token_EnumerateAsync_hands_it_to_every_fetch()
    {
        // Arrange
        _pages.Enqueue(Page(number: 1, lastPage: 2, "Zürich")).Enqueue(Page(number: 2, lastPage: 2, "Genève"));

        // Act
        await PingenPaging.EnumerateAsync(_pages.FetchPageAsync, cancellationToken: CancellationToken.None).ToListAsync(Ct);

        // Assert
        _pages.Tokens.Should().Equal(Ct, Ct);
    }

    private static PingenList<string> Page(int number, int lastPage, params string[] items) =>
        new(
            Data: items,
            Links: null,
            Meta: new() { CurrentPage = number, LastPage = lastPage }
        );
}
