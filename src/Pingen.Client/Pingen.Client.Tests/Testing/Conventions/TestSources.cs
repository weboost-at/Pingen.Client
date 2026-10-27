using System.Reflection;
using System.Runtime.CompilerServices;

namespace Pingen.Client.Tests.Testing.Conventions;

public static class TestSources
{
    private const BindingFlags Declared = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;

    public static string ProjectDirectory { get; } = FindProjectDirectory();

    // Every test source outside the shared Testing folder, relative to the project
    public static IReadOnlyList<string> Files { get; } =
    [
        .. Directory.EnumerateFiles(ProjectDirectory, "*.cs", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(ProjectDirectory, path).Replace('\\', '/'))
            .Where(path => !path.StartsWith("bin/") && !path.StartsWith("obj/") && !path.StartsWith("Testing/"))
            .Order()
    ];

    // xUnit's TheoryAttribute derives from FactAttribute, so this finds both
    public static IReadOnlyList<MethodInfo> TestMethods { get; } =
    [
        .. typeof(TestSources).Assembly.GetTypes()
            .Where(type => type is { IsAbstract: false, IsClass: true })
            .SelectMany(type => type.GetMethods(Declared))
            .Where(method => method.GetCustomAttribute<FactAttribute>() is not null)
    ];

    public static string[] Lines(string file) => File.ReadAllLines(Path.Combine(ProjectDirectory, file));

    // This file sits two folders below the project
    private static string FindProjectDirectory([CallerFilePath] string path = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(path)!, "..", ".."));
}
