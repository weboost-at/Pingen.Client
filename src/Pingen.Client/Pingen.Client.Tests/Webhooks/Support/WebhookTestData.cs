using Pingen.Client.Webhooks.Payloads;

namespace Pingen.Client.Tests.Webhooks.Support;

// Interpolated into webhook documents and payloads, so no value needs JSON escaping
public class WebhookTestData(TestFaker faker)
{
    private static readonly string[] Urls =
    [
        "https://acme.example.com/hooks/pingen",
        "https://büro.example.ch/webhooks/pingen",
        "https://example.at/api/pingen/zustellung",
        "https://hooks.example.com/pingen?source=genève"
    ];

    private static readonly string[] IssueReasons =
    [
        "Content failed inspection",
        "Inhalt hat die Prüfung nicht bestanden",
        "Le contenu n’a pas passé l’inspection",
        "Adresse außerhalb des Zustellgebiets"
    ];

    private static readonly string[] UndeliverableReasons =
    [
        "Recipient could not be determined at the specified address.",
        "Empfänger unter der angegebenen Adresse nicht ermittelbar",
        "Destinataire inconnu à l’adresse indiquée",
        "Annahme verweigert"
    ];

    private static readonly string[] Names = ["Alex Meier 🇨🇭", "Jürgen Zürcher", "Zoë Müller-Lüdenscheidt", "Chloé Dubois"];

    private static readonly string[] Emails = ["jürgen@example.com", "zoë.müller@example.ch", "chloe@genève.example", "alex@example.at"];

    private static readonly string[] Streets = ["Example street", "Bahnhofstrasse", "Rue du Rhône", "Via Nassa"];

    // Each carries a precomposed letter, so a payload naming one changes its bytes when normalized to FormD
    private static readonly string[] Cities = ["Zürich", "Genève", "Neuchâtel", "Delémont"];

    public Guid Id { get; } = faker.Guid();

    public Guid EventId { get; } = faker.Guid();

    public Guid DeliverableId { get; } = faker.Guid();

    public Guid DeliveryEventId { get; } = faker.Guid();

    public Guid ChannelEbillId { get; } = faker.Guid();

    public string Url { get; } = faker.PickRandom(Urls);

    // Pingen takes signing keys of 20 to 32 characters
    public string SigningKey { get; } = faker.AlphaNumeric(faker.Int(20, 32));

    public string OtherSigningKey { get; } = faker.AlphaNumeric(32);

    public string IssueReason { get; } = faker.PickRandom(IssueReasons);

    public string UndeliverableReason { get; } = faker.PickRandom(UndeliverableReasons);

    public WebhookCorrectedAddress CorrectedAddress { get; } = new()
    {
        Name = faker.PickRandom(Names),
        Street = faker.PickRandom(Streets),
        Number = $"{faker.Int(1, 99)}A",
        Zip = $"{faker.Int(1000, 9999)}",
        City = faker.PickRandom(Cities)
    };

    public string Identifier { get; } = $"41{faker.Int(100_000, 999_999)}{faker.Int(100_000_000, 999_999_999)}";

    public string Name { get; } = faker.PickRandom(Names);

    public string Email { get; } = faker.PickRandom(Emails);
}
