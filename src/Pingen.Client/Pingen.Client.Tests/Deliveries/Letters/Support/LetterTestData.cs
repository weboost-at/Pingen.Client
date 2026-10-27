using Pingen.Client.Deliveries.Letters;

namespace Pingen.Client.Tests.Deliveries.Letters.Support;

// Interpolated into letter documents, so no value needs JSON escaping
public class LetterTestData(TestFaker faker)
{
    private static readonly string[] FileNames = ["lörem.pdf", "Rechnung 2024-0815.pdf", "Mahnung Zürich.pdf", "facture_genève.pdf"];

    private static readonly string[] Names = ["Alex Meier", "Jürgen Zürcher", "Zoë Müller-Lüdenscheidt", "Chloé Dubois"];

    private static readonly string[] Cities = ["Zürich", "Genève", "Bern", "Lugano"];

    public Guid Id { get; } = faker.Guid();

    public Guid EventId { get; } = faker.Guid();

    public Guid PresetId { get; } = faker.Guid();

    public string FileOriginalName { get; } = faker.PickRandom(FileNames);

    public LetterMetaData MetaData { get; } = new()
    {
        Recipient = new()
        {
            Name = faker.PickRandom(Names),
            PoBox = $"Postfach {faker.Int(1, 999)}",
            Zip = $"{faker.Int(1000, 9999)}",
            City = faker.PickRandom(Cities),
            Country = "CH"
        },
        Sender = new()
        {
            Name = "Pingen AG",
            Street = "Example street",
            Number = $"{faker.Int(1, 99)}A",
            Zip = $"{faker.Int(1000, 9999)}",
            City = faker.PickRandom(Cities),
            Country = "CH"
        }
    };
}
