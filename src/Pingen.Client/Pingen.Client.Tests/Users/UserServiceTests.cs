using System.Net;
using FluentAssertions;
using Pingen.Client.Common;
using Pingen.Client.Tests.Users.Support;

namespace Pingen.Client.Tests.Users;

public class UserServiceTests : UserTestBase
{
    [Fact]
    public async Task When_the_authenticated_user_is_asked_for_GetAsync_gets_the_singleton_without_an_id_and_maps_it()
    {
        // Arrange
        Host.Api.EnqueueOk(UserDocument());

        // Act
        var user = await Users.GetAsync(Ct);

        // Assert
        Host.Api.Request.Query.Should().BeEmpty();
        Host.Api.Request.Assert.Sent(HttpMethod.Get, "/user");
        Host.Api.Request.Header("Authorization").Should().Be($"Bearer {PingenTestHost.AccessToken}");

        user.Id.Should().Be(User.Id);
        user.Type.Should().Be("users");

        user.Attributes.Flags.Should().Equal("beta");
        user.Attributes.Email.Should().Be(User.Email);
        user.Attributes.Language.Should().Be("de-DE");
        user.Attributes.LastName.Should().Be(User.LastName);
        user.Attributes.FirstName.Should().Be(User.FirstName);
        user.Attributes.UpdatedAt.Should().Be(new(2021, 11, 19, 9, 42, 48, TimeSpan.FromHours(1)));

        user.Relationships!.Associations!.Count.Should().Be(2);
        user.Relationships.Notifications!.Count.Should().Be(7);

        user.Meta!.Abilities.Should().Contain(new KeyValuePair<string, string>("manage", "ok"));
    }

    [Fact]
    public async Task When_a_page_is_requested_ListAssociationsAsync_sends_the_query_and_maps_the_organisation()
    {
        // Arrange
        Host.Api.EnqueueOk(AssociationPage(User.AssociationId, number: 1, lastPage: 1));

        var options = new PingenListOptions
        {
            PageNumber = 1,
            PageLimit = 20,
            Sort = "created_at"
        };

        // Act
        var associations = await Users.ListAssociationsAsync(options, Ct);

        // Assert
        Host.Api.Request.Assert.Sent(HttpMethod.Get, "/user/associations");
        Host.Api.Request.Assert.Query("?page[number]=1&page[limit]=20&sort=created_at");

        associations.Should().ContainSingle();
        associations[0].Meta.Should().BeNull();
        associations[0].Id.Should().Be(User.AssociationId);
        associations[0].Type.Should().Be("associations");
        associations.Meta!.Total.Should().Be(2);

        associations[0].Attributes.Role.Should().Be("owner");
        associations[0].Attributes.Status.Should().Be("active");

        associations[0].Relationships!.User!.Data!.Type.Should().Be("users");
        associations[0].Relationships!.Organisation!.Data!.Id.Should().Be(Organisation.Id.ToString());
    }

    [Fact]
    public async Task When_two_pages_are_enumerated_ListAssociationsAutoPagingAsync_yields_both_and_stops_at_the_last()
    {
        // Arrange
        Host.Api
            .EnqueueOk(AssociationPage(User.AssociationId, number: 1, lastPage: 2))
            .EnqueueOk(AssociationPage(User.OtherAssociationId, number: 2, lastPage: 2));

        // Act
        var associations = await Users.ListAssociationsAutoPagingAsync(new() { PageLimit = 1 }, Ct).ToListAsync(Ct);

        // Assert
        associations.Select(association => association.Id).Should().Equal(User.AssociationId, User.OtherAssociationId);

        Host.Api.Requests.Should().HaveCount(2);
        Host.Api.Requests[1].Assert.Query("?page[number]=2&page[limit]=1");
    }

    [Fact]
    public async Task When_the_token_was_issued_without_the_user_scope_GetAsync_throws_with_the_parsed_errors()
    {
        // Arrange
        Host.Api.Enqueue(Rejection(HttpStatusCode.Forbidden));

        // Act
        var act = () => Users.GetAsync(Ct);

        // Assert
        var exception = await act.Assert.ThrowsAsync(HttpStatusCode.Forbidden);
        exception.Errors.Should().ContainSingle();
        exception.Errors[0].Detail.Should().Be(Error.Detail);
    }
}
