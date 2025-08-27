using Microsoft.CodeAnalysis.CSharp.Syntax;
using ReviewSharp.Models;
using ReviewSharp.Services;
using ReviewSharpApp.Tests.TestHelpers;
using Xunit;

namespace ReviewSharpApp.Tests;

public class DiViolationServiceTests
{
    private const string DiDirectInstantiationRule = "DI Violation: Direct Instantiation";
    private const string DiServiceLocatorRule = "DI Violation: Service Locator";

    private static List<CodeReviewResult> GetResults(string source)
    {
        var service = new DiViolationService(true);
        var root = CodeParsing.ParseCompilation(source);
        return service.Review(root);
    }

    #region Direct Instantiation Tests

    [Theory]
    [InlineData("var svc = new UserService();", true)]
    [InlineData("var repo = new UserRepository();", true)]
    [InlineData("var dto = new UserDto();", false)]
    [InlineData("var list = new List<int>();", false)]
    [InlineData("var config = new AppSettings();", false)]
    [InlineData("var factory = new UserFactory();", true)]
    [InlineData("var repo = new Repository<User>();", true)]
    [InlineData("var arr = new UserService[5];", false)]
    public void Class_DirectInstantiation_EmitsExpectedWarning(string codeLine, bool shouldWarn)
    {
        var source = $@"
        class TestClass {{
            public void Do() {{
                {codeLine}
            }}
        }}
    ";

        var results = GetResults(source);

        if (shouldWarn)
        {
            var result = Assert.Single(results);
            Assert.Equal(DiDirectInstantiationRule, result.RuleName);
            Assert.Equal(4, result.LineNumber); // Adjust based on formatting
        }
        else
        {
            Assert.DoesNotContain(results, r => r.RuleName == DiDirectInstantiationRule);
        }
    }

    #endregion

    #region Service Locator Tests

    [Theory]
    [InlineData("var svc = serviceProvider.GetService<UserService>();", true)]
    [InlineData("var svc = serviceProvider.GetRequiredService<UserService>();", true)]
    [InlineData("var repo = serviceProvider.GetService<UserRepository>();", true)]
    [InlineData("var dto = new UserDto();", false)]
    [InlineData("var factory = serviceProvider.GetRequiredService<UserFactory>();", true)]
    [InlineData("var nested = serviceProvider.GetService<NestedService>();", true)]
    public void Class_ServiceLocator_EmitsExpectedWarning(string codeLine, bool shouldWarn)
    {
        var source = $@"
        class TestClass {{
            public void Do(IServiceProvider serviceProvider) {{
                {codeLine}
            }}
        }}
    ";

        var results = GetResults(source);

        if (shouldWarn)
        {
            var result = Assert.Single(results);
            Assert.Equal(DiServiceLocatorRule, result.RuleName);
            Assert.Equal(4, result.LineNumber); // Adjust based on formatting
        }
        else
        {
            Assert.DoesNotContain(results, r => r.RuleName == DiServiceLocatorRule);
        }
    }

    #endregion

}
