using System.Net;
using FluentAssertions;
using Pingen.Client.Common;
using Pingen.Client.Tests.Deliveries.Ebills.Support;

namespace Pingen.Client.Tests.Deliveries.Ebills;

public class EbillServiceTests : EbillTestBase
{
    [Fact]
    public async Task ListAsync_Gets_The_Ebills_And_Maps_The_Invoice_Dates()
    {
        // Arrange
        Host.Api.EnqueueOk(EbillPage(number: 1, lastPage: 1));

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
    public async Task Given_Two_Pages_ListAutoPagingAsync_Yields_Both_And_Stops_At_The_Last()
    {
        // Arrange
        Host.Api.EnqueueOk(EbillPage(number: 1, lastPage: 2)).EnqueueOk(EbillPage(number: 2, lastPage: 2));

        // Act
        var ebills = await Ebills
            .ListAutoPagingAsync(Organisation.Id, new() { PageLimit = 1, Sort = "-created_at" }, Ct)
            .ToListAsync(Ct);

        // Assert
        ebills.Select(ebill => ebill.Attributes.FilePages).Should().Equal(1, 2);

        Host.Api.Requests.Should().HaveCount(2);
        Host.Api.Requests[0].Assert.Sent(HttpMethod.Get, EbillsPath);
        Host.Api.Requests[0].Assert.Query("?page[limit]=1&sort=-created_at");
        Host.Api.Requests[1].Assert.Query("?page[number]=2&page[limit]=1&sort=-created_at");
    }

    [Fact]
    public async Task Given_An_Uploaded_File_CreateAsync_Posts_The_Invoice_Dates_As_Plain_Days()
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
    public async Task Given_A_Preset_And_A_Key_CreateAsync_Relates_The_Preset_And_Sends_The_Key()
    {
        // Arrange
        Host.Api.EnqueueJson(HttpStatusCode.Created, EbillDocument());

        var options = EbillCreateOptions() with
        {
            FileUrl = File.Url.ToString(),
            FileUrlSignature = File.Signature,
            PresetId = Ebill.PresetId
        };

        // Act
        await Ebills.CreateAsync(Organisation.Id, options, new() { IdempotencyKey = Request.IdempotencyKey }, Ct);

        // Assert
        Host.Api.Request.Header("Idempotency-Key").Should().Be(Request.IdempotencyKey);

        var preset = Host.Api.Request.Data.GetProperty("relationships").GetProperty("preset").GetProperty("data");
        preset.Assert.Has("type", "presets");
        preset.Assert.Has("id", Ebill.PresetId.ToString());
    }

    [Fact]
    public async Task Given_A_Stream_CreateAsync_Uploads_It_And_Posts_Its_Url_And_Signature()
    {
        // Arrange
        Host.Api.EnqueueOk(UploadDocument()).EnqueueJson(HttpStatusCode.Created, EbillDocument());
        Host.Files.EnqueueEmpty(HttpStatusCode.OK);

        using var content = new MemoryStream(File.Pdf);

        // Act
        await Ebills.CreateAsync(
            Organisation.Id,
            content,
            EbillCreateOptions(),
            new() { IdempotencyKey = Request.IdempotencyKey },
            Ct
        );

        // Assert
        Host.Api.Requests[0].Path.Should().Be("/file-upload");

        Host.Files.Request.Body.Should().Equal(File.Pdf);
        Host.Files.Request.Url.Should().Be(File.Url);
        Host.Files.Request.Method.Should().Be(HttpMethod.Put);
        Host.Files.Request.Assert.Unauthenticated();

        Host.Api.Requests[1].Path.Should().Be(EbillsPath);
        Host.Api.Requests[1].Attributes.Assert.Has("file_url", File.Url.ToString());
        Host.Api.Requests[1].Attributes.Assert.Has("file_url_signature", File.Signature);
        Host.Api.Requests[1].Header("Idempotency-Key").Should().Be(Request.IdempotencyKey);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task Given_An_Upload_Target_In_The_Options_CreateAsync_Refuses_The_Stream(bool url, bool signature)
    {
        // Arrange
        using var content = new MemoryStream(File.Pdf);

        var options = EbillCreateOptions() with
        {
            FileUrl = url ? File.Url.ToString() : null,
            FileUrlSignature = signature ? File.Signature : null
        };

        // Act
        var act = () => Ebills.CreateAsync(Organisation.Id, content, options, cancellationToken: Ct);

        // Assert
        await act.Should()
            .ThrowAsync<ArgumentException>()
            .WithParameterName("options")
            .WithMessage("*fills FileUrl and FileUrlSignature from the upload it performs - leave both unset*");

        Host.Api.Requests.Should().BeEmpty();
        Host.Files.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAsync_Gets_The_Ebill_And_Maps_The_Abilities()
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
    public async Task SendAsync_Patches_Send_Without_A_Body()
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
    public async Task DeleteAsync_Deletes_The_Ebill_Without_A_Body()
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
    public async Task CancelAsync_Patches_Cancel_Without_A_Body()
    {
        // Arrange
        Host.Api.EnqueueEmpty(HttpStatusCode.Accepted);

        // Act
        await Ebills.CancelAsync(Organisation.Id, Ebill.Id, new() { IdempotencyKey = Request.IdempotencyKey }, Ct);

        // Assert
        Host.Api.Request.Assert.Bodyless();
        Host.Api.Request.Assert.Sent(HttpMethod.Patch, $"{EbillPath}/cancel");
        Host.Api.Request.Header("Idempotency-Key").Should().Be(Request.IdempotencyKey);
    }

    [Fact]
    public async Task GetFileLocationAsync_Returns_The_Location_Unfollowed()
    {
        // Arrange
        Host.Api.EnqueueRedirect(File.Location);

        // Act
        var location = await Ebills.GetFileLocationAsync(Organisation.Id, Ebill.Id, Ct);

        // Assert
        location.Should().Be(File.Location);

        Host.Files.Requests.Should().BeEmpty();
        Host.Api.Request.Path.Should().Be($"{EbillPath}/file");
    }

    [Fact]
    public async Task DownloadFileAsync_Streams_The_Location_Unauthenticated()
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
    public async Task ListEventsAsync_Gets_The_Events_And_Maps_The_Parent()
    {
        // Arrange
        Host.Api.EnqueueOk(EventsDocument());

        var options = new PingenListOptions { Language = "de-DE", Sort = "-emitted_at" };

        // Act
        var events = await Ebills.ListEventsAsync(Organisation.Id, Ebill.Id, options, Ct);

        // Assert
        Host.Api.Request.Path.Should().Be($"{EbillPath}/events");
        Host.Api.Request.Assert.Query("?sort=-emitted_at&language=de-DE");

        events.Should().ContainSingle();
        events[0].Attributes.Code.Should().Be("approved");
        events[0].Relationships!.Parent!.Data!.Type.Should().Be("ebills");
    }

    [Fact]
    public async Task DownloadEventImageAsync_Streams_The_Image_Unauthenticated()
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
    public async Task GetEventImageLocationAsync_Returns_The_Location_Unfollowed()
    {
        // Arrange
        Host.Api.EnqueueRedirect(File.Location);

        // Act
        var location = await Ebills.GetEventImageLocationAsync(Organisation.Id, Ebill.Id, Ebill.EventId, Ct);

        // Assert
        location.Should().Be(File.Location);

        Host.Files.Requests.Should().BeEmpty();
        Host.Api.Request.Path.Should().Be($"{EbillPath}/events/{Ebill.EventId}/image");
    }

    [Fact]
    public async Task Given_A_Rejection_SendAsync_Throws_With_The_Parsed_Errors()
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
