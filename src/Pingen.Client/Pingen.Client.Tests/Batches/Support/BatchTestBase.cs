using Pingen.Client.Batches;
using Pingen.Client.Batches.ValueTypes;

namespace Pingen.Client.Tests.Batches.Support;

public abstract class BatchTestBase : TestBase
{
    protected BatchTestData Batch => Data.Batch;

    protected BatchService Batches => Resolve<BatchService>();

    protected string BatchesPath => $"/organisations/{Organisation.Id}/batches";

    protected string BatchPath => $"{BatchesPath}/{Batch.Id}";

    protected BatchCreateOptions BatchCreateOptions() =>
        new()
        {
            Name = Batch.Name,
            Icon = BatchIcon.WaveHand,
            FileOriginalName = Batch.FileOriginalName,
            GroupingType = BatchGroupingType.Zip,
            SplitType = BatchSplitType.File
        };

    protected string BatchDocument() =>
        $$"""{ "data": {{BatchResource(letters: 2)}} }""";

    protected string BatchPage(int number, int lastPage) =>
        $$"""
          {
            "data": [ {{BatchResource(letters: number, abilities: false)}} ],
            "links": {
              "first": "https://api.pingen.com{{BatchesPath}}?page[number]=1",
              "last": "https://api.pingen.com{{BatchesPath}}?page[number]={{lastPage}}",
              "next": "https://api.pingen.com{{BatchesPath}}?page[number]=2",
              "self": "https://api.pingen.com{{BatchesPath}}?page[number]={{number}}"
            },
            "meta": { "current_page": {{number}}, "last_page": {{lastPage}}, "per_page": 1, "from": {{number}}, "to": {{number}}, "total": 2 }
          }
          """;

    protected string BatchResource(int letters, bool abilities = true) =>
        $$"""
          {
            "id": "{{Batch.Id}}",
            "type": "batches",
            "attributes": {
              "name": "{{Batch.Name.Default}}",
              "channel_type": "post",
              "icon": "wave-hand",
              "status": "processing",
              "file_original_name": "{{Batch.FileOriginalName}}",
              "letter_count": {{letters}},
              "deliverable_count": {{letters}},
              "address_position": "left",
              "print_mode": "simplex",
              "print_spectrum": "color",
              "price_currency": "CHF",
              "price_value": 1.25,
              "source": "api",
              "submitted_at": "",
              "created_at": "2020-11-19T09:42:48+0100",
              "updated_at": "2020-11-20T10:00:00+0100"
            },
            "relationships": {
              "organisation": { "data": { "id": "{{Organisation.Id}}", "type": "organisations" } },
              "events": {
                "links": { "related": { "href": "https://api.pingen.com{{BatchPath}}/events", "meta": { "count": 3 } } }
              }
            },
            "links": { "self": "https://api.pingen.com{{BatchPath}}" }{{(abilities ? AbilitiesMeta : "")}}
          }
          """;

    protected string EventsDocument() =>
        $$"""
          {
            "data": [
              {
                "id": "{{Batch.EventId}}",
                "type": "batches_events",
                "attributes": {
                  "code": "processing",
                  "name": "Wird verarbeitet",
                  "producer": "Pingen",
                  "location": "8051 Zürich, CH",
                  "data": ["split"],
                  "emitted_at": "2021-11-19T09:42:48+0100",
                  "created_at": "2021-11-19T09:42:48+0100",
                  "updated_at": "2021-11-19T09:42:48+0100"
                },
                "relationships": {
                  "batch": { "data": { "id": "{{Batch.Id}}", "type": "batches" } }
                }
              }
            ],
            "meta": { "current_page": 1, "last_page": 1, "per_page": 20, "from": 1, "to": 1, "total": 1 }
          }
          """;

    protected string StatisticsDocument() =>
        $$"""
          {
            "data": {
              "id": "{{Batch.Id}}",
              "type": "batch_details_statistics",
              "attributes": {
                "letter_validating": 2,
                "letter_groups": [{ "name": "valid", "count": 3 }, { "name": "not_available", "count": 1 }],
                "letter_countries": [{ "country": "CH", "count": 3 }],
                "letter_regions": [{ "country": "CH", "count": 3 }]
              },
              "links": { "self": "https://api.pingen.com{{BatchPath}}/statistics" }
            }
          }
          """;

    private const string AbilitiesMeta =
        """
        , "meta": { "abilities": { "self": { "cancel": "ok", "delete": "state" } } }
        """;
}
