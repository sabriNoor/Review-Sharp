using Xunit;
using ReviewSharp.Services;
using ReviewSharp.Models;
using Microsoft.CodeAnalysis.CSharp;
using System.Linq;
using ReviewSharpApp.Tests.TestHelpers;

namespace ReviewSharpApp.Tests.ServiceTests
{
    public class UnusedSymbolServiceTests
    {
        private static List<CodeReviewResult> GetResults(string source)
        {
            // Arrange
            var service = new UnusedSymbolService();
            var root = CodeParsing.ParseCompilation(source);

            // Act
            var results = service.Review(root);
            return results;
        }

        [Theory]
        [InlineData("class C { private int x; }", "Private field 'x' is declared but never used.")]
        [InlineData("class C { private int x; void M() { int y = x; } }", null)]
        public void Review_PrivateFieldUsage_WarnsIfUnused(string code, string? expectedMessage)
        {
            var results = GetResults(code);
            if (expectedMessage == null)
                Assert.DoesNotContain(results, r => r.Message.Contains("Private field"));
            else
                Assert.Contains(results, r => r.Message.Contains(expectedMessage));
        }

        [Theory]
        [InlineData("class C { void M(int a) { } }", "Parameter 'a' is never used within the method.")]
        [InlineData("class C { void M(int a) { int x = a; } }", null)]
        public void Review_MethodParameterUsage_WarnsIfUnused(string code, string? expectedMessage)
        {
            var results = GetResults(code);
            if (expectedMessage == null)
                Assert.DoesNotContain(results, r => r.Message.Contains("Parameter"));
            else
                Assert.Contains(results, r => r.Message.Contains(expectedMessage));
        }

        [Theory]
        [InlineData("class C { void M() { int x; } }", "Local variable 'x' is declared but never used.")]
        [InlineData("class C { void M() { int x = 1; Console.WriteLine(x); } }", null)]
        public void Review_LocalVariableUsage_WarnsIfUnused(string code, string? expectedMessage)
        {
            var results = GetResults(code);
            if (expectedMessage == null)
                Assert.DoesNotContain(results, r => r.Message.Contains("Local variable"));
            else
                Assert.Contains(results, r => r.Message.Contains(expectedMessage));
        }

        [Theory]
        [InlineData("class C { void M() { void Local(int a) { } Local(1); } }", "Parameter 'a' is never used within the local function.")]
        [InlineData("class C { void M() { void Local(int a) { int x = a; } Local(1); } }", null)]
        public void Review_LocalFunctionParameterUsage_WarnsIfUnused(string code, string? expectedMessage)
        {
            var results = GetResults(code);
            if (expectedMessage == null)
                Assert.DoesNotContain(results, r => r.Message.Contains("local function"));
            else
                Assert.Contains(results, r => r.Message.Contains(expectedMessage));
        }

        [Fact]
        public void Review_MultipleUnusedSymbols_AllReported()
        {
            var code = @"
                class C {
                    private int a;
                    void M(int b) {
                        int c;
                        void Local(int d) { }
                        Local(1);
                    }
                }";
            var results = GetResults(code);
            Assert.Contains(results, r => r.Message.Contains("Private field 'a'"));
            Assert.Contains(results, r => r.Message.Contains("Parameter 'b'"));
            Assert.Contains(results, r => r.Message.Contains("Local variable 'c'"));
            Assert.Contains(results, r => r.Message.Contains("Parameter 'd'"));
        }

        [Fact]
        public void Review_NoUnusedSymbols_NoWarnings()
        {
            var code = @"
                class C {
                    private int a;
                    void M(int b) {
                        int c = b;
                        void Local(int d) { int x = d; Console.WriteLine(x);}
                        Local(a);
                        Console.WriteLine(c);
                    }
                }";
            var results = GetResults(code);
            Assert.Empty(results);
        }
    }
}