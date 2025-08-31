using Xunit;
using ReviewSharp.Services;
using ReviewSharp.Models;
using ReviewSharpApp.Tests.TestHelpers;
using System.Collections.Generic;

namespace ReviewSharpApp.Tests.ServiceTests
{
	public class SwitchStatementServiceTests
	{
		private static List<CodeReviewResult> GetResults(string source)
		{
			var service = new SwitchStatementService();
			var root = CodeParsing.ParseCompilation(source);
			return service.Review(root);
		}

		[Theory]
	[InlineData("class C { void M(int x) { if (x == 1) { } else if (x == 2) { } else if (x == 3) { } else if (x == 4) { } } }", true)]
	[InlineData("class C { void M(int x) { if (x == 1) { } else if (x == 2) { } else if (y == 3) { } else if (x == 4) { } } }", false)]
	[InlineData("class C { void M(int x) { if (x == 1) { } else if (x == 2) { } else { } } }", false)]
		public void Review_IfElseChains_SwitchSuggestion(string code, bool shouldSuggest)
		{
			var results = GetResults(code);
			if (shouldSuggest)
				Assert.Contains(results, r => r.Message.Contains("switch statement"));
			else
				Assert.DoesNotContain(results, r => r.Message.Contains("switch statement"));
		}

		[Fact]
		public void Review_NullRoot_ReturnsEmpty()
		{
			var service = new SwitchStatementService();
			var results = service.Review(null);
			Assert.Empty(results);
		}

		[Fact]
		public void Review_NoIfElseChains_NoSuggestion()
		{
			var code = @"class C { void M() { int x = 0; } }";
			var results = GetResults(code);
			Assert.Empty(results);
		}

		[Fact]
		public void Review_IfElseChainWithDifferentVariables_NoSuggestion()
		{
			var code = @"class C { void M(int x, int y) { if (x == 1) { } else if (y == 2) { } else if (x == 3) { } else if (y == 4) { } } }";
			var results = GetResults(code);
			Assert.Empty(results);
		}

		[Fact]
		public void Review_IfElseChainWithLessThanThreeElseIfs_NoSuggestion()
		{
			var code = @"class C { void M(int x) { if (x == 1) { } else if (x == 2) { } } }";
			var results = GetResults(code);
			Assert.Empty(results);
		}

		[Fact]
		public void Review_IfElseChainWithMethodCallCondition_SwitchSuggestion()
		{
			var code = "class C { void M(string s) { if (s.Equals(\"a\")) { } else if (s.Equals(\"b\")) { } else if (s.Equals(\"c\")) { } else if (s.Equals(\"d\")) { } } }";
			var results = GetResults(code);
			Assert.Contains(results, r => r.Message.Contains("switch statement"));
		}
	}
}
// Create SwitchStatementServiceTests.cs with comprehensive test cases for all scenarios in SwitchStatementService.