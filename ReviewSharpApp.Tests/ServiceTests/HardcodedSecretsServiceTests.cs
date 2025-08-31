using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ReviewSharp.Services;
using ReviewSharp.Models;
using Xunit;
using System.Linq;
using ReviewSharpApp.Tests.TestHelpers;

namespace ReviewSharpApp.Tests.ServiceTests
{
    public class HardcodedSecretsServiceTests
    {
        private static List<CodeReviewResult> GetResults(string code)
        {
            // Arrange
            var service = new HardcodedSecretsService();
            var root = CodeParsing.ParseCompilation(code);
            
            // Act
            var results = service.Review(root);
            return results;
        }

        [Theory]
        [InlineData("string password = \"abcd1234\";", true)]
        [InlineData("string secret = \"12345678\";", true)]
        [InlineData("string apiKey = \"ABCDEFGH\";", true)]
        [InlineData("string token = \"qwerty1234\";", true)]
        [InlineData("string accessKey = \"abcd\";", false)] // too short
        [InlineData("string connectionString = \"Server=.;Database=Test;\";", false)] // not matched by value pattern
        [InlineData("string username = \"admin\";", false)]
        [InlineData("string password;", false)] // no value
        public void  Review_HardcodedSecret_Variable_ShouldWarnWhenDetected(string codeLine, bool shouldWarn)
        {
            var source = $@"
            class TestClass {{
                void Do() {{
                    {codeLine}
                }}
            }}";

            var results = GetResults(source);

            if (shouldWarn)
            {
                var result = Assert.Single(results);
                Assert.Equal("Hardcoded Secret/Password", result.RuleName);
            }
            else
            {
                Assert.Empty(results);
            }
        }
    }
}
