using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using ReviewSharp.Services;
using ReviewSharpApp.Tests.TestHelpers;
using Xunit;
using System.Linq;
using ReviewSharp.Models;

public class UnusedUsingServiceTests
{
    private readonly UnusedUsingService _service = new();

    private static List<CodeReviewResult> GetResults(string source)
    {
        var (root, model) = CodeParsing.ParseAndGetSemanticModel(source);
        var service = new UnusedUsingService();
        return service.Review(root, model);
    }

    [Fact]
    public void Review_ShouldWarn_WhenUsingIsUnused()
    {
        var source = @"
            using System.Text;
            class TestClass {
                void M() { int x = 1; }
            }";

        var results = GetResults(source);

        Assert.Single(results);
        var result = results.First();
        Assert.Equal("Unused Using Directive", result.RuleName);
        Assert.Contains("System.Text", result.Message);
        Assert.Equal(2, result.LineNumber); // line of the using directive
    }

    [Fact]
    public void Review_ShouldNotWarn_WhenUsingIsUsed()
    {
        var source = @"
            using System;
            class TestClass {
                void M() { Console.WriteLine(""Hello""); }
            }";

        var results = GetResults(source);

        Assert.Empty(results);
    }

    [Fact]
    public void Review_ShouldIgnoreStaticAndAliasUsings()
    {
        var source = @"
            using System.IO;
            using static System.Console;
            using IO = System.IO;
            class TestClass { }";

        var results = GetResults(source);

        // Only System.IO is unused and not static/alias
        Assert.Single(results);
        Assert.Equal("System.IO", results.First().Message.Split('\'')[1]);
    }
    [Fact]
    public void Review_ShouldNotWarn_WhenAllUsingsAreUsed()
    {
        var source = @"
            using System;
            using System.Collections.Generic;
            class TestClass {
                void M() { 
                Console.WriteLine(""Hello""); 
                List<int> list = new List<int>(); 
                }
            }";

        var results = GetResults(source);

        Assert.Empty(results);
    }
    [Fact]
    public void Review_ShouldWarn_ForMultipleUnusedUsings()
    {
        var source = @"
            using System.Text;
            using System.IO;
            using System.Linq;
            class TestClass {
                void M() { int x = 1; }
            }";

        var results = GetResults(source);
        var unusedUsings = results.Where(r => r.RuleName == "Unused Using Directive").ToList();

        Assert.Equal(3, unusedUsings.Count);
        Assert.All(unusedUsings, r => Assert.Equal("Unused Using Directive", r.RuleName));
        Assert.Contains(unusedUsings, r => r.Message.Contains("System.Text"));
        Assert.Contains(unusedUsings, r => r.Message.Contains("System.IO"));
        Assert.Contains(unusedUsings, r => r.Message.Contains("System.Linq"));
        Assert.Contains(unusedUsings, r => r.LineNumber == 2);
        Assert.Contains(unusedUsings, r => r.LineNumber == 3);
        Assert.Contains(unusedUsings, r => r.LineNumber == 4);
    }
    [Fact]
    public void LocalNamespace_IsUsed_ShouldNotReport()
    {
        // Arrange: using points to a namespace outside the root namespace
        var source = @"
            namespace MyApp
            {
                using MyApp.Utilities;
                class TestClass
                {
                    void M() 
                    { 
                        Utilities.Helper(); 
                    }
                }
            }

            namespace MyApp.Utilities
            {
                class Utilities
                {
                    public static void Helper() { }
                }
            }";

        // Act
        var results = GetResults(source);

        // Assert
        Assert.Empty(results);
    }


}
