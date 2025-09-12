using Microsoft.CodeAnalysis.CSharp;
using ReviewSharp.Services;
using ReviewSharp.Models;
using Xunit;
using System.Linq;
using ReviewSharpApp.Tests.TestHelpers;

namespace ReviewSharpApp.Tests.ServiceTests
{
    public class NullCheckStyleServiceTests
    {
        private static System.Collections.Generic.List<CodeReviewResult> GetResults(string code)
        {
            var service = new NullCheckStyleService();
            var root = CodeParsing.ParseCompilation(code);
            return service.Review(root);
        }

        [Theory]
        [InlineData("if (obj == null) {}", 1)]
        [InlineData("if (null == obj) {}", 1)]
        [InlineData("if (obj != null) {}", 1)]
        [InlineData("if (null != obj) {}", 1)]
        public void Review_InequalityWithNull_ShouldReturnSuggestion(string codeLine, int expectedCount)
        {
            var source = $@"
            class TestClass {{
                void Do() {{
                    {codeLine}
                }}
            }}";

            var results = GetResults(source);
            Assert.Equal(expectedCount, results.Count);

            if (codeLine.Contains("=="))
            {
                Assert.All(results, r => Assert.Contains("is null", r.Message));
            }
            else if (codeLine.Contains("!="))
            {
                Assert.All(results, r => Assert.Contains("is not null", r.Message));
            }
        }

        [Fact]
        public void Review_MultipleNullChecks_ShouldReturnAllSuggestions()
        {
            var source = @"
            class TestClass {
                void Do() {
                    object a = null;
                    object b = null;
                    if (a == null) {}
                    if (b != null) {}
                    if (null == a) {}
                    if (null != b) {}
                }
            }";

            var results = GetResults(source);
            Assert.Equal(4, results.Count);

            Assert.Contains(results, r => r.Message.Contains("is null"));
            Assert.Contains(results, r => r.Message.Contains("is not null"));
        }

        [Fact]
        public void Review_NonNullComparisons_ShouldReturnNoSuggestions()
        {
            var source = @"
            class TestClass {
                void Do() {
                    int x = 0;
                    if (x == 1) {}
                    if (x != 1) {}
                }
            }";

            var results = GetResults(source);
            Assert.Empty(results);
        }
    }
}
