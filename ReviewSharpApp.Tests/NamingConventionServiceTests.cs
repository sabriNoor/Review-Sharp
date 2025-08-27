using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ReviewSharp.Models;
using ReviewSharp.Services;
using ReviewSharpApp.Tests.TestHelpers;

namespace ReviewSharpApp.Tests;

public class NamingConventionServiceTests
{
    private static CompilationUnitSyntax Parse(string source) => CodeParsing.ParseCompilation(source);

    private static List<CodeReviewResult> GetResults(string source)
    {
        // Arrange
        var service = new NamingConventionService();
        var root = Parse(source);

        // Act
        var results = service.Review(root);
        return results;
    }

  
    [Theory]
    [InlineData("class badClass { }", "Class Naming Convention", "Warning", 1)]
    [InlineData("interface Service { }", "Interface Naming Convention", "Error", 1)]
    [InlineData("class Test { void doThing() { } }", "Method Naming Convention", "Warning", 1)]
    [InlineData("class Test { void DoThing(int NotCamel) { } }", "Parameter Naming Convention", "Info", 1)]
    [InlineData("class Test { private int fieldName; }", "Field Naming Convention", "Warning", 1)]
    [InlineData("class Test { public const int Not_All_Caps = 1; }", "Constant Naming Convention", "Warning", 1)]
    [InlineData("class Test { public int propName { get; set; } }", "Property Naming Convention", "Warning", 1)]
    [InlineData("enum badEnum { A }", "Enum Naming Convention", "Warning", 1)]
    [InlineData("enum Colors { red }", "Enum Member Naming Convention", "Warning", 1)]
    [InlineData("class Test { void Do() { int NotCamel = 0; } }", "Local Variable Naming Convention", "Info", 1)]
    [InlineData("using System; class Test { void Handle(int x, EventArgs e) {} }", "Event Handler Naming Convention", "Info", 1)]
    [InlineData("delegate void MyDelegate();", "Delegate Type Naming Convention", "Info", 1)]
    [InlineData("class Box<TItem> { }", "Generic Type Parameter Naming Convention", "Info", 1)]
    public void Review_InvalidNamePatterns_ReportExpectedRuleAndSeverity_AtExpectedLine(string source, string expectedRule, string expectedSeverity, int expectedLine)
    {
        var results = GetResults(source);

        // Assert
        Assert.Contains(results, r => r.RuleName == expectedRule && r.Severity == expectedSeverity && r.LineNumber == expectedLine);
    }

    [Theory]
    [InlineData("class GoodClass { }", "Class Naming Convention")]
    [InlineData("interface IGoodService { }", "Interface Naming Convention")]
    [InlineData("class Test { void DoThing() { } }", "Method Naming Convention")]
    [InlineData("class Test { void DoThing(int someValue) { } }", "Parameter Naming Convention")]
    [InlineData("class Test { private int _count; }", "Field Naming Convention")]
    [InlineData("class Test { public const int MAX_COUNT = 1; }", "Constant Naming Convention")]
    [InlineData("class Test { public int Count { get; set; } }", "Property Naming Convention")]
    [InlineData("enum Color { Red }", "Enum Naming Convention")]
    [InlineData("enum Colors { Red }", "Enum Member Naming Convention")]
    [InlineData("class Test { void Do() { int count = 0; } }", "Local Variable Naming Convention")]
    [InlineData("using System; class Test { void OnClicked(object sender, EventArgs e) {} }", "Event Handler Naming Convention")]
    [InlineData("delegate void CompletedHandler();", "Delegate Type Naming Convention")]
    [InlineData("class Box<T> { }", "Generic Type Parameter Naming Convention")]
    public void Review_CompliantNamePatterns_DoNotReportRule(string source, string unexpectedRule)
    {
        var results = GetResults(source);

        // Assert
        Assert.DoesNotContain(results, r => r.RuleName == unexpectedRule);
    }

    [Fact]
    public void Review_MultipleViolationsInOneSource_ReportsAllWithCorrectLines()
    {
        var source = @"
            class badClass
            {
                private int fieldName;
                public const int Not_All_Caps = 1;
                public int prop_name { get; set; }
                void doThing(int NotCamel)
                {
                    int NotLocal = 0;
                }
            }
            interface Service { }
            enum badEnum { a, B }";

        var results = GetResults(source);

        // Assert
        Assert.Contains(results, r => r.RuleName == "Class Naming Convention" && r.LineNumber == 2);
        Assert.Contains(results, r => r.RuleName == "Field Naming Convention" && r.LineNumber == 4);
        Assert.Contains(results, r => r.RuleName == "Constant Naming Convention" && r.LineNumber == 5);
        Assert.Contains(results, r => r.RuleName == "Property Naming Convention" && r.LineNumber == 6);
        Assert.Contains(results, r => r.RuleName == "Method Naming Convention" && r.LineNumber == 7);
        Assert.Contains(results, r => r.RuleName == "Parameter Naming Convention" && r.LineNumber == 7);
        Assert.Contains(results, r => r.RuleName == "Local Variable Naming Convention" && r.LineNumber == 9);
        Assert.Contains(results, r => r.RuleName == "Interface Naming Convention" && r.LineNumber == 12);
        Assert.Contains(results, r => r.RuleName == "Enum Naming Convention" && r.LineNumber == 13);
        Assert.Contains(results, r => r.RuleName == "Enum Member Naming Convention" && r.LineNumber == 13);
    }

    [Fact]
    public void Review_EmptyOrMinimalCode_ReturnsNoViolations()
    {
        var empty = string.Empty;
        var minimal = "\n\n";
        Assert.Empty(GetResults(empty));
        Assert.Empty(GetResults(minimal));
    }
}


