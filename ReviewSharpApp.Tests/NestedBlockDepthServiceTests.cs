using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using Xunit;
using ReviewSharp.Services;
using System.Linq;

namespace ReviewSharp.Tests
{
    public class NestedBlockDepthServiceTests
    {
        [Fact]
        public void Detects_Excessive_Nested_Block_Depth()
        {
            var code = @"
            public class TestClass {
                public void DeepMethod() {
                    if (true) {
                        for (int i = 0; i < 10; i++) {
                            while (true) {
                                if (false) {
                                    // Depth 4
                                }
                            }
                        }
                    }
                }
            }
            ";
            var tree = CSharpSyntaxTree.ParseText(code);
            var root = tree.GetCompilationUnitRoot();
            var service = new NestedBlockDepthService();
            var results = service.Review(root);
            Assert.Contains(results, r => r.RuleName == "Nested Block Depth");
        }

        [Theory]
        [InlineData(@"public class TestClass { public void ShallowMethod() { if (true) { for (int i = 0; i < 10; i++) { } } } }")]
        public void DoesNotDetect_When_Depth_Is_Allowed(string code)
        {
            var tree = CSharpSyntaxTree.ParseText(code);
            var root = tree.GetCompilationUnitRoot();
            var service = new NestedBlockDepthService();
            var results = service.Review(root);
            Assert.DoesNotContain(results, r => r.RuleName == "Nested Block Depth");
        }
    }
}
