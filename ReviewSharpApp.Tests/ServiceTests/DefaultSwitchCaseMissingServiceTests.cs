using Xunit;
using ReviewSharp.Services;
using ReviewSharp.Models;
using ReviewSharpApp.Tests.TestHelpers;
using System.Collections.Generic;

namespace ReviewSharpApp.Tests.ServiceTests
{
    public class DefaultSwitchCaseMissingServiceTests
    {
        private static List<CodeReviewResult> GetResults(string source)
        {
            var service = new DefaultSwitchCaseMissingService();
            var root = CodeParsing.ParseCompilation(source);
            return service.Review(root);
        }

        [Fact]
        public void Review_SwitchWithDefault_NoWarning()
        {
            var code = "class C { void M(int x) { switch(x) { case 1: break; default: break; } } }";
            var results = GetResults(code);
            Assert.Empty(results);
        }

        [Fact]
        public void Review_SwitchWithoutDefault_Warns()
        {
            var code = "class C { void M(int x) { switch(x) { case 1: break; case 2: break; } } }";
            var results = GetResults(code);
            Assert.Single(results);
            Assert.Contains("missing a default case", results[0].Message);
        }

        [Fact]
        public void Review_MultipleSwitchStatements_MixedWarnings()
        {
            var code = @"class C { void M(int x) { switch(x) { case 1: break; } switch(x) { case 2: break; default: break; } } }";
            var results = GetResults(code);
            Assert.Single(results);
            Assert.Contains("missing a default case", results[0].Message);
        }

        [Fact]
        public void Review_EmptySwitch_Warns()
        {
            var code = "class C { void M(int x) { switch(x) { } } }";
            var results = GetResults(code);
            Assert.Single(results);
            Assert.Contains("missing a default case", results[0].Message);
        }

        [Fact]
        public void Review_NullRoot_ReturnsEmpty()
        {
            var service = new DefaultSwitchCaseMissingService();
            var results = service.Review(null);
            Assert.Empty(results);
        }
    }
}
