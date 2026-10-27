using Pingen.Client.Organisations;

namespace Pingen.Client.Tests.Organisations.Support;

public abstract class OrganisationTestBase : TestBase
{
    protected OrganisationService Organisations => Resolve<OrganisationService>();

    protected string OrganisationPath => $"/organisations/{Organisation.Id}";

    protected string OrganisationDocument(string limits = "5000") =>
        $$"""{ "data": {{OrganisationResource(Organisation.Id, limits, abilities: true)}} }""";

    protected string OrganisationPage(Guid id, int number, int lastPage) =>
        $$"""
          {
            "data": [ {{OrganisationResource(id, limits: "5000", abilities: false)}} ],
            "links": { "self": "https://api.pingen.com/organisations?page[number]={{number}}" },
            "meta": { "current_page": {{number}}, "last_page": {{lastPage}}, "per_page": 1, "from": {{number}}, "to": {{number}}, "total": 2 }
          }
          """;

    private string OrganisationResource(Guid id, string limits, bool abilities) =>
        $$"""
          {
            "id": "{{id}}",
            "type": "organisations",
            "attributes": {
              "name": "{{Organisation.Name}}",
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
              "limits_monthly_letters_count": {{limits}},
              "limits_monthly_ebills_count": {{limits}},
              "limits_monthly_emails_count": {{limits}},
              "color": "#0758FF",
              "flags": ["batch", "ebill"],
              "created_at": "2020-11-19T09:42:48+0100",
              "updated_at": "2021-11-19T09:42:48+0100"
            },
            "relationships": {
              "associations": {
                "links": { "related": { "href": "https://api.pingen.com/organisations/{{id}}/associations", "meta": { "count": 3 } } }
              }
            },
            "links": { "self": "https://api.pingen.com/organisations/{{id}}" }{{(abilities ? AbilitiesMeta : "")}}
          }
          """;

    private const string AbilitiesMeta =
        """
        , "meta": { "abilities": { "self": { "manage": "ok" } } }
        """;
}
