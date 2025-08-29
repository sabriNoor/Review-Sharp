using ReviewSharp.Models;
using ReviewSharp.Services;
using ReviewSharpApp.Tests.TestHelpers;
using Xunit;

namespace ReviewSharpApp.Tests;

public class UnreachableCodeServiceTests
{
    private const string RuleName = "Unreachable Code";

    private static List<CodeReviewResult> GetResults(string source)
    {
        var service = new UnreachableCodeService();
        var root = CodeParsing.ParseCompilation(source);
        return service.Review(root);
    }

    [Fact]
    public void Review_ShouldWarn_WhenCodeAfterReturn()
    {
        var source = @"
            class TestClass {
                void M() {
                    return;
                    int x = 10;
                }
            }";

        var results = GetResults(source);

        var result = Assert.Single(results);
        Assert.Equal(RuleName, result.RuleName);
        Assert.Equal(5, result.LineNumber); // adjust based on indentation
    }

    [Fact]
    public void Review_ShouldWarn_WhenCodeAfterThrow()
    {
        var source = @"
            class TestClass {
                void M() {
                    throw new Exception();
                    Console.WriteLine(""never reached"");
                }
            }";

        var results = GetResults(source);

        var result = Assert.Single(results);
        Assert.Equal(RuleName, result.RuleName);
        Assert.Equal(5, result.LineNumber);
    }
    [Fact]
    public void Review_ShouldWarn_WhenCodeAfterBreak()
    {
        var source = @"
            class TestClass {
                void M() {
                    while(true) {
                        break;
                        Console.WriteLine(""unreachable"");
                    }
                }
            }";

        var results = GetResults(source);

        var result = Assert.Single(results);
        Assert.Equal(RuleName, result.RuleName);
        Assert.Equal(6, result.LineNumber);
    }
    [Fact]
    public void Review_ShouldWarn_WhenCodeAfterContinue()
    {
        var source = @"
            class TestClass {
                void M() {
                    for(int i = 0; i < 10; i++) {
                        continue;
                        Console.WriteLine(""unreachable"");
                    }
                }
            }";

        var results = GetResults(source);

        var result = Assert.Single(results);
        Assert.Equal(RuleName, result.RuleName);
        Assert.Equal(6, result.LineNumber);
    }
    [Fact]
    public void Review_ShouldNotWarn_WhenNoUnreachableCode()
    {
        var source = @"
            class TestClass {
                void M() {
                    int x = 1;
                    x++;
                    return;
                }
            }";

        var results = GetResults(source);

        Assert.Empty(results);
    }
    [Fact]
    public void Review_ShouldWarn_ForMultipleUnreachableStatements()
    {
        var source = @"
            class TestClass {
                void M() {
                    return;
                    int x = 10;
                    Console.WriteLine(x);
                }
            }";

        var results = GetResults(source);

        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.Equal(RuleName, r.RuleName));
        Assert.Contains(results, r => r.LineNumber == 5);
        Assert.Contains(results, r => r.LineNumber == 6);
    }
    [Fact]
    public void Review_ShouldHandleMultipleMethods()
    {
        var source = @"
            class TestClass {
                void M1() {
                    return;
                    int x = 10;
                }
                void M2() {
                    throw new Exception();
                    Console.WriteLine(""never reached"");
                }
                void M3() {
                    int y = 5;
                    y++;
                }
            }";

        var results = GetResults(source);

        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.Equal(RuleName, r.RuleName));
        Assert.Contains(results, r => r.LineNumber == 5); // from M1
        Assert.Contains(results, r => r.LineNumber == 9); // from M2
    }
    
}
