using Xunit;
using ReviewSharp.Services;
using ReviewSharp.Models;
using ReviewSharpApp.Tests.TestHelpers;
using System.Collections.Generic;

namespace ReviewSharpApp.Tests.ServiceTests
{
    public class ManyMethodParametersServiceTests
    {
        private static List<CodeReviewResult> GetResults(string source)
        {
            var service = new ManyMethodParametersService();
            var root = CodeParsing.ParseCompilation(source);
            return service.Review(root);
        }

        [Fact]
        public void Review_MethodWithFewParameters_NoWarning()
        {
            var code = "class C { void M(int a, int b, int c, int d, int e, int f) { } }";
            var results = GetResults(code);
            Assert.Empty(results);
        }

        [Fact]
        public void Review_MethodWithManyParameters_Warns()
        {
            var code = "class C { void M(int a, int b, int c, int d, int e, int f, int g, int h) { } }";
            var results = GetResults(code);
            Assert.Single(results);
            Assert.Contains("has 8 parameters", results[0].Message);
        }

        [Fact]
        public void Review_MultipleMethods_MixedWarnings()
        {
            var code = "class C { void M1(int a, int b, int c, int d, int e, int f) { } void M2(int a, int b, int c, int d, int e, int f, int g, int h) { } }";
            var results = GetResults(code);
            Assert.Single(results);
            Assert.Contains("M2", results[0].Message);
        }

        [Fact]
        public void Review_MethodWithExactlyThreshold_NoWarning()
        {
            var code = "class C { void M(int a, int b, int c, int d, int e, int f, int g) { } }";
            var results = GetResults(code);
            Assert.Empty(results);
        }

    }
}
