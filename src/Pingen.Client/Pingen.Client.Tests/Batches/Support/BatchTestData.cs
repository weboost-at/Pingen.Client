namespace Pingen.Client.Tests.Batches.Support;

// Interpolated into batch documents, so no value needs JSON escaping
public class BatchTestData(TestFaker faker)
{
    // Two disjoint halves, so a rename never repeats the name the batch was created with
    private static readonly string[] CreatedNames = ["Rechnungslauf Zürich", "Mahnungen Q4", "Relevés Genève", "Kündigungen 2024"];

    private static readonly string[] UpdatedNames = ["Rechnungslauf Bern", "Jahresabschluss Lugano", "Facturation été", "Ölwechsel-Aktion"];

    private static readonly string[] FileNames = ["lörem.zip", "Rechnungen 2024-0815.zip", "Mahnungen Zürich.zip", "factures_genève.pdf"];

    private static readonly string[] Separators = ["Trennblatt", "Séparateur", "Separatore", "Nächste Sendung"];

    public Guid Id { get; } = faker.Guid();

    public Guid EventId { get; } = faker.Guid();

    public Guid PresetId { get; } = faker.Guid();

    public TestValue<string> Name { get; } = new(
        @default: faker.PickRandom(CreatedNames),
        updated: faker.PickRandom(UpdatedNames)
    );

    public string FileOriginalName { get; } = faker.PickRandom(FileNames);

    public string SplitSeparator { get; } = faker.PickRandom(Separators);

    public byte[] Archive { get; } = [.. "PK\u0003\u0004 Zürich "u8, .. faker.Bytes(32)];
}
