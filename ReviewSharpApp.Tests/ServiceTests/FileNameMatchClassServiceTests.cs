using ReviewSharp.Models;
using ReviewSharp.Services;
using ReviewSharpApp.Tests.TestHelpers;

namespace ReviewSharpApp.Tests.ServiceTests
{
    public class FileNameMatchClassServiceTests
    {
        private const string RuleName = "File Name Mismatch";
        private static List<CodeReviewResult> GetResults(string source, string filePath = "")
        {
            var service = new FileNameMatchClassService();
            var root = CodeParsing.ParseCompilation(source, filePath);
            return service.Review(root);
        }


        [Fact]
        public void ClassNameMatchesFileName_ShouldReturnNoWarnings()
        {
            var filePath = @"C:\Projects\TestClass.cs";
            var source = @"
            namespace MyApp
            {
                public class TestClass { }
            }";

            var results = GetResults(source, filePath);

            Assert.Empty(results);
        }

        [Fact]
        public void ClassNameDoesNotMatchFileName_ShouldReturnWarning()
        {
            var filePath = @"C:\Projects\DifferentName.cs";
            var source = @"
            namespace MyApp
            {
                public class TestClass { }
            }";

            var results = GetResults(source, filePath);

            Assert.Single(results);
            var result = results[0];
            Assert.Equal(RuleName, result.RuleName);
            Assert.Contains("TestClass", result.Message);
            Assert.Contains("DifferentName", result.Message);
            Assert.Equal(4, result.LineNumber);
        }
        [Fact]
        public void MultipleClassesWithOneMismatch_ShouldReturnSingleWarning()
        {
            var filePath = @"C:\Projects\MyFile.cs";
            var source = @"
            namespace MyApp
            {
                public class MyFile { }
                public class AnotherClass { }
            }";

            var results = GetResults(source, filePath);

            Assert.Single(results);
            var result = results[0];
            Assert.Equal(RuleName, result.RuleName);
            Assert.Contains("AnotherClass", result.Message);
            Assert.Contains("MyFile", result.Message);
            Assert.Equal(5, result.LineNumber);
        }
    }
}