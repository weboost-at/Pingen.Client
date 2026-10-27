using FluentAssertions;

namespace Pingen.Client.Tests.Testing.Conventions;

public class TestConventionTests
{
    [Fact]
    public void Every_Test_Is_Named_Given_State_And_Outcome_Or_The_Outcome_Alone() =>
        ConventionViolations.Names().Should().BeEmpty(Listing(ConventionViolations.Names()));

    [Fact]
    public void Every_Test_Declaration_Fits_120_Columns() =>
        ConventionViolations.LongDeclarations().Should().BeEmpty(Listing(ConventionViolations.LongDeclarations()));

    [Fact]
    public void Every_Test_Class_Lives_In_A_File_Named_After_It_In_Its_Namespace_Folder() =>
        ConventionViolations.Misplaced().Should().BeEmpty(Listing(ConventionViolations.Misplaced()));

    [Fact]
    public void Test_Bodies_Carry_No_Comments_But_Arrange_Act_And_Assert() =>
        ConventionViolations.Comments().Should().BeEmpty(Listing(ConventionViolations.Comments()));

    [Fact]
    public void Every_Helper_Lives_In_Testing_Or_In_Its_Slices_Support_Folder() =>
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
    public void A_Banned_Call_Stays_Out_Of_The_Tests(string call) =>
        ConventionViolations.Banned(call).Should().BeEmpty(Listing(ConventionViolations.Banned(call)));

    private static string Listing(IEnumerable<string> entries) =>
        $"every entry breaks the convention:{Environment.NewLine}{string.Join(Environment.NewLine, entries)}";
}
