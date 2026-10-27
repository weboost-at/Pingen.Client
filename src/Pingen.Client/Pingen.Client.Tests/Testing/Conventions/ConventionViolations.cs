using System.Reflection;
using System.Text.RegularExpressions;

namespace Pingen.Client.Tests.Testing.Conventions;

public static partial class ConventionViolations
{
    private const string RootNamespace = "Pingen.Client.Tests";

    public static IReadOnlyDictionary<string, Regex> BannedCalls { get; } = new Dictionary<string, Regex>
    {
        ["Task.Delay"] = new(@"\bTask\.Delay\("),
        ["Thread.Sleep"] = new(@"\bThread\.Sleep\("),
        ["#region"] = new(@"^\s*#(end)?region\b"),
        ["sealed"] = new(@"\bsealed\b"),
        ["XML doc"] = new(@"^\s*///"),
        ["new Random"] = new(@"\bnew Random\("),
        ["raw cancellation token"] = new(@"TestContext\.Current\.CancellationToken"),
        ["own test host"] = new(@"\bnew PingenTestHost\("),
        ["wall clock"] = new(@"\bDateTime(Offset)?\.(Now|UtcNow|Today)\b"),
        ["intermediate subject"] = new(@"\)\.Which\b|\)\.And\.|\)\.Subject\b")
    };

    // Class.Method of every test whose name is neither Given_<state>_<outcome> nor the outcome alone
    public static IEnumerable<string> Names() =>
        TestSources.TestMethods
            .Where(method => !IsWellNamed(method))
            .Select(method => $"{method.DeclaringType!.Name}.{method.Name}")
            .Distinct();

    // file: Method of every test declaration line past 120 columns
    public static IEnumerable<string> LongDeclarations() =>
        TestSources.Files.SelectMany(file => TestSources.Lines(file)
            .Where(line => line.Length > 120)
            .Select(line => TestDeclaration().Match(line))
            .Where(match => match.Success)
            .Select(match => $"{file}: {match.Groups["name"].Value}"));

    // Namespace.Class of every test class outside <namespace as folders>/<Class>.cs
    public static IEnumerable<string> Misplaced() =>
        TestSources.TestMethods
            .Select(method => method.DeclaringType!)
            .Distinct()
            .Where(type => !IsInItsOwnFile(type))
            .Select(type => type.FullName!);

    // file: Method of every test body holding a comment other than Arrange, Act or Assert
    public static IEnumerable<string> Comments() =>
        TestSources.Files.SelectMany(file => TestBodies(TestSources.Lines(file))
            .Where(body => body.Lines.Any(IsForeignComment))
            .Select(body => $"{file}: {body.Name}"));

    // file: Type of every top-level type but a test class outside the slices' Support folders
    public static IEnumerable<string> SupportOutsideSupport() =>
        TestSources.Files
            .Where(file => !file.Contains("/Support/"))
            .SelectMany(file => TestSources.Lines(file)
                .Select(line => TopLevelType().Match(line))
                .Where(match => match.Success && !match.Groups["name"].Value.EndsWith("Tests"))
                .Select(match => $"{file}: {match.Groups["name"].Value}"));

    // file: banned call
    // An assertions class is the one place a road may hand its end on, so it may use .Which and .Subject
    public static IEnumerable<string> Banned(string call) =>
        TestSources.Files
            .Where(file => call is not "intermediate subject" || !file.EndsWith("Assertions.cs"))
            .Where(file => TestSources.Lines(file).Any(line => BannedCalls[call].IsMatch(line)))
            .Select(file => $"{file}: {call}");

    private static bool IsWellNamed(MethodInfo method) =>
        WellFormedName().IsMatch(method.Name)
        && !method.Name.StartsWith("When_")
        && !method.Name.StartsWith($"{method.DeclaringType!.Name.Replace("Tests", "")}_");

    private static bool IsInItsOwnFile(Type type) =>
        TestSources.Files.Contains($"{type.Namespace!.Replace($"{RootNamespace}.", "").Replace('.', '/')}/{type.Name}.cs")
        || type.Namespace == RootNamespace && TestSources.Files.Contains($"{type.Name}.cs")
        || type.Namespace!.StartsWith($"{RootNamespace}.Testing");

    private static bool IsForeignComment(string line) =>
        line.Trim() is var trimmed && trimmed.StartsWith("//") && trimmed is not ("// Arrange" or "// Act" or "// Assert");

    // The lines between a [Fact] or [Theory] method's braces, found by counting them
    private static IEnumerable<(string Name, List<string> Lines)> TestBodies(string[] lines)
    {
        for (var index = 0; index < lines.Length; index++)
        {
            if (!TestAttribute().IsMatch(lines[index])) continue;

            var declaration = Array.FindIndex(lines, index, line => TestDeclaration().IsMatch(line));
            if (declaration < 0) yield break;

            var name = TestDeclaration().Match(lines[declaration]).Groups["name"].Value;
            var body = new List<string>();
            var depth = 0;
            var opened = false;

            for (index = declaration + 1; index < lines.Length; index++)
            {
                depth += lines[index].Count(character => character == '{');
                depth -= lines[index].Count(character => character == '}');
                opened |= depth > 0;
                body.Add(lines[index]);

                if (opened && depth == 0 || !opened && lines[index].TrimEnd().EndsWith(';')) break;
            }

            yield return (name, body);
        }
    }

    [GeneratedRegex(@"^(Given_)?[A-Z][A-Za-z0-9]*(_[A-Z0-9][A-Za-z0-9]*)*$")]
    private static partial Regex WellFormedName();

    [GeneratedRegex(@"^\s*public (async )?(Task|void|ValueTask) (?<name>\w+)\(")]
    private static partial Regex TestDeclaration();

    // Top-level types start in the first column, nested ones are indented
    [GeneratedRegex(@"^(public |internal |file )?((static|abstract|partial) )*(class|record|struct|interface|enum) (?<name>\w+)")]
    private static partial Regex TopLevelType();

    [GeneratedRegex(@"^\s*\[(Fact|Theory)\b")]
    private static partial Regex TestAttribute();
}
