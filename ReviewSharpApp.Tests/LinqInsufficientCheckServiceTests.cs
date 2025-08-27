using Microsoft.CodeAnalysis.CSharp.Syntax;
using ReviewSharp.Models;
using ReviewSharp.Services;
using ReviewSharpApp.Tests.TestHelpers;
using Xunit;

namespace ReviewSharpApp.Tests;

public class LinqInsufficientCheckServiceTests
{
    private const string InsufficientPredicateRule = "LINQ Insufficient Check";
    private const string InefficientMaterializationRule = "LINQ Inefficient Materialization";
    private const string ExistenceCheckRule = "LINQ Existence Check";
    private const string ChainedWhereRule = "LINQ Chained Where";
    private const string NullHandlingRule = "LINQ Null Handling";

    private static List<CodeReviewResult> GetResults(string source)
    {
        var service = new LinqInsufficientCheckService();
        var root = CodeParsing.ParseCompilation(source);
        return service.Review(root);
    }

    #region Insufficient Predicate Tests

    [Theory]
    [InlineData("items.First();")]
    [InlineData("items.Single();")]
    [InlineData("items.Last();")]
    public void Review_InsufficientPredicate_ShouldWarn_WhenNoPredicateUsed(string codeLine)
    {
        var source = $@"
        using System.Linq;
        class TestClass {{
            void Do() {{
                var items = new int[]{{1,2,3}};
                {codeLine}
            }}
        }}";

        var results = GetResults(source);
        var result = Assert.Single(results);
        Assert.Equal(InsufficientPredicateRule, result.RuleName);
        Assert.Equal(6, result.LineNumber); // Adjust for actual line
    }

    #endregion

    #region Inefficient Materialization Tests

    [Theory]
    [InlineData("items.ToList().Where(x => x > 0);")]
    [InlineData("items.ToArray().Select(x => x*2);")]
    public void Review_InefficientMaterialization_ShouldSuggest_WhenFilteringAfterMaterialization(string codeLine)
    {
        var source = $@"
        using System.Linq;
        class TestClass {{
            void Do() {{
                var items = new int[]{{1,2,3}};
                {codeLine}
            }}
        }}";

        var results = GetResults(source);
        var result = Assert.Single(results);
        Assert.Equal(InefficientMaterializationRule, result.RuleName);
        Assert.Equal(6, result.LineNumber);
    }

    #endregion

    #region Count vs Any Tests

    [Fact]
    public void Review_CountVsAny_ShouldSuggest_WhenCountGreaterThanZero()
    {
        var source = @"
        using System.Linq;
        class TestClass {
            void Do() {
                var items = new int[]{1,2,3};
                if(items.Count() > 0) {}
            }
        }";

        var results = GetResults(source);
        var result = Assert.Single(results);
        Assert.Equal(ExistenceCheckRule, result.RuleName);
        Assert.Equal(6, result.LineNumber);
    }

    #endregion

    #region Chained Where Tests

    [Fact]
    public void Review_ChainedWhere_ShouldSuggest_WhenMultipleWhereCalls()
    {
        var source = @"
        using System.Linq;
        class TestClass {
            void Do() {
                var items = new int[]{1,2,3};
                var result = items.Where(x => x > 0).Where(x => x < 3);
            }
        }";

        var results = GetResults(source);
        var result = Assert.Single(results);
        Assert.Equal(ChainedWhereRule, result.RuleName);
        Assert.Equal(6, result.LineNumber);
    }

    #endregion

    #region FirstOrDefault Null Handling Tests

    [Fact]
    public void Review_FirstOrDefaultNullHandling_ShouldWarn_WhenMemberAccessedDirectly()
    {
        var source = @"
        using System.Linq;
        class TestClass {
            void Do() {
                var items = new int[]{1,2,3};
                var value = items.FirstOrDefault().ToString();
            }
        }";

        var results = GetResults(source);
        var result = Assert.Single(results);
        Assert.Equal(NullHandlingRule, result.RuleName);
        Assert.Equal(6, result.LineNumber);
    }

    [Fact]
    public void Review_FirstOrDefaultNullHandling_ShouldNotWarn_WhenNotAccessingMember()
    {
        var source = @"
        using System.Linq;
        class TestClass {
            void Do() {
                var items = new int[]{1,2,3};
                var value = items.FirstOrDefault();
            }
        }";

        var results = GetResults(source);
        Assert.DoesNotContain(results, r => r.RuleName == NullHandlingRule);
    }

    #endregion

    #region Multiple Violations

    [Fact]
    public void Review_MultipleViolations_ShouldWarn_ForAllDetectedIssues()
    {
        var source = @"
    using System.Linq;
    class TestClass {
        void Do() {
            var items = new int[]{1,2,3};

            // 1. First() without predicate
            var firstItem = items.First();

            // 2. ToList() then Where()
            var filtered = items.ToList().Where(x => x > 1);

            // 3. Count() > 0 instead of Any()
            if(items.Count() > 0) {}

            // 4. Chained Where()
            var chained = items.Where(x => x > 0).Where(x => x < 3);

            // 5. FirstOrDefault() member access
            var value = items.FirstOrDefault().ToString();
        }
    }";

        var results = LinqInsufficientCheckServiceTests.GetResults(source);

        Assert.Equal(5, results.Count); // all 5 issues detected

        Assert.Contains(results, r => r.RuleName == "LINQ Insufficient Check");
        Assert.Contains(results, r => r.RuleName == "LINQ Inefficient Materialization");
        Assert.Contains(results, r => r.RuleName == "LINQ Existence Check");
        Assert.Contains(results, r => r.RuleName == "LINQ Chained Where");
        Assert.Contains(results, r => r.RuleName == "LINQ Null Handling");
    }
    #endregion

}
