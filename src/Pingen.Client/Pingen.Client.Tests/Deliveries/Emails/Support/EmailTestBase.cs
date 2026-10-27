using Pingen.Client.Deliveries.Emails;

namespace Pingen.Client.Tests.Deliveries.Emails.Support;

public abstract class EmailTestBase : TestBase
{
    protected EmailTestData Email => Data.Email;

    protected EmailService Emails => Resolve<EmailService>();

    protected string EmailsPath => $"/organisations/{Organisation.Id}/deliveries/emails";

    protected string EmailPath => $"{EmailsPath}/{Email.Id}";

    protected EmailCreateOptions EmailCreateOptions() =>
        new()
        {
            FileOriginalName = Email.FileOriginalName,
            AutoSend = true,
            MetaData = Email.MetaData
        };

    protected string EmailDocument() =>
        $$"""{ "data": {{EmailResource(filePages: 2)}} }""";

    protected string EmailPage(int number, int lastPage) =>
        $$"""
          {
            "data": [ {{EmailResource(filePages: number, abilities: false)}} ],
            "links": { "self": "https://api.pingen.com{{EmailsPath}}?page[number]={{number}}" },
            "meta": { "current_page": {{number}}, "last_page": {{lastPage}}, "per_page": 1, "from": {{number}}, "to": {{number}}, "total": {{lastPage}} }
          }
          """;

    protected string EventsDocument() =>
        $$"""
          {
            "data": [
              {
                "id": "{{Email.EventId}}",
                "type": "deliverables_events",
                "attributes": {
                  "code": "delivered",
                  "name": "Zugestellt",
                  "producer": "Pingen",
                  "location": "8051 Zürich, CH",
                  "has_image": true,
                  "data": [],
                  "emitted_at": "2021-11-19T09:42:48+0100",
                  "created_at": "2021-11-19T09:42:48+0100",
                  "updated_at": "2021-11-19T09:42:48+0100"
                },
                "relationships": {
                  "email": { "data": { "id": "{{Email.Id}}", "type": "emails" } }
                }
              }
            ],
            "meta": { "current_page": 1, "last_page": 1, "per_page": 20, "from": 1, "to": 1, "total": 1 }
          }
          """;

    // The ISO offset of updated_at is deliberate - the converter reads it beside Pingen's colon-less one
    private string EmailResource(int filePages, bool abilities = true) =>
        $$"""
          {
            "id": "{{Email.Id}}",
            "type": "emails",
            "attributes": {
              "status": "sent",
              "file_original_name": "{{Email.FileOriginalName}}",
              "file_pages": {{filePages}},
              "recipient_identifier": "{{Email.MetaData.RecipientEmail}}",
              "price_currency": "CHF",
              "price_value": 1.25,
              "source": "api",
              "submitted_at": "2021-11-19T09:42:48+0100",
              "created_at": "2020-11-19T09:42:48+0100",
              "updated_at": "2020-11-20T10:00:00+01:00"
            },
            "relationships": {
              "organisation": {
                "links": { "related": "https://api.pingen.com/organisations/{{Organisation.Id}}" },
                "data": { "id": "{{Organisation.Id}}", "type": "organisations" }
              },
              "batch": { "data": null },
              "events": {
                "links": { "related": { "href": "https://api.pingen.com{{EmailPath}}/events", "meta": { "count": 3 } } }
              }
            },
            "links": { "self": "https://api.pingen.com{{EmailPath}}" }{{(abilities ? AbilitiesMeta : "")}}
          }
          """;

    private const string AbilitiesMeta =
        """
        , "meta": { "abilities": { "self": { "cancel": "ok", "delete": "state" } } }
        """;
}
