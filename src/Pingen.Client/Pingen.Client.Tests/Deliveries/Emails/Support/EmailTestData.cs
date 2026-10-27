using Pingen.Client.Deliveries.Emails;

namespace Pingen.Client.Tests.Deliveries.Emails.Support;

// Interpolated into email documents, so no value needs JSON escaping - the line breaks of the body only meet the serializer
public class EmailTestData(TestFaker faker)
{
    private static readonly string[] FileNames = ["rechnung_zürich.pdf", "Rechnung 2024-0815.pdf", "Mahnung Zürich.pdf", "facture_genève.pdf"];

    private static readonly string[] Senders = ["ACME AG", "Müller & Söhne GmbH", "Café Crème Sàrl", "Bäckerei Zürcher AG"];

    private static readonly string[] Recipients = ["Alex Meier", "Jürgen Zürcher", "Zoë Müller-Lüdenscheidt", "Chloé Dubois"];

    private static readonly string[] ReplyNames = ["ACME Support", "Kundendienst Müller", "Service clientèle", "Équipe Facturation"];

    private static readonly string[] Mailboxes = ["info", "buchhaltung", "zoë.müller", "chloé.dubois"];

    private static readonly string[] ReplyMailboxes = ["support", "kundendienst", "réponse", "no-reply"];

    private static readonly string[] Domains = ["acme.com", "müller-söhne.ch", "example.ch", "café-crème.fr"];

    private static readonly string[] Subjects = ["Ihre Rechnung", "Votre facture n° 2024-0815", "Mahnung – 2. Stufe", "Ihre Bestellung bei Café Crème"];

    private static readonly string[] Bodies =
    [
        "Guten Tag\n\nAnbei Ihre Rechnung.",
        "Bonjour,\n\nveuillez trouver ci-joint votre facture.",
        "Grüezi\r\n\r\nIm Anhang die Mahnung.",
        "Anbei die Bestätigung 📎"
    ];

    private static readonly string[] Searches = ["Zürich", "Müller AG", "Genève", "Crème brûlée"];

    public Guid Id { get; } = faker.Guid();

    public Guid EventId { get; } = faker.Guid();

    public Guid PresetId { get; } = faker.Guid();

    public string FileOriginalName { get; } = faker.PickRandom(FileNames);

    public string Search { get; } = faker.PickRandom(Searches);

    public EmailMetaData MetaData { get; } = new()
    {
        SenderName = faker.PickRandom(Senders),
        RecipientEmail = $"{faker.PickRandom(Mailboxes)}@{faker.PickRandom(Domains)}",
        RecipientName = faker.PickRandom(Recipients),
        ReplyEmail = $"{faker.PickRandom(ReplyMailboxes)}@{faker.PickRandom(Domains)}",
        ReplyName = faker.PickRandom(ReplyNames),
        Subject = faker.PickRandom(Subjects),
        Content = faker.PickRandom(Bodies)
    };
}
