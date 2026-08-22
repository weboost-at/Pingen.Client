using System.Net;
using FluentAssertions;
using Pingen.Client.Common;
using Pingen.Client.Tests.Tests;
using Pingen.Client.Users;

namespace Pingen.Client.Tests.Users;

public class UserServiceTests
{
    private static readonly Guid UserId = Guid.Parse("4e5f6a7b-4444-4000-8000-000000000004");
    private static readonly Guid AssociationId = Guid.Parse("8c9d0e1f-5555-4000-8000-000000000005");

    private const string UserJson = """
                                    {
                                      "meta": { "abilities": { "self": { "manage": "ok" } } },
                                      "id": "4e5f6a7b-4444-4000-8000-000000000004",
                                      "type": "users",
                                      "attributes": {
                                        "email": "jürgen@example.com",
                                        "first_name": "Jürgen",
                                        "last_name": "Snow",
                                        "status": "active",
                                        "language": "de-DE",
                                        "edition": "pingen",
                                        "flags": ["beta"],
                                        "created_at": "2020-11-19T09:42:48+0100",
                                        "updated_at": "2021-11-19T09:42:48+0100"
                                      },
                                      "relationships": {
                                        "associations": {
                                          "links": { "related": { "href": "https://api.pingen.com/user/associations", "meta": { "count": 2 } } }
                                        },
                                        "notifications": {
                                          "links": { "related": { "href": "https://api.pingen.com/user/notifications", "meta": { "count": 7 } } }
                                        }
                                      },
                                      "links": { "self": "https://api.pingen.com/user" }
                                    }
                                    """;

    private static string AssociationsJson(int currentPage, int lastPage, string id) =>
        $$"""
          {
            "data": [
              {
                "id": "{{id}}",
                "type": "associations",
                "attributes": {
                  "role": "owner",
                  "status": "active",
                  "created_at": "2020-11-19T09:42:48+0100",
                  "updated_at": "2021-11-19T09:42:48+0100"
                },
                "relationships": {
                  "organisation": { "data": { "id": "6c3d1f0a-1111-4000-8000-000000000001", "type": "organisations" } },
                  "user": { "data": { "id": "4e5f6a7b-4444-4000-8000-000000000004", "type": "users" } }
                },
                "links": { "self": "https://api.pingen.com/user/associations/{{id}}" }
              }
            ],
            "links": { "self": "https://api.pingen.com/user/associations" },
            "meta": { "current_page": {{currentPage}}, "last_page": {{lastPage}}, "per_page": 1, "from": 1, "to": 1, "total": 2 }
          }
          """;

    [Fact]
    public async Task When_the_authenticated_user_is_asked_for_GetAsync_gets_the_singleton_without_an_id_and_maps_it()
    {
        // Arrange
        using var host = new PingenTestHost();
        host.Api.EnqueueOk($$"""{ "data": {{UserJson}} }""");

        // Act
        var user = await new UserService(host.Client).GetAsync(TestContext.Current.CancellationToken);

        // Assert
        host.Api.Request.Method.Should().Be(HttpMethod.Get);
        host.Api.Request.Path.Should().Be("/user");
        host.Api.Request.Query.Should().BeEmpty();
        host.Api.Request.Header("Authorization").Should().Be($"Bearer {PingenTestHost.AccessToken}");
        user.Id.Should().Be(UserId);
        user.Type.Should().Be("users");
        user.Attributes.Email.Should().Be("jürgen@example.com");
        user.Attributes.FirstName.Should().Be("Jürgen");
        user.Attributes.Language.Should().Be("de-DE");
        user.Attributes.Flags.Should().Equal("beta");
        user.Attributes.UpdatedAt.Should().Be(new(2021, 11, 19, 9, 42, 48, TimeSpan.FromHours(1)));
        user.Relationships!.Associations!.Count.Should().Be(2);
        user.Relationships.Notifications!.Count.Should().Be(7);
        user.Meta!.Abilities.Should().Contain(new KeyValuePair<string, string>("manage", "ok"));
    }

    [Fact]
    public async Task When_the_memberships_of_the_user_are_listed_ListAssociationsAsync_gets_them_and_maps_the_organisation()
    {
        // Arrange
        using var host = new PingenTestHost();
        host.Api.EnqueueOk(AssociationsJson(currentPage: 1, lastPage: 1, id: "8c9d0e1f-5555-4000-8000-000000000005"));

        // Act
        var associations = await new UserService(host.Client).ListAssociationsAsync(
            new() { PageNumber = 1, PageLimit = 20, Sort = "created_at" },
            TestContext.Current.CancellationToken
        );

        // Assert
        host.Api.Request.Method.Should().Be(HttpMethod.Get);
        host.Api.Request.Path.Should().Be("/user/associations");
        host.Api.Request.Query.Should().Be("?page[number]=1&page[limit]=20&sort=created_at");
        var association = associations.Should().ContainSingle().Which;
        association.Id.Should().Be(AssociationId);
        association.Type.Should().Be("associations");
        association.Attributes.Role.Should().Be("owner");
        association.Attributes.Status.Should().Be("active");
        association.Relationships!.Organisation!.Data!.Id.Should().Be("6c3d1f0a-1111-4000-8000-000000000001");
        association.Relationships.User!.Data!.Type.Should().Be("users");
        association.Meta.Should().BeNull();
        associations.Meta!.Total.Should().Be(2);
    }

    [Fact]
    public async Task When_the_memberships_span_two_pages_ListAssociationsAutoPagingAsync_yields_every_membership_and_stops_at_the_last_page()
    {
        // Arrange
        using var host = new PingenTestHost();
        host.Api
            .EnqueueOk(AssociationsJson(currentPage: 1, lastPage: 2, id: "8c9d0e1f-5555-4000-8000-000000000005"))
            .EnqueueOk(AssociationsJson(currentPage: 2, lastPage: 2, id: "1f2e3d4c-6666-4000-8000-000000000006"));

        // Act
        var associations = await new UserService(host.Client)
            .ListAssociationsAutoPagingAsync(new() { PageLimit = 1 }, TestContext.Current.CancellationToken)
            .ToListAsync(TestContext.Current.CancellationToken);

        // Assert
        associations.Select(association => association.Id).Should().Equal(AssociationId, Guid.Parse("1f2e3d4c-6666-4000-8000-000000000006"));
        host.Api.Requests.Should().HaveCount(2);
        host.Api.Requests[1].Query.Should().Be("?page[number]=2&page[limit]=1");
    }

    [Fact]
    public async Task When_the_token_was_issued_without_the_user_scope_GetAsync_throws_with_the_parsed_errors()
    {
        // Arrange
        using var host = new PingenTestHost();
        host.Api.EnqueueJson(
            HttpStatusCode.Forbidden,
            """{ "errors": [ { "code": "403", "title": "Forbidden", "detail": "Missing scope user", "source": { "parameter": "scope" } } ] }"""
        );

        // Act
        var act = () => new UserService(host.Client).GetAsync(TestContext.Current.CancellationToken);

        // Assert
        var exception = (await act.Should().ThrowAsync<PingenException>()).Which;
        exception.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        exception.Errors.Should().ContainSingle().Which.Detail.Should().Be("Missing scope user");
    }
}
