using Pingen.Client.Users;

namespace Pingen.Client.Tests.Users.Support;

public abstract class UserTestBase : TestBase
{
    protected UserTestData User => Data.User;

    protected UserService Users => Resolve<UserService>();

    protected string UserDocument() =>
        $$"""
          {
            "data": {
              "id": "{{User.Id}}",
              "type": "users",
              "attributes": {
                "email": "{{User.Email}}",
                "first_name": "{{User.FirstName}}",
                "last_name": "{{User.LastName}}",
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
              "links": { "self": "https://api.pingen.com/user" },
              "meta": { "abilities": { "self": { "manage": "ok" } } }
            }
          }
          """;

    protected string AssociationPage(Guid id, int number, int lastPage) =>
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
                  "organisation": { "data": { "id": "{{Organisation.Id}}", "type": "organisations" } },
                  "user": { "data": { "id": "{{User.Id}}", "type": "users" } }
                },
                "links": { "self": "https://api.pingen.com/user/associations/{{id}}" }
              }
            ],
            "links": { "self": "https://api.pingen.com/user/associations?page[number]={{number}}" },
            "meta": { "current_page": {{number}}, "last_page": {{lastPage}}, "per_page": 1, "from": {{number}}, "to": {{number}}, "total": 2 }
          }
          """;
}
