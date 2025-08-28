using Xunit;
using ReviewSharp.Services;
using ReviewSharp.Models;
using ReviewSharpApp.Tests.TestHelpers;
using System.Collections.Generic;

namespace ReviewSharp.Tests
{
    public class PossibleNullReferenceServiceTests
    {
        private static List<CodeReviewResult> GetResults(string source)
        {
            var service = new PossibleNullReferenceService();
            var root = CodeParsing.ParseCompilation(source);
            return service.Review(root);
        }

        [Fact]
        public void Review_NullableLocalVariableInBinaryExpression_Warns()
        {
            var code = "class C { void M() { int? x = null; int y = x + 1; } }";
            var results = GetResults(code);
            Assert.Contains(results, r => r.Message.Contains("Possible null reference on 'x'"));
        }

        [Fact]
        public void Review_NullablePropertyInBinaryExpression_Warns()
        {
            var code = "class C { public int? p { get; set; } void M() { int y = p + 1; } }";
            var results = GetResults(code);
            Assert.Contains(results, r => r.Message.Contains("Possible null reference on 'p'"));
        }

        [Fact]
        public void Review_NullableFieldInAssignment_Warns()
        {
            var code = "class C { int? f; void M() { int y; y = f; } }";
            var results = GetResults(code);
            Assert.Contains(results, r => r.Message.Contains("Possible null reference on 'f'"));
        }

        [Fact]
        public void Review_NullableParameterInReturn_Warns()
        {
            var code = "class C { int M(int? x) { return x; } }";
            var results = GetResults(code);
            Assert.Contains(results, r => r.Message.Contains("Possible null reference on 'x'"));
        }

        [Fact]
        public void Review_NullableParameterInMemberAccess_Warns()
        {
            var code = "class C { void M(User? user) { user.Login(); } }";
            var results = GetResults(code);
            Assert.Contains(results, r => r.Message.Contains("Possible null reference on 'user'"));
        }

        [Fact]
        public void Review_NonNullableVariable_NoWarning()
        {
            var code = "class C { void M() { int x = 1; int y = x + 1; } }";
            var results = GetResults(code);
            Assert.DoesNotContain(results, r => r.Message.Contains("Possible null reference"));
        }

        [Fact]
        public void Review_NullRoot_ReturnsEmpty()
        {
            var service = new PossibleNullReferenceService();
            var results = service.Review(null);
            Assert.Empty(results);
        }
    }
}
