using Pingen.Client.Deliveries.Ebills;

namespace Pingen.Client.Tests.Deliveries.Ebills.Support;

public abstract class EbillTestBase : TestBase
{
    protected EbillTestData Ebill => Data.Ebill;

    protected EbillService Ebills => Resolve<EbillService>();

    protected string EbillsPath => $"/organisations/{Organisation.Id}/deliveries/ebills";

    protected string EbillPath => $"{EbillsPath}/{Ebill.Id}";

    protected EbillCreateOptions EbillCreateOptions() =>
        new()
        {
            FileOriginalName = Ebill.FileOriginalName,
            AutoSend = false,
            MetaData = Ebill.MetaData
        };

    protected string EbillDocument() =>
        $$"""{ "data": {{EbillResource(filePages: 2)}} }""";

    protected string EbillPage(int number, int lastPage) =>
        $$"""
          {
            "data": [ {{EbillResource(filePages: number, abilities: false)}} ],
            "links": { "self": "https://api.pingen.com{{EbillsPath}}?page[number]={{number}}" },
            "meta": { "current_page": {{number}}, "last_page": {{lastPage}}, "per_page": 1, "from": {{number}}, "to": {{number}}, "total": {{lastPage}} }
          }
          """;

    protected string EbillResource(int filePages, bool abilities = true) =>
        $$"""
          {
            "id": "{{Ebill.Id}}",
            "type": "ebills",
            "attributes": {
              "status": "submitted",
              "file_original_name": "{{Ebill.FileOriginalName}}",
              "file_pages": {{filePages}},
              "recipient_identifier": "41100010014282213",
              "recipient_address": "ACME GmbH\nExamplestreet 432\n3000 Bern",
              "invoice_number": "Invoice 8051",
              "invoice_date": "2025-10-01",
              "invoice_due_date": "2025-10-30",
              "invoice_value": 1250.3,
              "invoice_currency": "CHF",
              "invoice_iban": "CH8009000000854254426",
              "invoice_address": "ACME GmbH\nExamplestreet 432\n3000 Bern",
              "invoice_reference": "111119346200000000000127257",
              "price_currency": "CHF",
              "price_value": 1.25,
              "source": "api",
              "submitted_at": null,
              "created_at": "2020-11-19T09:42:48+0100",
              "updated_at": "2020-11-20T10:00:00+0100"
            },
            "relationships": {
              "organisation": { "data": { "id": "{{Organisation.Id}}", "type": "organisations" } },
              "events": {
                "links": { "related": { "href": "https://api.pingen.com{{EbillPath}}/events", "meta": { "count": 2 } } }
              }
            },
            "links": { "self": "https://api.pingen.com{{EbillPath}}" }{{(abilities ? AbilitiesMeta : "")}}
          }
          """;

    protected string EventsDocument() =>
        $$"""
          {
            "data": [
              {
                "id": "{{Ebill.EventId}}",
                "type": "deliverables_events",
                "attributes": {
                  "code": "approved",
                  "name": "Approved",
                  "producer": "Pingen",
                  "location": "8051 Zürich, CH",
                  "has_image": false,
                  "data": [],
                  "emitted_at": "2021-11-19T09:42:48+0100",
                  "created_at": "2021-11-19T09:42:48+0100",
                  "updated_at": "2021-11-19T09:42:48+0100"
                },
                "relationships": {
                  "ebill": { "data": { "id": "{{Ebill.Id}}", "type": "ebills" } }
                }
              }
            ],
            "meta": { "current_page": 1, "last_page": 1, "per_page": 20, "from": 1, "to": 1, "total": 1 }
          }
          """;

    private const string AbilitiesMeta =
        """
        , "meta": { "abilities": { "self": { "submit": "ok", "cancel": "state" } } }
        """;
}
