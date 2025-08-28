using Xunit;
using ReviewSharp.Services;
using ReviewSharp.Models;
using ReviewSharpApp.Tests.TestHelpers;
using System.Collections.Generic;

namespace ReviewSharp.Tests
{
    public class EmptyFinallyBlockServiceTests
    {
        private static List<CodeReviewResult> GetResults(string source)
        {
            var service = new EmptyFinallyBlockService();
            var root = CodeParsing.ParseCompilation(source);
            return service.Review(root);
        }

        [Fact]
        public void Review_NoFinallyBlock_NoWarning()
        {
            var code = "class C { void M() { try { int x = 1; } catch { } } }";
            var results = GetResults(code);
            Assert.Empty(results);
        }

        [Fact]
        public void Review_EmptyFinallyBlock_Warns()
        {
            var code = "class C { void M() { try { int x = 1; } finally { } } }";
            var results = GetResults(code);
            Assert.Single(results);
            Assert.Contains("Empty finally block detected", results[0].Message);
        }

        [Fact]
        public void Review_NonEmptyFinallyBlock_NoWarning()
        {
            var code = "class C { void M() { try { int x = 1; } finally { int y = 2; } } }";
            var results = GetResults(code);
            Assert.Empty(results);
        }

        [Fact]
        public void Review_MultipleFinallyBlocks_AllReported()
        {
            var code = @"class C { void M() { try { } finally { } try { } finally { int x = 1; } try { } finally { } } }";
            var results = GetResults(code);
            Assert.Equal(2, results.Count);
            Assert.All(results, r => Assert.Contains("Empty finally block detected", r.Message));
        }

        [Fact]
        public void Review_NullRoot_ReturnsEmpty()
        {
            var service = new EmptyFinallyBlockService();
            var results = service.Review(null);
            Assert.Empty(results);
        }
    }
}
