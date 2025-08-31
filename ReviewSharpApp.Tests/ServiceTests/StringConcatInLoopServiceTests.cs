using Xunit;
using ReviewSharp.Services;
using ReviewSharp.Models;
using ReviewSharpApp.Tests.TestHelpers;
using System.Collections.Generic;

namespace ReviewSharpApp.Tests.ServiceTests
{
    public class StringConcatInLoopServiceTests
    {
        private static List<CodeReviewResult> GetResults(string source)
        {
            var service = new StringConcatInLoopService();
            var root = CodeParsing.ParseCompilation(source);
            return service.Review(root);
        }

        [Fact]
        public void Review_NoLoops_NoSuggestion()
        {
            var code = "class C { void M() { string s = \"\"; s += \"a\"; } }";
            var results = GetResults(code);
            Assert.Empty(results);
        }

        [Fact]
        public void Review_StringConcatInForLoop_Suggests()
        {
            var code = "class C { void M() { string s = \"\"; for(int i=0;i<10;i++) { s += i; } } }";
            var results = GetResults(code);
            Assert.Contains(results, r => r.Message.Contains("String concatenation inside loops"));
        }

        [Fact]
        public void Review_StringConcatInForEachLoop_Suggests()
        {
            var code = "class C { void M() { string s = \"\"; foreach(var x in new[]{1,2,3}) { s += x; } } }";
            var results = GetResults(code);
            Assert.Contains(results, r => r.Message.Contains("String concatenation inside loops"));
        }

        [Fact]
        public void Review_StringConcatInWhileLoop_Suggests()
        {
            var code = "class C { void M() { string s = \"\"; int i=0; while(i<5) { s += i; i++; } } }";
            var results = GetResults(code);
            Assert.Contains(results, r => r.Message.Contains("String concatenation inside loops"));
        }

        [Fact]
        public void Review_StringConcatInDoLoop_Suggests()
        {
            var code = "class C { void M() { string s = \"\"; int i=0; do { s += i; i++; } while(i<3); } }";
            var results = GetResults(code);
            Assert.Contains(results, r => r.Message.Contains("String concatenation inside loops"));
        }

        [Fact]
        public void Review_StringConcatWithPlusEquals_Suggests()
        {
            var code = "class C { void M() { string s = \"\"; for(int i=0;i<2;i++) { s += \"a\"; } } }";
            var results = GetResults(code);
            Assert.Contains(results, r => r.Message.Contains("String concatenation inside loops"));
        }

        [Fact]
        public void Review_StringConcatWithSimpleAssignment_Suggests()
        {
            var code = "class C { void M() { string s = \"\"; for(int i=0;i<2;i++) { s = s + \"a\"; } } }";
            var results = GetResults(code);
            Assert.Contains(results, r => r.Message.Contains("String concatenation inside loops"));
        }

        [Fact]
        public void Review_StringConcatWithVar_Suggests()
        {
            var code = "class C { void M() { var s = \"\"; for(int i=0;i<2;i++) { s += \"a\"; } } }";
            var results = GetResults(code);
            Assert.Contains(results, r => r.Message.Contains("String concatenation inside loops"));
        }

        [Fact]
        public void Review_StringConcatWithStringEmpty_Suggests()
        {
            var code = "class C { void M() { var s = string.Empty; for(int i=0;i<2;i++) { s += \"a\"; } } }";
            var results = GetResults(code);
            Assert.Contains(results, r => r.Message.Contains("String concatenation inside loops"));
        }

        [Fact]
        public void Review_NonStringConcatInLoop_NoSuggestion()
        {
            var code = @"class C { void M() { int s = 0; for(int i=0;i<2;i++) { s += i; } } }";
            var results = GetResults(code);
            Assert.Empty(results);
        }

    
    }
}