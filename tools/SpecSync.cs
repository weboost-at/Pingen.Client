using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

return await SpecSync.RunAsync(args);

// A zero-package net10 file-based app, deliberately outside the solution so `dotnet test` never compiles it.
static class SpecSync
{
    private const string DefaultSpecUrl = "https://api.pingen.com/documentation/swagger-docs";

    private const string WebhookPathPrefix = "/your-webhook-url-for-";

    private const string OperationKind = "operation";

    private const string WebhookPayloadKind = "webhook-payload";

    private const string Unmapped = "(unmapped)";

    private static readonly HashSet<string> HttpMethods = ["get", "put", "post", "delete", "patch", "head", "options", "trace"];

    // Prose and cosmetics - a reworded description must never fire a drift alarm.
    private static readonly HashSet<string> StrippedKeys = ["description", "summary", "example", "examples", "title"];

    // Objects whose property names are data rather than OpenAPI keywords - a schema property called `description` survives.
    private static readonly HashSet<string> NameMaps =
    [
        "properties", "patternProperties", "headers", "content", "responses", "callbacks", "links",
        "schemas", "securitySchemes", "variables", "mapping", "scopes", "encoding", "paths", "definitions",
    ];

    // Reflection-based serialization is off in file-based apps, so every document this tool writes goes out through Utf8JsonWriter.
    private static readonly JsonWriterOptions DocumentOptions = new()
    {
        Indented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private static readonly string ToolDirectory = ResolveToolDirectory();

    private static readonly string RepoRoot = Directory.GetParent(ToolDirectory)?.FullName ?? ToolDirectory;

    private static string ManifestPath => Path.Combine(ToolDirectory, "spec-manifest.json");

    private static string CachedSpecPath => Path.Combine(RepoRoot, ".tmp", "swagger-docs.json");

    private static string DriftPath => Path.Combine(RepoRoot, ".tmp", "spec-drift.json");

    public static async Task<int> RunAsync(string[] args)
    {
        try
        {
            var mode = args.Length > 0 ? args[0] : string.Empty;
            var spec = Option(args, "--spec");

            return mode switch
            {
                "check" => await CheckAsync(spec),
                "update" => await UpdateAsync(spec),
                "show" => await ShowAsync(args.Length > 1 && !args[1].StartsWith("--") ? args[1] : string.Empty, spec),
                _ => Usage(),
            };
        }
        catch (Unobtainable failure)
        {
            Console.Error.WriteLine(failure.Message);

            return 2;
        }
    }

    private static async Task<int> CheckAsync(string? spec)
    {
        var manifest = ReadManifest() ?? throw new Unobtainable($"manifest unobtainable: no file at {Relative(ManifestPath)} - run `update` to create it");
        using var document = Parse(await ReadSpecAsync(spec, manifest.SpecUrl));
        var scan = Scan(document.RootElement);

        return Report(manifest, scan, writeDrift: true);
    }

    private static async Task<int> UpdateAsync(string? spec)
    {
        var previous = ReadManifest();
        var bytes = await ReadSpecAsync(spec, previous?.SpecUrl ?? DefaultSpecUrl);
        using var document = Parse(bytes);
        var scan = Scan(document.RootElement);
        var known = previous?.Operations.ToDictionary(entry => entry.Id) ?? [];

        var manifest = new Manifest
        {
            SpecTitle = scan.Title,
            SpecVersion = scan.Version,
            SpecUrl = previous?.SpecUrl ?? DefaultSpecUrl,
            LastSync = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            GeneratedFrom = $"sha256:{Convert.ToHexStringLower(SHA256.HashData(bytes))}",
            AuthDigest = scan.AuthDigest,
            EmptyPathCount = scan.EmptyPathCount,
            Operations =
            [
                .. scan.Entries
                    .Select(scanned => known.TryGetValue(scanned.Entry.Id, out var carried) ? scanned.Entry with { Sdk = carried.Sdk, Notes = carried.Notes } : scanned.Entry)
                    .OrderBy(entry => entry.Id, StringComparer.Ordinal),
            ],
        };

        WriteManifest(manifest);

        var counts = manifest.Operations.CountBy(entry => entry.Kind).OrderBy(pair => pair.Key, StringComparer.Ordinal);
        Console.WriteLine($"WROTE    {Relative(ManifestPath)} - {manifest.Operations.Count} entries ({string.Join(", ", counts.Select(pair => $"{pair.Value} {pair.Key}"))})");

        // Self-verification: the manifest that was just written has to read back in sync against the very same spec.
        var written = ReadManifest() ?? throw new Unobtainable($"manifest unobtainable: {Relative(ManifestPath)} could not be read back");
        var verdict = Report(written, scan, writeDrift: false);
        if (verdict is not 0) Console.Error.WriteLine("the regenerated manifest does not verify against the spec it was generated from");

        return verdict;
    }

    private static async Task<int> ShowAsync(string id, string? spec)
    {
        if (id.Length is 0) return Usage();

        var manifest = ReadManifest();
        using var document = Parse(await ReadSpecAsync(spec, manifest?.SpecUrl ?? DefaultSpecUrl));
        var scan = Scan(document.RootElement);
        var scanned = scan.Entries.FirstOrDefault(candidate => candidate.Entry.Id == id);
        if (scanned is null)
        {
            Console.Error.WriteLine($"no operation with id '{id}' - `check` lists the ids the spec carries");

            return 1;
        }

        var known = manifest?.Operations.FirstOrDefault(entry => entry.Id == id);
        using var slice = JsonDocument.Parse(Canonical(scanned.Operation, document.RootElement));
        using var stream = Console.OpenStandardOutput();
        using var writer = new Utf8JsonWriter(stream, DocumentOptions);

        writer.WriteStartObject();
        writer.WriteString("id", scanned.Entry.Id);
        writer.WriteString("method", scanned.Entry.Method);
        writer.WriteString("path", scanned.Entry.Path);
        writer.WriteString("kind", scanned.Entry.Kind);
        writer.WriteString("digest", scanned.Entry.Digest);
        writer.WriteString("sdk", known?.Sdk);
        writer.WriteString("notes", known?.Notes);
        writer.WritePropertyName("operation");
        slice.RootElement.WriteTo(writer);
        writer.WriteEndObject();
        writer.Flush();
        Console.WriteLine();

        return 0;
    }

    private static int Usage()
    {
        Console.Error.WriteLine("usage: dotnet run tools/SpecSync.cs -- <check|update|show <id>> [--spec <path|url>]");

        return 2;
    }

    private static int Report(Manifest manifest, ScanResult scan, bool writeDrift)
    {
        var current = scan.Entries.ToDictionary(scanned => scanned.Entry.Id, scanned => scanned.Entry);
        var recorded = manifest.Operations.ToDictionary(entry => entry.Id);

        List<Entry> changed = [.. recorded.Values.Where(entry => current.TryGetValue(entry.Id, out var found) && found.Digest != entry.Digest).OrderBy(entry => entry.Id, StringComparer.Ordinal)];
        List<Entry> added = [.. current.Values.Where(entry => !recorded.ContainsKey(entry.Id)).OrderBy(entry => entry.Id, StringComparer.Ordinal)];
        List<Entry> removed = [.. recorded.Values.Where(entry => !current.ContainsKey(entry.Id)).OrderBy(entry => entry.Id, StringComparer.Ordinal)];

        var authChanged = manifest.AuthDigest != scan.AuthDigest;
        var stubCountChanged = manifest.EmptyPathCount != scan.EmptyPathCount;
        var matched = recorded.Values.Count(entry => current.TryGetValue(entry.Id, out var found) && found.Digest == entry.Digest);

        Console.WriteLine($"MATCH    {matched}");
        foreach (var entry in changed) Console.WriteLine($"CHANGED  {entry.Id}  {entry.Method} {entry.Path}  ->  {entry.Sdk ?? Unmapped}");
        foreach (var entry in added) Console.WriteLine($"ADDED    {entry.Id}  {entry.Method} {entry.Path}  ->  {Unmapped}");
        foreach (var entry in removed) Console.WriteLine($"REMOVED  {entry.Id}  {entry.Method} {entry.Path}  ->  {entry.Sdk ?? Unmapped}");
        Console.WriteLine($"NOTED    {recorded.Values.Count(entry => entry.Notes is not null)}");
        Console.WriteLine($"AUTH     {(authChanged ? "CHANGED" : "unchanged")}");
        Console.WriteLine($"STUBS    {(stubCountChanged ? $"{manifest.EmptyPathCount} -> {scan.EmptyPathCount} CHANGED" : $"{scan.EmptyPathCount} unchanged")}");

        var drifted = changed.Count + added.Count + removed.Count > 0 || authChanged || stubCountChanged;
        if (!drifted) return 0;

        if (writeDrift)
        {
            WriteDrift(changed, added, removed, current, authChanged, stubCountChanged);
            Console.WriteLine($"DRIFT    {Relative(DriftPath)}");
        }

        return 1;
    }

    private static void WriteDrift(List<Entry> changed, List<Entry> added, List<Entry> removed, Dictionary<string, Entry> current, bool authChanged, bool stubCountChanged)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(DriftPath)!);

