using System.Net;
using FluentAssertions;
using Pingen.Client.Tests.Deliveries.Ebills.Support;

namespace Pingen.Client.Tests.Deliveries.Ebills;

public class EbillServiceTests : EbillTestBase
{
    [Fact]
    public async Task When_the_ebills_of_an_organisation_are_listed_ListAsync_gets_them_and_maps_the_invoice_dates()
    {
        // Arrange
        Host.Api.EnqueueOk(EbillPage());

        // Act
        var ebills = await Ebills.ListAsync(Organisation.Id, new() { PageLimit = 100, Sort = "-created_at" }, Ct);

        // Assert
        Host.Api.Request.Assert.Sent(HttpMethod.Get, EbillsPath);
        Host.Api.Request.Assert.Query("?page[limit]=100&sort=-created_at");

        ebills.Should().ContainSingle();
        ebills.Meta!.Total.Should().Be(1);

        ebills[0].Meta.Should().BeNull();
        ebills[0].Id.Should().Be(Ebill.Id);
        ebills[0].Type.Should().Be("ebills");
        ebills[0].Attributes.PriceValue.Should().Be(1.25m);
        ebills[0].Attributes.SubmittedAt.Should().BeNull();
        ebills[0].Attributes.Status.Should().Be("submitted");
        ebills[0].Attributes.InvoiceValue.Should().Be(1250.3m);
        ebills[0].Attributes.InvoiceNumber.Should().Be("Invoice 8051");
        ebills[0].Attributes.InvoiceDate.Should().Be(new(2025, 10, 1));
        ebills[0].Attributes.InvoiceDueDate.Should().Be(new(2025, 10, 30));
        ebills[0].Attributes.InvoiceIban.Should().Be("CH8009000000854254426");
        ebills[0].Attributes.InvoiceReference.Should().Be("111119346200000000000127257");
        ebills[0].Attributes.RecipientAddress.Should().Be("ACME GmbH\nExamplestreet 432\n3000 Bern");

        ebills[0].Relationships!.Batch.Should().BeNull();
        ebills[0].Relationships!.Events!.Count.Should().Be(2);
    }

    [Fact]
    public async Task When_a_file_is_already_uploaded_CreateAsync_posts_the_invoice_dates_as_plain_days()
    {
        // Arrange
        Host.Api.EnqueueJson(HttpStatusCode.Created, EbillDocument());

        var options = EbillCreateOptions() with { FileUrl = File.Url.ToString(), FileUrlSignature = File.Signature };

        // Act
        var ebill = await Ebills.CreateAsync(Organisation.Id, options, cancellationToken: Ct);

        // Assert
        ebill.Meta!.Abilities.Should().Contain(new KeyValuePair<string, string>("submit", "ok"));

        Host.Api.Request.Assert.Sent(HttpMethod.Post, EbillsPath);
        Host.Api.Request.Header("Content-Type").Should().Be(PingenClient.JsonApiMediaType);

        Host.Api.Request.Data.Assert.Lacks("id");
        Host.Api.Request.Data.Assert.Has("type", "ebills");
        Host.Api.Request.Data.Assert.Lacks("relationships");
        Host.Api.Request.Attributes.Assert.Has("file_url_signature", File.Signature);
        Host.Api.Request.Attributes.GetProperty("auto_send").GetBoolean().Should().BeFalse();

        var metaData = Host.Api.Request.Attributes.GetProperty("meta_data");
        metaData.Assert.Has("invoice_number", Ebill.MetaData.InvoiceNumber);
        metaData.Assert.Has("recipient_identifier", Ebill.MetaData.RecipientIdentifier);
        metaData.Assert.Has("invoice_date", Ebill.MetaData.InvoiceDate.ToString("O"));
        metaData.Assert.Has("invoice_due_date", Ebill.MetaData.InvoiceDueDate.ToString("O"));
    }

    [Fact]
    public async Task When_no_url_is_known_yet_CreateAsync_uploads_the_stream_and_posts_its_url_and_signature()
    {
        // Arrange
        Host.Api.EnqueueOk(UploadDocument()).EnqueueJson(HttpStatusCode.Created, EbillDocument());
        Host.Files.EnqueueEmpty(HttpStatusCode.OK);

        using var content = new MemoryStream(File.Pdf);

        // Act
        await Ebills.CreateAsync(Organisation.Id, content, EbillCreateOptions(), cancellationToken: Ct);

        // Assert
        Host.Api.Requests[0].Path.Should().Be("/file-upload");

        Host.Files.Request.Body.Should().Equal(File.Pdf);
        Host.Files.Request.Url.Should().Be(File.Url);
        Host.Files.Request.Method.Should().Be(HttpMethod.Put);
        Host.Files.Request.Assert.Unauthenticated();

        Host.Api.Requests[1].Path.Should().Be(EbillsPath);
        Host.Api.Requests[1].Attributes.Assert.Has("file_url", File.Url.ToString());
        Host.Api.Requests[1].Attributes.Assert.Has("file_url_signature", File.Signature);
    }

