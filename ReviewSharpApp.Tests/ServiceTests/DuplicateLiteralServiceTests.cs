using Xunit;
using ReviewSharp.Services;
using ReviewSharp.Models;
using ReviewSharpApp.Tests.TestHelpers;
using System.Collections.Generic;

namespace ReviewSharpApp.Tests.ServiceTests
{
    public class DuplicateLiteralServiceTests
    {
        private static List<CodeReviewResult> GetResults(string source)
        {
            var service = new DuplicateLiteralService();
            var root = CodeParsing.ParseCompilation(source);
            return service.Review(root);
        }

        [Fact]
        public void Review_DuplicateStringLiteral_Warns()
        {
            var code = "class C { void M() { var a = \"hello\"; var b = \"hello\"; } }";
            var results = GetResults(code);
            Assert.Contains(results, r => r.Message.Contains("Duplicate literal 'hello'"));
        }

        [Fact]
        public void Review_DuplicateIntLiteral_Warns()
        {
            var code = "class C { void M() { int a = 42; int b = 42; } }";
            var results = GetResults(code);
            Assert.Contains(results, r => r.Message.Contains("Duplicate literal '42'"));
        }

        [Fact]
        public void Review_DuplicateBoolLiteral_NoWarning()
        {
            var code = "class C { void M() { bool a = true; bool b = true; } }";
            var results = GetResults(code);
            Assert.DoesNotContain(results, r => r.Message.Contains("Duplicate literal 'true'"));
        }

        [Fact]
        public void Review_DuplicateCharLiteral_Warns()
        {
            var code = "class C { void M() { char a = 'x'; char b = 'x'; } }";
            var results = GetResults(code);
            Assert.Contains(results, r => r.Message.Contains("Duplicate literal 'x'"));
        }

        [Fact]
        public void Review_DuplicateInDifferentScopes_Warns()
        {
            var code = "class C { void M() { int a = 1; } void N() { int b = 1; } }";
            var results = GetResults(code);
            Assert.Contains(results, r => r.Message.Contains("Duplicate literal '1'"));
        }

        [Fact]
        public void Review_SingleLiteral_NoWarning()
        {
            var code = "class C { void M() { int a = 99; } }";
            var results = GetResults(code);
            Assert.DoesNotContain(results, r => r.Message.Contains("Duplicate literal"));
        }

        [Fact]
        public void Review_EmptyStringLiteral_NoWarning()
        {
            var code = "class C { void M() { var a = \"\"; } }";
            var results = GetResults(code);
            Assert.DoesNotContain(results, r => r.Message.Contains("Duplicate literal"));
        }

        [Fact]
        public void Review_DuplicateNullLiteral_NoWarning()
        {
            var code = "class C { void M() { object a = null; object b = null; } }";
            var results = GetResults(code);
            Assert.DoesNotContain(results, r => r.Message.Contains("Duplicate literal"));
        }

        [Fact]
        public void Review_DuplicateLiteralInArray_Warns()
        {
            var code = "class C { void M() { int[] arr = { 5, 5, 6 }; } }";
            var results = GetResults(code);
            Assert.Contains(results, r => r.Message.Contains("Duplicate literal '5'"));
        }

        [Fact]
        public void Review_DuplicateLiteralInFieldAndProperty_Warns()
        {
            var code = "class C { int x = 7; public int Y { get; set; } = 7; }";
            var results = GetResults(code);
            Assert.Contains(results, r => r.Message.Contains("Duplicate literal '7'"));
        }
    }
}
