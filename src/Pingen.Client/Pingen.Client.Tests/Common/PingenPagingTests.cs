using FluentAssertions;
using Pingen.Client.Common;
using Pingen.Client.Tests.Common.Support;

namespace Pingen.Client.Tests.Common;

public class PingenPagingTests : UnitTestBase
{
    private readonly PageRecorder _pages = new();

    [Fact]
    public async Task Every_Item_Of_Every_Page_Is_Yielded_In_Order()
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
    public async Task Given_The_Last_Page_No_Further_Page_Is_Fetched()
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
    public async Task Given_A_Page_Past_The_Last_No_Further_Page_Is_Fetched()
    {
        // Arrange
        _pages.Enqueue(Page(number: 3, lastPage: 2, "Zürich")).Enqueue(Page(number: 4, lastPage: 4, "Genève"));

        // Act
        await PingenPaging.EnumerateAsync(_pages.FetchPageAsync, cancellationToken: Ct).ToListAsync(Ct);

        // Assert
        _pages.Requests.Should().HaveCount(1);
    }

    [Fact]
    public async Task Given_An_Empty_Page_The_Walk_Stops_Though_Its_Meta_Claims_More()
    {
        // Arrange
        _pages.Enqueue(Page(number: 1, lastPage: 3)).Enqueue(Page(number: 2, lastPage: 3, "Zürich"));

        // Act
        await PingenPaging.EnumerateAsync(_pages.FetchPageAsync, cancellationToken: Ct).ToListAsync(Ct);

        // Assert
        _pages.Requests.Should().HaveCount(1);
    }

    [Fact]
    public async Task Given_A_Page_Without_Meta_It_Is_Taken_For_The_Whole_Collection()
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
    public async Task The_Next_Page_Is_Asked_For_With_Every_Other_Option_Kept()
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
    public async Task Given_No_Options_The_Walk_Starts_From_The_Defaults()
    {
        // Arrange
        _pages.Enqueue(Page(number: 1, lastPage: 2, "Zürich")).Enqueue(Page(number: 2, lastPage: 2, "Genève"));

        // Act
        await PingenPaging.EnumerateAsync(_pages.FetchPageAsync, null, Ct).ToListAsync(Ct);

        // Assert
        _pages.Requests.Should().Equal(new PingenListOptions(), new PingenListOptions { PageNumber = 2 });
    }

    [Fact]
    public async Task Given_A_Token_Argument_It_Reaches_Every_Fetch()
    {
        // Arrange
        _pages.Enqueue(Page(number: 1, lastPage: 2, "Zürich")).Enqueue(Page(number: 2, lastPage: 2, "Genève"));

        // Act
        await PingenPaging.EnumerateAsync(_pages.FetchPageAsync, cancellationToken: Ct).ToListAsync(CancellationToken.None);

        // Assert
        _pages.Tokens.Should().Equal(Ct, Ct);
    }

    [Fact]
    public async Task Given_A_Token_On_The_Enumerator_It_Reaches_Every_Fetch()
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
