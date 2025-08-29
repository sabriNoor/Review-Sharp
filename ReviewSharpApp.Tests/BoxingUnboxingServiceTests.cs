using ReviewSharp.Models;
using ReviewSharp.Services;
using ReviewSharpApp.Tests.TestHelpers;
using Xunit;
using System.Collections.Generic;
using System.Linq;

namespace ReviewSharpApp.Tests;

public class BoxingUnboxingServiceTests
{
    private const string BoxingRule = "Unnecessary Boxing";
    private const string UnboxingRule = "Unnecessary Unboxing";

    private static List<CodeReviewResult> GetResults(string source)
    {
        var service = new BoxingUnboxingService();
        var (root,model) = CodeParsing.ParseAndGetSemanticModel(source); // Assuming TestHelpers provides this
        return service.Review(root, model);
    }

    [Fact]
    public void Review_ShouldWarn_WhenValueTypeAssignedToObject()
    {
        var source = @"
            class TestClass {
                void M() {
                    int x = 42;
                    object o = x;
                }
            }";

        var results = GetResults(source);

        var result = results.Where(r => r.RuleName == BoxingRule).FirstOrDefault();
        Assert.NotNull(result);
        Assert.Equal(BoxingRule, result.RuleName);
        Assert.Equal(5, result.LineNumber); // adjust if indentation changes
    }

    [Fact]
    public void Review_ShouldWarn_WhenObjectCastToValueType()
    {
        var source = @"
            class TestClass {
                void M() {
                    object o = 42;
                    int x = (int)o;
                }
            }";

        var results = GetResults(source);

        var result = results.Where(r => r.RuleName == UnboxingRule).FirstOrDefault();
        Assert.NotNull(result);
        Assert.Equal(UnboxingRule, result.RuleName);
        Assert.Equal(5, result.LineNumber);
    }

    [Fact]
    public void Review_ShouldNotWarn_WhenValueTypeAssignedToValueType()
    {
        var source = @"
            class TestClass {
                void M() {
                    int a = 10;
                    int b = a;
                }
            }";

        var results = GetResults(source);

        Assert.Empty(results);
    }

    [Fact]
    public void Review_ShouldNotWarn_WhenReferenceTypeCast()
    {
        var source = @"
            class TestClass {
                void M() {
                    object o = ""hello"";
                    string s = (string)o;
                }
            }";

        var results = GetResults(source);

        Assert.Empty(results);
    }

    [Fact]
    public void Review_ShouldWarn_ForMultipleBoxingStatements()
    {
        var source = @"
            class TestClass {
                void M() {
                    int x = 1;
                    object o1 = x;
                    object o2 = x;
                }
            }";

        var results = GetResults(source);
        var boxingResults = results.Where(r => r.RuleName == BoxingRule).ToList();

        Assert.Equal(2, boxingResults.Count);
        Assert.All(boxingResults, r => Assert.Equal(BoxingRule, r.RuleName));
        Assert.Contains(boxingResults, r => r.LineNumber == 5);
        Assert.Contains(boxingResults, r => r.LineNumber == 6);
    }

    [Fact]
    public void Review_ShouldWarn_ForMultipleUnboxingStatements()
    {
        var source = @"
            class TestClass {
                void M() {
                    object o1 = 42;
                    object o2 = 43;
                    int x = (int)o1;
                    int y = (int)o2;
                }
            }";

        var results = GetResults(source);
        var unboxingResults = results.Where(r => r.RuleName == UnboxingRule).ToList();

        Assert.Equal(2, unboxingResults.Count);
        Assert.All(unboxingResults, r => Assert.Equal(UnboxingRule, r.RuleName));
        Assert.Contains(unboxingResults, r => r.LineNumber == 6);
        Assert.Contains(unboxingResults, r => r.LineNumber == 7);
    }
}
