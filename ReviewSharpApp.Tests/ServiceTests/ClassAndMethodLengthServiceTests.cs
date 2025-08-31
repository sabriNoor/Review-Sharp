using Xunit;
using ReviewSharp.Services;
using ReviewSharp.Models;
using ReviewSharpApp.Tests.TestHelpers;
using System.Collections.Generic;

namespace ReviewSharpApp.Tests.ServiceTests
{
    public class ClassAndMethodLengthServiceTests
    {
        private static List<CodeReviewResult> GetResults(string source)
        {
            var service = new ClassAndMethodLengthService();
            var root = CodeParsing.ParseCompilation(source);
            return service.Review(root);
        }

        [Fact]
        public void Review_ShortClassAndMethod_NoWarnings()
        {
            var code = "class C { void M() { int x = 1; } }";
            var results = GetResults(code);
            Assert.Empty(results);
        }

        [Fact]
        public void Review_LongClass_Warns()
        {
            var classBody = string.Join("\n", Enumerable.Repeat("int x = 1;", 301));
            var code = $"class C {{ {classBody} }}";
            var results = GetResults(code);
            Assert.Contains(results, r => r.Message.Contains("Class 'C' is 301 lines long"));
        }

        [Fact]
        public void Review_LongMethod_Warns()
        {
            var methodBody = string.Join("\n", Enumerable.Repeat("int x = 1;", 51));
            var code = $"class C {{ void M() {{ {methodBody} }} }}";
            var results = GetResults(code);
            Assert.Contains(results, r => r.Message.Contains("Method 'M' is 51 lines long"));
        }

        [Fact]
        public void Review_LongClassAndMethod_BothWarn()
        {
            var classBody = string.Join("\n", Enumerable.Repeat("int x = 1;", 301));
            var methodBody = string.Join("\n", Enumerable.Repeat("int y = 2;", 51));
            var code = $"class C {{ {classBody} void M() {{ {methodBody} }} }}";
            var results = GetResults(code);
            Assert.Contains(results, r => r.Message.Contains("Class 'C' is 351 lines long"));
            Assert.Contains(results, r => r.Message.Contains("Method 'M' is 51 lines long"));
        }

        [Fact]
        public void Review_NullRoot_ReturnsEmpty()
        {
            var service = new ClassAndMethodLengthService();
            var results = service.Review(null);
            Assert.Empty(results);
        }
    }
}
