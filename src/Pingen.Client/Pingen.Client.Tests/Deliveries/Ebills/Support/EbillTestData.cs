using Pingen.Client.Deliveries.Ebills;

namespace Pingen.Client.Tests.Deliveries.Ebills.Support;

// Interpolated into ebill documents, so no value needs JSON escaping
public class EbillTestData(TestFaker faker)
{
    private static readonly string[] FileNames =
        ["rechnung_zürich.pdf", "Rechnung 2025-0815.pdf", "facture_genève.pdf", "fattura_lugano.pdf"];

    private static readonly string[] InvoicePrefixes = ["Rechnung", "Facture n°", "Fattura", "Invoice"];

    public Guid Id { get; } = faker.Guid();

    public Guid EventId { get; } = faker.Guid();

    public string FileOriginalName { get; } = faker.PickRandom(FileNames);

    // Two disjoint halves of the year, so the due date never precedes the invoice date
    public EbillMetaData MetaData { get; } = new()
    {
        InvoiceNumber = $"{faker.PickRandom(InvoicePrefixes)} {faker.Int(1000, 9999)}",
        InvoiceDate = new DateOnly(2025, 1, 1).AddDays(faker.Int(0, 180)),
        InvoiceDueDate = new DateOnly(2025, 7, 1).AddDays(faker.Int(0, 180)),
        RecipientIdentifier = $"41100{faker.Int(100_000, 999_999)}{faker.Int(100_000, 999_999)}"
    };
}
