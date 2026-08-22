using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using Pingen.Client.Common;
using Pingen.Client.Organisations;
using Pingen.Client.Tests.Tests;

namespace Pingen.Client.Tests.Organisations;

public class OrganisationServiceTests
{
    private static readonly Guid OrganisationId = Guid.Parse("6c3d1f0a-1111-4000-8000-000000000001");
    private const string OrganisationPath = "/organisations/6c3d1f0a-1111-4000-8000-000000000001";

    private const string Abilities = """
                                     "meta": { "abilities": { "self": { "manage": "ok" } } },
                                     """;

    private static string OrganisationJson(string id = "6c3d1f0a-1111-4000-8000-000000000001", string meta = "") =>
        $$"""
          {
            {{meta}}
            "id": "{{id}}",
            "type": "organisations",
            "attributes": {
              "name": "ACME GmbH Zürich",
              "status": "active",
              "plan": "free",
              "billing_mode": "prepaid",
              "billing_currency": "CHF",
              "billing_balance": 11.23,
              "missing_credits": 0,
              "edition": "pingen",
              "default_country": "CH",
              "default_address_position": "left",
              "data_retention_addresses": 18,
              "data_retention_pdf": 12,
              "limits_monthly_letters_count": 5000,
              "limits_monthly_ebills_count": 5000,
              "limits_monthly_emails_count": 5000,
              "color": "#0758FF",
              "flags": ["batch", "ebill"],
              "created_at": "2020-11-19T09:42:48+0100",
              "updated_at": "2021-11-19T09:42:48+0100"
            },
            "relationships": {
              "associations": {
                "links": { "related": { "href": "https://api.pingen.com/organisations/6c3d1f0a/associations", "meta": { "count": 3 } } }
              }
            },
            "links": { "self": "https://api.pingen.com/organisations/6c3d1f0a" }
          }
          """;

    private static string ListJson(int currentPage, int lastPage, string id) =>
        $$"""
          {
            "data": [ {{OrganisationJson(id)}} ],
            "links": { "self": "https://api.pingen.com/organisations" },
            "meta": { "current_page": {{currentPage}}, "last_page": {{lastPage}}, "per_page": 1, "from": 1, "to": 1, "total": 2 }
          }
          """;

    [Fact]
    public async Task When_the_organisations_of_the_user_are_listed_ListAsync_gets_them_and_maps_the_attributes()
    {
        // Arrange
        using var host = new PingenTestHost();
        host.Api.EnqueueOk(ListJson(currentPage: 1, lastPage: 1, id: "6c3d1f0a-1111-4000-8000-000000000001"));

        // Act
        var organisations = await new OrganisationService(host.Client).ListAsync(
            new() { PageLimit = 50, Sort = "-created_at", Search = "ACME" },
            TestContext.Current.CancellationToken
        );

        // Assert
        host.Api.Request.Method.Should().Be(HttpMethod.Get);
        host.Api.Request.Path.Should().Be("/organisations");
        host.Api.Request.Query.Should().Be("?page[limit]=50&sort=-created_at&q=ACME");
        var organisation = organisations.Should().ContainSingle().Which;
        organisation.Id.Should().Be(OrganisationId);
        organisation.Type.Should().Be("organisations");
        organisation.Attributes.Name.Should().Be("ACME GmbH Zürich");
        organisation.Attributes.Status.Should().Be("active");
        organisation.Attributes.BillingMode.Should().Be("prepaid");
        organisation.Attributes.BillingBalance.Should().Be(11.23m);
        organisation.Attributes.DataRetentionPdf.Should().Be(12);
        organisation.Attributes.LimitsMonthlyEmailsCount.Should().Be(5000);
        organisation.Attributes.Flags.Should().Equal("batch", "ebill");
        organisation.Attributes.CreatedAt.Should().Be(new(2020, 11, 19, 9, 42, 48, TimeSpan.FromHours(1)));
        organisation.Relationships!.Associations!.Count.Should().Be(3);
        organisation.Meta.Should().BeNull();
        organisations.Meta!.Total.Should().Be(2);
    }

    [Fact]
    public async Task When_the_list_spans_two_pages_ListAutoPagingAsync_yields_every_organisation_and_asks_for_the_next_page_only()
    {
        // Arrange
        using var host = new PingenTestHost();
        host.Api
            .EnqueueOk(ListJson(currentPage: 1, lastPage: 2, id: "6c3d1f0a-1111-4000-8000-000000000001"))
            .EnqueueOk(ListJson(currentPage: 2, lastPage: 2, id: "9e8d7c6b-8888-4000-8000-000000000008"));

        // Act
        var organisations = await new OrganisationService(host.Client)
            .ListAutoPagingAsync(new() { PageLimit = 1 }, TestContext.Current.CancellationToken)
            .ToListAsync(TestContext.Current.CancellationToken);

        // Assert
        organisations.Select(organisation => organisation.Id).Should().Equal(OrganisationId, Guid.Parse("9e8d7c6b-8888-4000-8000-000000000008"));
        host.Api.Requests.Should().HaveCount(2);
        host.Api.Requests[0].Query.Should().Be("?page[limit]=1");
        host.Api.Requests[1].Query.Should().Be("?page[number]=2&page[limit]=1");
    }

    [Fact]
    public async Task When_one_organisation_is_addressed_GetAsync_gets_it_and_maps_the_abilities()
    {
        // Arrange
        using var host = new PingenTestHost();
        host.Api.EnqueueOk($$"""{ "data": {{OrganisationJson(meta: Abilities)}} }""");

        // Act
        var organisation = await new OrganisationService(host.Client).GetAsync(OrganisationId, TestContext.Current.CancellationToken);

        // Assert
        host.Api.Request.Method.Should().Be(HttpMethod.Get);
        host.Api.Request.Path.Should().Be(OrganisationPath);
        host.Api.Request.Query.Should().BeEmpty();
        organisation.Attributes.DefaultAddressPosition.Should().Be("left");
        organisation.Meta!.Abilities.Should().Contain(new KeyValuePair<string, string>("manage", "ok"));
    }

    [Fact]
    public async Task When_the_organisation_list_is_rate_limited_ListAsync_throws_with_the_retry_delay_and_the_request_id()
    {
        // Arrange
        using var host = new PingenTestHost();
        var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
        response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(30));
        response.Headers.Add("X-Request-Id", "01HQ8Z9WQ4");
        host.Api.Enqueue(response);

        // Act
        var act = () => new OrganisationService(host.Client).ListAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        var exception = (await act.Should().ThrowAsync<PingenException>()).Which;
        exception.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        exception.RetryAfter.Should().Be(TimeSpan.FromSeconds(30));
        exception.RequestId.Should().Be("01HQ8Z9WQ4");
        exception.Errors.Should().BeEmpty();
    }
}
