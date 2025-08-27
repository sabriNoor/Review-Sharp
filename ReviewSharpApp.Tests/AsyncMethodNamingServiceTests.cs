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
        var service = new AsyncMethodNamingService();
        var root = CodeParsing.ParseCompilation(source);
        return service.Review(root);
    }

    #region Class Tests

    [Theory]
    [InlineData("public async Task Do()", true)]
    [InlineData("public async Task DoAsync()", false)]
    [InlineData("public void Do()", false)]
    public void Class_MethodNaming_EmitsExpectedWarning(string methodCode, bool shouldWarn)
    {
        var source = $@"
            using System.Threading.Tasks;
            class Test {{ {methodCode} }}
        ";

        var results = GetResults(source);

        if (shouldWarn)
            Assert.Contains(results, r => r.RuleName == AsyncMethodRule);
        else
            Assert.DoesNotContain(results, r => r.RuleName == AsyncMethodRule);
    }

    #endregion

    #region Interface Tests

    [Theory]
    [InlineData("Task Do()", true)]
    [InlineData("Task<int> Get()", true)]
    [InlineData("Task DoAsync()", false)]
    [InlineData("void Do()", false)]
    public void Interface_MethodNaming_EmitsExpectedWarning(string methodCode, bool shouldWarn)
    {
        var source = $@"
            using System.Threading.Tasks;
            interface IFoo {{ {methodCode} }}
        ";

        var results = GetResults(source);

        if (shouldWarn)
            Assert.Contains(results, r => r.RuleName == AsyncMethodInterfaceRule);
        else
            Assert.DoesNotContain(results, r => r.RuleName == AsyncMethodInterfaceRule);
    }

    #endregion
}
