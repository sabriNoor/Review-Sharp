using Microsoft.CodeAnalysis.CSharp.Syntax;
using ReviewSharp.Models;
using ReviewSharp.Services;
using ReviewSharpApp.Tests.TestHelpers;
using Xunit;

namespace ReviewSharpApp.Tests;

public class AsyncMethodNamingServiceTests
{
    private const string AsyncMethodRule = "Async Method Naming Convention";
    private const string AsyncMethodInterfaceRule = "Async Method Naming Convention (Interface)";

    private static List<CodeReviewResult> GetResults(string source)
    {
        // Arrange
        var service = new AsyncMethodNamingService();
        var root = CodeParsing.ParseCompilation(source);

        // Act
        var results = service.Review(root);
        return results;
    }


    [Theory]
    [InlineData("public async Task Do()", true, 3)]
    [InlineData("public async Task DoAsync()", false, 3)]
    [InlineData("public void Do()", false, 3)]
    public void  Review_AsyncClassMethod_ShouldWarn_WhenMissingAsyncSuffix(string methodCode, bool shouldWarn, int expectedLine)
    {
        var source = $@"
            using System.Threading.Tasks;
            class Test {{ {methodCode} }}
            ";

        var results = GetResults(source);

        if (shouldWarn)
        {
            Assert.Contains(results, r => r.RuleName == AsyncMethodRule && r.LineNumber == expectedLine);
        }
        else
        {
            Assert.DoesNotContain(results, r => r.RuleName == AsyncMethodRule);
        }
    }



    [Theory]
    [InlineData("Task Do()", true, 3)]
    [InlineData("Task<int> Get()", true, 3)]
    [InlineData("Task DoAsync()", false, 3)]
    [InlineData("Task<int> DoAsync()", false, 3)]
    [InlineData("void Do()", false, 3)]
    public void Review_AsyncInterfaceMethod_ShouldWarn_WhenMissingAsyncSuffix(string methodCode, bool shouldWarn, int expectedLine)
    {
        var source = $@"
            using System.Threading.Tasks;
            interface IFoo {{ {methodCode} }}
            ";

        var results = GetResults(source);

        if (shouldWarn)
        {
            Assert.Contains(results, r => r.RuleName == AsyncMethodInterfaceRule && r.LineNumber == expectedLine);
        }
        else
        {
            Assert.DoesNotContain(results, r => r.RuleName == AsyncMethodInterfaceRule);
        }
    }

}