    [Fact]
    public async Task When_the_options_already_carry_an_upload_CreateAsync_with_a_stream_refuses_to_overwrite_it()
    {
        // Arrange
        using var content = new MemoryStream(File.Pdf);

        var options = EbillCreateOptions() with { FileUrlSignature = File.Signature };

        // Act
        var act = () => Ebills.CreateAsync(Organisation.Id, content, options, cancellationToken: Ct);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("options");

        Host.Api.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task When_one_ebill_is_addressed_GetAsync_gets_it_and_maps_the_abilities()
    {
        // Arrange
        Host.Api.EnqueueOk(EbillDocument());

        // Act
        var ebill = await Ebills.GetAsync(Organisation.Id, Ebill.Id, Ct);

        // Assert
        Host.Api.Request.Assert.Sent(HttpMethod.Get, EbillPath);

        ebill.Meta!.Abilities
            .Should().BeEquivalentTo(new Dictionary<string, string> { ["submit"] = "ok", ["cancel"] = "state" });
    }

    [Fact]
    public async Task When_an_ebill_was_created_without_auto_send_SendAsync_patches_send_without_a_body()
    {
        // Arrange
        Host.Api.EnqueueOk(EbillDocument());

        // Act
        var ebill = await Ebills.SendAsync(Organisation.Id, Ebill.Id, new() { IdempotencyKey = Request.IdempotencyKey }, Ct);

        // Assert
        ebill.Id.Should().Be(Ebill.Id);
        ebill.Attributes.InvoiceDate.Should().Be(new(2025, 10, 1));

        Host.Api.Request.Assert.Bodyless();
        Host.Api.Request.Header("Content-Type").Should().BeNull();
        Host.Api.Request.Assert.Sent(HttpMethod.Patch, $"{EbillPath}/send");
        Host.Api.Request.Header("Idempotency-Key").Should().Be(Request.IdempotencyKey);
    }

    [Fact]
    public async Task When_an_ebill_is_dropped_DeleteAsync_deletes_it_without_a_body()
    {
        // Arrange
        Host.Api.EnqueueEmpty();

        // Act
        await Ebills.DeleteAsync(Organisation.Id, Ebill.Id, Ct);

        // Assert
        Host.Api.Request.Assert.Bodyless();
        Host.Api.Request.Assert.Sent(HttpMethod.Delete, EbillPath);
    }

    [Fact]
    public async Task When_an_ebill_is_still_cancellable_CancelAsync_patches_cancel_without_a_body()
    {
        // Arrange
        Host.Api.EnqueueEmpty(HttpStatusCode.Accepted);

        // Act
        await Ebills.CancelAsync(Organisation.Id, Ebill.Id, cancellationToken: Ct);

        // Assert
        Host.Api.Request.Assert.Bodyless();
        Host.Api.Request.Assert.Sent(HttpMethod.Patch, $"{EbillPath}/cancel");
    }

    [Fact]
    public async Task When_the_file_endpoint_redirects_DownloadFileAsync_streams_the_location_unauthenticated()
    {
        // Arrange
        Host.Api.EnqueueRedirect(File.Location);
        Host.Files.EnqueueBytes(File.Pdf);

        using var downloaded = new MemoryStream();

        // Act
        await using var file = await Ebills.DownloadFileAsync(Organisation.Id, Ebill.Id, Ct);
        await file.CopyToAsync(downloaded, Ct);

        // Assert
        downloaded.ToArray().Should().Equal(File.Pdf);

        Host.Api.Request.Path.Should().Be($"{EbillPath}/file");

        Host.Files.Request.Url.Should().Be(File.Location);
        Host.Files.Request.Assert.Unauthenticated();
    }

    [Fact]
    public async Task When_the_events_of_an_ebill_are_listed_ListEventsAsync_gets_them_and_maps_the_parent()
    {
        // Arrange
        Host.Api.EnqueueOk(EventsDocument());

        // Act
        var events = await Ebills.ListEventsAsync(Organisation.Id, Ebill.Id, cancellationToken: Ct);

        // Assert
        Host.Api.Request.Path.Should().Be($"{EbillPath}/events");

        events.Should().ContainSingle();
        events[0].Attributes.Code.Should().Be("approved");
        events[0].Relationships!.Parent!.Data!.Type.Should().Be("ebills");
    }

    [Fact]
    public async Task When_an_event_image_is_requested_DownloadEventImageAsync_streams_it_unauthenticated()
    {
        // Arrange
        Host.Api.EnqueueRedirect(File.Location);
        Host.Files.EnqueueBytes(File.Pdf);

        using var downloaded = new MemoryStream();

        // Act
        await using var image = await Ebills.DownloadEventImageAsync(Organisation.Id, Ebill.Id, Ebill.EventId, Ct);
        await image.CopyToAsync(downloaded, Ct);

        // Assert
        downloaded.ToArray().Should().Equal(File.Pdf);

        Host.Api.Request.Path.Should().Be($"{EbillPath}/events/{Ebill.EventId}/image");

        Host.Files.Request.Url.Should().Be(File.Location);
        Host.Files.Request.Assert.Unauthenticated();
    }

    [Fact]
    public async Task When_the_ebill_cannot_be_sent_SendAsync_throws_with_the_parsed_errors()
    {
        // Arrange
        Host.Api.Enqueue(Rejection(HttpStatusCode.UnprocessableContent));

        // Act
        var act = () => Ebills.SendAsync(Organisation.Id, Ebill.Id, cancellationToken: Ct);

        // Assert
        var exception = await act.Assert.ThrowsAsync(HttpStatusCode.UnprocessableContent);
        exception.Errors.Should().ContainSingle();
        exception.Errors[0].Title.Should().Be(Error.Title);
    }
}
