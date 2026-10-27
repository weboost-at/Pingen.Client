using Pingen.Client.Deliveries.Letters;

namespace Pingen.Client.Tests.Deliveries.Letters.Support;

public abstract class LetterTestBase : TestBase
{
    protected LetterTestData Letter => Data.Letter;

    protected LetterService Letters => Resolve<LetterService>();

    protected string LettersPath => $"/organisations/{Organisation.Id}/deliveries/letters";

    protected string LetterPath => $"{LettersPath}/{Letter.Id}";

    protected string LetterDocument() =>
        $$"""{ "data": {{LetterResource(filePages: 2)}} }""";

    protected string LetterPage(int number, int lastPage) =>
        $$"""
          {
            "data": [ {{LetterResource(filePages: number, abilities: false)}} ],
            "links": {
              "first": "https://api.pingen.com{{LettersPath}}?page[number]=1",
              "last": "https://api.pingen.com{{LettersPath}}?page[number]={{lastPage}}",
              "next": "https://api.pingen.com{{LettersPath}}?page[number]=2",
              "self": "https://api.pingen.com{{LettersPath}}?page[number]={{number}}"
            },
            "meta": { "current_page": {{number}}, "last_page": {{lastPage}}, "per_page": 1, "from": {{number}}, "to": {{number}}, "total": 2 }
          }
          """;

    protected string LetterResource(int filePages, bool abilities = true) =>
        $$"""
          {
            "id": "{{Letter.Id}}",
            "type": "letters",
            "attributes": {
              "status": "sent",
              "file_original_name": "{{Letter.FileOriginalName}}",
              "file_pages": {{filePages}},
              "address": "Hans Meier\nExample street 4\n8000 Zürich\nSwitzerland",
              "address_position": "left",
              "country": "CH",
              "delivery_product": "fast",
              "print_mode": "simplex",
              "print_spectrum": "color",
              "price_currency": "CHF",
              "price_value": 1.25,
              "paper_types": ["normal", "qr"],
              "fonts": [
                { "name": "Helvetica", "is_embedded": true },
                { "name": "Helvetica-Bold", "is_embedded": 0 }
              ],
              "source": "api",
              "tracking_number": "98.1234.11",
              "submitted_at": "2021-11-19T09:42:48+0100",
              "created_at": "2020-11-19T09:42:48+0100",
              "updated_at": "2020-11-20T10:00:00+0100",
              "cover_page": "an attribute this client does not know"
            },
            "relationships": {
              "organisation": {
                "links": { "related": "https://api.pingen.com/organisations/{{Organisation.Id}}" },
                "data": { "id": "{{Organisation.Id}}", "type": "organisations" }
              },
              "batch": { "data": null },
              "events": {
                "links": { "related": { "href": "https://api.pingen.com{{LetterPath}}/events", "meta": { "count": 3 } } }
              }
            },
            "links": { "self": "https://api.pingen.com{{LetterPath}}" }{{(abilities ? AbilitiesMeta : "")}}
          }
          """;

    protected string EventsDocument() =>
        $$"""
          {
            "data": [
              {
                "id": "{{Letter.EventId}}",
                "type": "letters_events",
                "attributes": {
                  "code": "undeliverable",
                  "name": "Nicht zustellbar",
                  "producer": "Pingen",
                  "location": "8051 Zürich, CH",
                  "has_image": true,
                  "data": ["moved"],
                  "emitted_at": "2021-11-19T09:42:48+0100",
                  "created_at": "2021-11-19T09:42:48+0100",
                  "updated_at": "2021-11-19T09:42:48+0100"
                },
                "relationships": {
                  "letter": { "data": { "id": "{{Letter.Id}}", "type": "letters" } }
                }
              }
            ],
            "meta": { "current_page": 1, "last_page": 1, "per_page": 20, "from": 1, "to": 1, "total": 1 }
          }
          """;

    protected string PriceDocument() =>
        """
        {
          "data": {
            "id": "7f0b2c55-4444-4000-8000-000000000004",
            "type": "letter_price_calculator",
            "attributes": { "currency": "EUR", "price": 12.12 },
            "links": { "self": "https://api.pingen.com/price" }
          }
        }
        """;

    private const string AbilitiesMeta =
        """
        , "meta": { "abilities": { "self": { "cancel": "ok", "delete": "state", "send-simplex": "permission" } } }
        """;
}
