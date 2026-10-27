using FluentAssertions;

namespace Pingen.Client.Tests.Testing.Conventions;

public class TestConventionTests
{
    [Fact]
    public void When_the_tests_are_named_every_name_reads_When_or_Given_When() =>
        ConventionViolations.Names().Should().BeEmpty(Listing(ConventionViolations.Names()));

    [Fact]
    public void When_a_test_is_declared_its_line_fits_120_columns() =>
        ConventionViolations.LongDeclarations().Should().BeEmpty(Listing(ConventionViolations.LongDeclarations()));

    [Fact]
    public void When_a_test_class_is_placed_it_lives_in_a_file_named_after_it_in_its_namespace_folder() =>
        ConventionViolations.Misplaced().Should().BeEmpty(Listing(ConventionViolations.Misplaced()));

    [Fact]
    public void When_a_test_body_is_commented_it_carries_only_Arrange_Act_and_Assert() =>
        ConventionViolations.Comments().Should().BeEmpty(Listing(ConventionViolations.Comments()));

    [Fact]
    public void When_a_helper_is_declared_it_lives_in_Testing_or_in_its_slices_Support_folder() =>
        ConventionViolations.SupportOutsideSupport().Should().BeEmpty(Listing(ConventionViolations.SupportOutsideSupport()));

    [Theory]
    [InlineData("Task.Delay")]
    [InlineData("Thread.Sleep")]
    [InlineData("#region")]
    [InlineData("sealed")]
    [InlineData("XML doc")]
    [InlineData("new Random")]
    [InlineData("raw cancellation token")]
    [InlineData("own test host")]
    [InlineData("intermediate subject")]
    public void When_the_tests_are_written_a_banned_call_stays_out_of_them(string call) =>
        ConventionViolations.Banned(call).Should().BeEmpty(Listing(ConventionViolations.Banned(call)));

    private static string Listing(IEnumerable<string> entries) =>
        $"every entry breaks the convention:{Environment.NewLine}{string.Join(Environment.NewLine, entries)}";
}