        using var stream = File.Create(DriftPath);
        using var writer = new Utf8JsonWriter(stream, DocumentOptions);

        writer.WriteStartObject();
        WriteDriftGroup(writer, "changed", changed, current);
        WriteDriftGroup(writer, "added", added, current);
        WriteDriftGroup(writer, "removed", removed, current);
        writer.WriteBoolean("authChanged", authChanged);
        writer.WriteBoolean("stubCountChanged", stubCountChanged);
        writer.WriteEndObject();
        writer.Flush();
        stream.Write("\n"u8);
    }

    private static void WriteDriftGroup(Utf8JsonWriter writer, string name, List<Entry> entries, Dictionary<string, Entry> current)
    {
        writer.WriteStartArray(name);

        foreach (var entry in entries)
        {
            // A changed or added entry is described by the spec's method and path, a removed one by the manifest's.
            var live = current.GetValueOrDefault(entry.Id, entry);

            writer.WriteStartObject();
            writer.WriteString("id", entry.Id);
            writer.WriteString("method", live.Method);
            writer.WriteString("path", live.Path);
            WriteText(writer, "sdk", entry.Sdk);
            WriteText(writer, "notes", entry.Notes);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    private static void WriteManifest(Manifest manifest)
    {
        using var stream = File.Create(ManifestPath);
        using var writer = new Utf8JsonWriter(stream, DocumentOptions);

        writer.WriteStartObject();
        writer.WriteString("specTitle", manifest.SpecTitle);
        writer.WriteString("specVersion", manifest.SpecVersion);
        writer.WriteString("specUrl", manifest.SpecUrl);
        writer.WriteString("lastSync", manifest.LastSync);
        writer.WriteString("generatedFrom", manifest.GeneratedFrom);
        writer.WriteString("authDigest", manifest.AuthDigest);
        writer.WriteNumber("emptyPathCount", manifest.EmptyPathCount);
        writer.WriteStartArray("operations");

        foreach (var entry in manifest.Operations)
        {
            writer.WriteStartObject();
            writer.WriteString("id", entry.Id);
            writer.WriteString("method", entry.Method);
            writer.WriteString("path", entry.Path);
            writer.WriteString("kind", entry.Kind);
            writer.WriteString("digest", entry.Digest);
            WriteText(writer, "sdk", entry.Sdk);
            WriteText(writer, "notes", entry.Notes);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
        writer.Flush();
        stream.Write("\n"u8);
    }

    private static void WriteText(Utf8JsonWriter writer, string name, string? value)
    {
        if (value is null) writer.WriteNull(name);
        else writer.WriteString(name, value);
    }

    private static ScanResult Scan(JsonElement root)
    {
        List<Scanned> entries = [];
        var emptyPaths = 0;

        foreach (var path in root.GetProperty("paths").EnumerateObject())
        {
            var operations = path.Value.EnumerateObject().Where(member => HttpMethods.Contains(member.Name.ToLowerInvariant())).ToList();
            if (operations.Count is 0) emptyPaths++;

            foreach (var operation in operations)
            {
                var webhookPayload = path.Name.StartsWith(WebhookPathPrefix, StringComparison.Ordinal);

                var entry = new Entry
                {
                    Id = webhookPayload ? $"webhooks.payload.{path.Name[WebhookPathPrefix.Length..]}" : Text(operation.Value, "operationId") ?? $"{operation.Name.ToLowerInvariant()}:{path.Name}",
                    Method = operation.Name.ToUpperInvariant(),
                    Path = path.Name,
                    Kind = webhookPayload ? WebhookPayloadKind : OperationKind,
                    Digest = Digest(Canonical(operation.Value, root)),
                };

                entries.Add(new(entry, operation.Value));
            }
        }

        var info = root.GetProperty("info");
        var auth = root.GetProperty("components").GetProperty("securitySchemes");

        return new(
            Entries: entries,
            AuthDigest: Digest(Canonical(auth, root)),
            EmptyPathCount: emptyPaths,
            Title: Text(info, "title") ?? string.Empty,
            Version: Text(info, "version") ?? string.Empty
        );
    }

    private static string Digest(string canonical) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)))[..12];

    private static string Canonical(JsonElement element, JsonElement root)
    {
        var builder = new StringBuilder();
        Write(element, root, builder, [], arbitraryKeys: false);

        return builder.ToString();
    }

    private static void Write(JsonElement element, JsonElement root, StringBuilder builder, HashSet<string> activeRefs, bool arbitraryKeys)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                WriteObject(element, root, builder, activeRefs, arbitraryKeys);
                break;
            case JsonValueKind.Array:
                builder.Append('[');
                var first = true;
                foreach (var item in element.EnumerateArray())
                {
                    if (!first) builder.Append(',');
                    first = false;
                    Write(item, root, builder, activeRefs, arbitraryKeys: false);
                }

                builder.Append(']');
                break;
            case JsonValueKind.String:
                builder.Append(Quote(element.GetString() ?? string.Empty));
                break;
            default:
                builder.Append(element.GetRawText());
                break;
        }
    }

    private static void WriteObject(JsonElement element, JsonElement root, StringBuilder builder, HashSet<string> activeRefs, bool arbitraryKeys)
    {
        if (!arbitraryKeys && element.TryGetProperty("$ref", out var reference) && reference.ValueKind is JsonValueKind.String)
        {
            var pointer = reference.GetString() ?? string.Empty;

            // A revisited ref renders as a cycle marker instead of recursing forever - siblings of $ref are ignored, as OpenAPI 3.0 prescribes.
            if (!activeRefs.Add(pointer))
            {
                builder.Append("{\"$cycle\":").Append(Quote(pointer)).Append('}');

                return;
            }

            if (Resolve(pointer, root) is { } target) Write(target, root, builder, activeRefs, arbitraryKeys: false);
            else builder.Append("{\"$unresolved\":").Append(Quote(pointer)).Append('}');

            activeRefs.Remove(pointer);

            return;
        }

        List<JsonProperty> members = [.. element.EnumerateObject().Where(member => arbitraryKeys || !IsStripped(member.Name))];
        members.Sort((left, right) => string.CompareOrdinal(left.Name, right.Name));

        builder.Append('{');
        var first = true;
        foreach (var member in members)
        {
            if (!first) builder.Append(',');
            first = false;
            builder.Append(Quote(member.Name)).Append(':');

            if (!arbitraryKeys && member.Name is "parameters" && member.Value.ValueKind is JsonValueKind.Array) WriteParameters(member.Value, root, builder, activeRefs);
            else if (!arbitraryKeys && member.Name is "required" && member.Value.ValueKind is JsonValueKind.Array) WriteSorted(member.Value, root, builder, activeRefs);
            else Write(member.Value, root, builder, activeRefs, arbitraryKeys: !arbitraryKeys && NameMaps.Contains(member.Name));
        }

        builder.Append('}');
    }

    private static void WriteParameters(JsonElement parameters, JsonElement root, StringBuilder builder, HashSet<string> activeRefs)
    {
        List<(string In, string Name, string Json)> rendered = [];
        foreach (var parameter in parameters.EnumerateArray())
        {
            var scoped = new StringBuilder();
            Write(parameter, root, scoped, activeRefs, arbitraryKeys: false);
            var resolved = Follow(parameter, root);
            rendered.Add((Text(resolved, "in") ?? string.Empty, Text(resolved, "name") ?? string.Empty, scoped.ToString()));
        }

        rendered.Sort((left, right) =>
            string.CompareOrdinal(left.In, right.In) is var byIn and not 0 ? byIn
                : string.CompareOrdinal(left.Name, right.Name) is var byName and not 0 ? byName
                : string.CompareOrdinal(left.Json, right.Json));

        builder.Append('[').AppendJoin(',', rendered.Select(item => item.Json)).Append(']');
    }

    private static void WriteSorted(JsonElement array, JsonElement root, StringBuilder builder, HashSet<string> activeRefs)
    {
        List<string> rendered = [];
        foreach (var item in array.EnumerateArray())
        {
            var scoped = new StringBuilder();
            Write(item, root, scoped, activeRefs, arbitraryKeys: false);
            rendered.Add(scoped.ToString());
        }

        rendered.Sort(StringComparer.Ordinal);
        builder.Append('[').AppendJoin(',', rendered).Append(']');
    }

    private static JsonElement Follow(JsonElement element, JsonElement root)
    {
        for (var hop = 0; hop < 32 && element.ValueKind is JsonValueKind.Object && element.TryGetProperty("$ref", out var reference) && reference.ValueKind is JsonValueKind.String; hop++)
        {
            if (Resolve(reference.GetString() ?? string.Empty, root) is not { } target) break;
            element = target;
        }

        return element;
    }

    private static JsonElement? Resolve(string pointer, JsonElement root)
    {
        if (!pointer.StartsWith("#/", StringComparison.Ordinal)) return null;

        var current = root;
        foreach (var raw in pointer[2..].Split('/'))
        {
            var token = raw.Replace("~1", "/").Replace("~0", "~");
            if (current.ValueKind is JsonValueKind.Object && current.TryGetProperty(token, out var next)) current = next;
            else if (current.ValueKind is JsonValueKind.Array && int.TryParse(token, out var index) && index >= 0 && index < current.GetArrayLength()) current = current[index];
            else return null;
        }

        return current;
    }

    private static bool IsStripped(string name) => StrippedKeys.Contains(name) || name.StartsWith("x-", StringComparison.Ordinal);

    private static string Quote(string value) => $"\"{JsonEncodedText.Encode(value)}\"";

    private static string? Text(JsonElement element, string name) =>
        element.ValueKind is JsonValueKind.Object && element.TryGetProperty(name, out var value) && value.ValueKind is JsonValueKind.String ? value.GetString() : null;

    private static Manifest? ReadManifest()
    {
        if (!File.Exists(ManifestPath)) return null;

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllBytes(ManifestPath));
            var root = document.RootElement;

            return new()
            {
                SpecTitle = Text(root, "specTitle") ?? string.Empty,
                SpecVersion = Text(root, "specVersion") ?? string.Empty,
                SpecUrl = Text(root, "specUrl") ?? DefaultSpecUrl,
                LastSync = Text(root, "lastSync") ?? string.Empty,
                GeneratedFrom = Text(root, "generatedFrom") ?? string.Empty,
                AuthDigest = Text(root, "authDigest") ?? string.Empty,
                EmptyPathCount = root.TryGetProperty("emptyPathCount", out var stubs) && stubs.TryGetInt32(out var count) ? count : -1,
                Operations =
                [
                    .. root.GetProperty("operations").EnumerateArray().Select(entry => new Entry
                    {
                        Id = Text(entry, "id") ?? string.Empty,
                        Method = Text(entry, "method") ?? string.Empty,
                        Path = Text(entry, "path") ?? string.Empty,
                        Kind = Text(entry, "kind") ?? OperationKind,
                        Digest = Text(entry, "digest") ?? string.Empty,
                        Sdk = Text(entry, "sdk"),
                        Notes = Text(entry, "notes"),
                    }),
                ],
            };
        }
        catch (Exception failure) when (failure is JsonException or KeyNotFoundException)
        {
            throw new Unobtainable($"manifest unobtainable: {Relative(ManifestPath)} is not a valid manifest - {Flatten(failure)}");
        }
    }

    private static JsonDocument Parse(byte[] bytes)
    {
        try
        {
            return JsonDocument.Parse(bytes);
        }
        catch (JsonException failure)
        {
            throw new Unobtainable($"spec unobtainable: the answer is not valid JSON - {Flatten(failure)}");
        }
    }

    private static async Task<byte[]> ReadSpecAsync(string? spec, string specUrl)
    {
        if (spec is not null)
        {
            if (IsUrl(spec)) return await DownloadAsync(spec, saveTo: null);
            if (!File.Exists(spec)) throw new Unobtainable($"spec unobtainable: no file at {spec}");

            return await File.ReadAllBytesAsync(spec);
        }

        if (File.Exists(CachedSpecPath)) return await File.ReadAllBytesAsync(CachedSpecPath);

        return await DownloadAsync(specUrl, saveTo: CachedSpecPath);
    }

    private static async Task<byte[]> DownloadAsync(string url, string? saveTo)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(120) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Pingen.Client-SpecSync/1.0");

        try
        {
            using var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode) throw new Unobtainable($"spec unobtainable: {url} answered {(int)response.StatusCode} {response.ReasonPhrase}");

            var bytes = await response.Content.ReadAsByteArrayAsync();
            if (saveTo is not null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(saveTo)!);
                await File.WriteAllBytesAsync(saveTo, bytes);
            }

            return bytes;
        }
        catch (HttpRequestException failure)
        {
            throw new Unobtainable($"spec unobtainable: {url} could not be fetched - {Flatten(failure)}");
        }
        catch (TaskCanceledException)
        {
            throw new Unobtainable($"spec unobtainable: {url} did not answer within 120 seconds");
        }
    }

    private static bool IsUrl(string value) => value.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || value.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

    // One line, never a stack trace - the scheduled run reports the reason and stops.
    private static string Flatten(Exception failure)
    {
        while (failure.InnerException is { } inner) failure = inner;

        return failure.Message.ReplaceLineEndings(" ").Trim();
    }

    private static string Relative(string path) => Path.GetRelativePath(RepoRoot, path).Replace('\\', '/');

    private static string? Option(string[] args, string name)
    {
        var index = Array.IndexOf(args, name);

        return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
    }

    private static string ResolveToolDirectory([CallerFilePath] string source = "") =>
        Path.GetDirectoryName(source) is { } directory && Directory.Exists(directory) ? directory : Directory.GetCurrentDirectory();

    private class Unobtainable(string reason) : Exception(reason);

    private record Scanned(Entry Entry, JsonElement Operation);

    private record ScanResult(List<Scanned> Entries, string AuthDigest, int EmptyPathCount, string Title, string Version);

    private record Manifest
    {
        public required string SpecTitle { get; init; }

        public required string SpecVersion { get; init; }

        public required string SpecUrl { get; init; }

        public required string LastSync { get; init; }

        public required string GeneratedFrom { get; init; }

        public required string AuthDigest { get; init; }

        public required int EmptyPathCount { get; init; }

        public required List<Entry> Operations { get; init; }
    }

    private record Entry
    {
        public required string Id { get; init; }

        public required string Method { get; init; }

        public required string Path { get; init; }

        public required string Kind { get; init; }

        public required string Digest { get; init; }

        public string? Sdk { get; init; }

        public string? Notes { get; init; }
    }
}
