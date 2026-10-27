using System.Net;
using FluentAssertions;
using Pingen.Client.Common;
using Pingen.Client.Tests.Organisations.Support;

namespace Pingen.Client.Tests.Organisations;

public class OrganisationServiceTests : OrganisationTestBase
{
    [Fact]
    public async Task When_the_organisations_of_the_user_are_listed_ListAsync_gets_them_and_maps_the_attributes()
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
    public async Task When_the_list_spans_two_pages_ListAutoPagingAsync_yields_both_and_asks_for_page_two_only()
    {
        // Arrange
        Host.Api
            .EnqueueOk(OrganisationPage(Organisation.Id, number: 1, lastPage: 2))
            .EnqueueOk(OrganisationPage(Organisation.OtherId, number: 2, lastPage: 2));

        // Act
        var organisations = await Organisations.ListAutoPagingAsync(new() { PageLimit = 1 }, Ct).ToListAsync(Ct);

        // Assert
        organisations.Select(organisation => organisation.Id).Should().Equal(Organisation.Id, Organisation.OtherId);

        Host.Api.Requests.Should().HaveCount(2);
        Host.Api.Requests[0].Assert.Query("?page[limit]=1");
        Host.Api.Requests[1].Assert.Query("?page[number]=2&page[limit]=1");
    }

    [Fact]
    public async Task When_one_organisation_is_addressed_GetAsync_gets_it_and_maps_the_abilities()
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
    public async Task When_pingen_reports_no_monthly_limits_GetAsync_maps_them_to_null()
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
    public async Task When_the_list_is_rate_limited_ListAsync_throws_with_the_retry_delay_and_the_request_id()
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
