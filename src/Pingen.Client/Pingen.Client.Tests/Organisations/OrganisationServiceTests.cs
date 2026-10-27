using System.Net;
using FluentAssertions;
using Pingen.Client.Common;
using Pingen.Client.Tests.Organisations.Support;

namespace Pingen.Client.Tests.Organisations;

public class OrganisationServiceTests : OrganisationTestBase
{
    [Fact]
    public async Task ListAsync_Gets_The_Organisations_And_Maps_The_Attributes()
    {
        // Arrange
        Host.Api.EnqueueOk(OrganisationPage(Organisation.Id, number: 1, lastPage: 1));

        var options = new PingenListOptions
        {
            PageLimit = 50,
            Sort = "-created_at",
            Search = Organisation.Name
        };

        // Act
        var organisations = await Organisations.ListAsync(options, Ct);

        // Assert
        Host.Api.Request.Assert.Sent(HttpMethod.Get, "/organisations");
        Host.Api.Request.Assert.Query($"?page[limit]=50&sort=-created_at&q={Organisation.Name}");

        organisations.Should().ContainSingle();
        organisations[0].Meta.Should().BeNull();
        organisations[0].Id.Should().Be(Organisation.Id);
        organisations[0].Type.Should().Be("organisations");
        organisations.Meta!.Total.Should().Be(2);

        organisations[0].Attributes.Status.Should().Be("active");
        organisations[0].Attributes.DataRetentionPdf.Should().Be(12);
        organisations[0].Attributes.BillingMode.Should().Be("prepaid");
        organisations[0].Attributes.BillingBalance.Should().Be(11.23m);
        organisations[0].Attributes.Name.Should().Be(Organisation.Name);
        organisations[0].Attributes.Flags.Should().Equal("batch", "ebill");
        organisations[0].Attributes.LimitsMonthlyEmailsCount.Should().Be(5000);
        organisations[0].Attributes.CreatedAt.Should().Be(new(2020, 11, 19, 9, 42, 48, TimeSpan.FromHours(1)));

        organisations[0].Relationships!.Associations!.Count.Should().Be(3);
    }

    [Fact]
    public async Task Given_Two_Pages_ListAutoPagingAsync_Yields_Both_And_Asks_For_Page_Two_Only()
    {
        // Arrange
        Host.Api
            .EnqueueOk(OrganisationPage(Organisation.Id, number: 1, lastPage: 2))
            .EnqueueOk(OrganisationPage(Organisation.OtherId, number: 2, lastPage: 2));

        var options = new PingenListOptions
        {
            PageLimit = 1,
            Sort = "-created_at",
            Search = Organisation.Name
        };

        // Act
        var organisations = await Organisations.ListAutoPagingAsync(options, Ct).ToListAsync(Ct);

        // Assert
        organisations.Select(organisation => organisation.Id).Should().Equal(Organisation.Id, Organisation.OtherId);

        Host.Api.Requests.Should().HaveCount(2);
        Host.Api.Requests[0].Assert.Query($"?page[limit]=1&sort=-created_at&q={Organisation.Name}");
        Host.Api.Requests[1].Assert.Query($"?page[number]=2&page[limit]=1&sort=-created_at&q={Organisation.Name}");
    }

    [Fact]
    public async Task Given_No_Options_ListAutoPagingAsync_Starts_Bare_And_Asks_For_Page_Two()
    {
        // Arrange
        Host.Api
            .EnqueueOk(OrganisationPage(Organisation.Id, number: 1, lastPage: 2))
            .EnqueueOk(OrganisationPage(Organisation.OtherId, number: 2, lastPage: 2));

        // Act
        var organisations = await Organisations.ListAutoPagingAsync(cancellationToken: Ct).ToListAsync(Ct);

        // Assert
        organisations.Should().HaveCount(2);

        Host.Api.Requests.Should().HaveCount(2);
        Host.Api.Requests[0].Query.Should().BeEmpty();
        Host.Api.Requests[1].Assert.Query("?page[number]=2");
    }

    [Fact]
    public async Task Given_An_Empty_Page_ListAutoPagingAsync_Stops_Although_More_Are_Announced()
    {
        // Arrange
        Host.Api.EnqueueOk(EmptyOrganisationPage());

        // Act
        var organisations = await Organisations.ListAutoPagingAsync(new() { PageLimit = 1 }, Ct).ToListAsync(Ct);

        // Assert
        organisations.Should().BeEmpty();

        Host.Api.Requests.Should().ContainSingle();
    }

    [Fact]
    public async Task GetAsync_Gets_The_Organisation_And_Maps_The_Abilities()
    {
        // Arrange
        Host.Api.EnqueueOk(OrganisationDocument());

        // Act
        var organisation = await Organisations.GetAsync(Organisation.Id, Ct);

        // Assert
        Host.Api.Request.Query.Should().BeEmpty();
        Host.Api.Request.Assert.Sent(HttpMethod.Get, OrganisationPath);

        organisation.Attributes.DefaultAddressPosition.Should().Be("left");
        organisation.Meta!.Abilities.Should().Contain(new KeyValuePair<string, string>("manage", "ok"));
    }

    [Fact]
    public async Task Given_No_Monthly_Limits_GetAsync_Maps_Them_To_Null()
    {
        // Arrange
        Host.Api.EnqueueOk(OrganisationDocument(limits: "null"));

        // Act
        var organisation = await Organisations.GetAsync(Organisation.Id, Ct);

        // Assert
        organisation.Attributes.LimitsMonthlyEbillsCount.Should().BeNull();
        organisation.Attributes.LimitsMonthlyEmailsCount.Should().BeNull();
        organisation.Attributes.LimitsMonthlyLettersCount.Should().BeNull();
    }

    [Fact]
    public async Task Given_A_Rate_Limit_ListAsync_Throws_With_The_Retry_Delay_And_The_Request_Id()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests)
        {
            Headers = { RetryAfter = new(Request.RetryAfter) }
        };

        response.Headers.Add("X-Request-Id", Request.Id);
        Host.Api.Enqueue(response);

        // Act
        var act = () => Organisations.ListAsync(cancellationToken: Ct);

        // Assert
        var exception = await act.Assert.ThrowsAsync(HttpStatusCode.TooManyRequests);
        exception.Errors.Should().BeEmpty();
        exception.RequestId.Should().Be(Request.Id);
        exception.RetryAfter.Should().Be(Request.RetryAfter);
    }
}
