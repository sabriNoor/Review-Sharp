using Xunit;
using ReviewSharp.Services;
using ReviewSharp.Models;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using System.Linq;
using ReviewSharpApp.Tests.TestHelpers;

namespace ReviewSharpApp.Tests.ServiceTests
{
    public class SyntaxCheckServiceTests
    {            
        private static List<CodeReviewResult> GetResults(string source)
        {
            // Arrange
            var service = new SyntaxCheckService();
            var root = CodeParsing.ParseCompilation(source);

            // Act
            var results = service.Review(root);
            return results;
        }
        [Fact]
        public void Review_ValidCode_NoErrors()
        {
            var code = "class C { void M() {} }";
            var results = GetResults(code);
            Assert.Empty(results);
        }

        [Fact]
        public void Review_CodeWithSyntaxError_ReturnsError()
        {
            var code = "class C { void M( }"; // Missing closing parenthesis
            var results = GetResults(code);
            Assert.Contains(results, r => r.RuleName == "SyntaxCheck" && r.Severity == "Error");
        }

        [Fact]
        public void Review_NullRoot_ReturnsEmpty()
        {
            var results = new SyntaxCheckService().Review(null);
            Assert.Empty(results);
        }

        [Fact]
        public void Review_MissingSemicolon_ReturnsError()
        {
            var code = "class C { void M() { int x = 1 } }"; // Missing semicolon
            var results = GetResults(code);
            Assert.Contains(results, r => r.RuleName == "SyntaxCheck" && r.Severity == "Error");
        }

        [Fact]
        public void Review_UnclosedStringLiteral_ReturnsError()
        {
            var code = "class C { void M() { string s = \"Hello; } }"; // Unclosed string
            var results = GetResults(code);
            Assert.Contains(results, r => r.RuleName == "SyntaxCheck" && r.Severity == "Error");
        }

        [Fact]
        public void Review_MultipleSyntaxErrors_ReturnsMultipleErrors()
        {
            var code = "class C { void M( int x { int y = ; } }"; // Multiple errors
            var results = GetResults(code);
            Assert.True(results.Count > 1);
        }

        [Fact]
        public void Review_EmptyCode_ReturnsEmpty()
        {
            var code = "";
            var results = GetResults(code);
            Assert.Empty(results);
        }

        [Fact]
        public void Review_OnlyComment_NoErrors()
        {
            var code = "// just a comment";
            var results = GetResults(code);
            Assert.Empty(results);
        }

        [Fact]
        public void Review_UsingDirective_NoErrors()
        {
            var code = "using System;";
            var results = GetResults(code);
            Assert.Empty(results);
        }

        [Fact]
        public void Review_CodeFromFile_ReturnsError()
        {
            var filePath = System.IO.Path.Combine("../../../", "TestCodeFiles", "SampleCodeWithSyntaxErrors.txt");
            var code = System.IO.File.ReadAllText(filePath);
            var results = GetResults(code);
            Assert.Contains(results, r => r.RuleName == "SyntaxCheck" && r.Severity == "Error");
        }

        [Fact]
        public void Review_DeeplyNestedBraces_ReturnsError()
        {
            var code = "class C { void M() { if (true) { if (false) { int x = 1; } } }"; // Missing closing braces
            var results = GetResults(code);
            Assert.Contains(results, r => r.RuleName == "SyntaxCheck" && r.Severity == "Error");
        }

        [Fact]
        public void Review_UnicodeCharactersInCode_NoErrors()
        {
            var code = "class C { void M() { string s = \"Привет\"; } }"; // Valid unicode string
            var results = GetResults(code);
            Assert.Empty(results);
        }

        [Fact]
        public void Review_CommentInsideStringLiteral_NoErrors()
        {
            var code = "class C { void M() { string s = \"// not a comment\"; } }";
            var results = GetResults(code);
            Assert.Empty(results);
        }

        [Fact]
        public void Review_OnlyWhitespace_ReturnsEmpty()
        {
            var code = "   \n   \t  ";
            var results = GetResults(code);
            Assert.Empty(results);
        }

        [Fact]
        public void Review_RegionDirective_NoErrors()
        {
            var code = "#region Test\nclass C { }\n#endregion";
            var results = GetResults(code);
            Assert.Empty(results);
        }

        [Fact]
        public void Review_PreprocessorError_ReturnsError()
        {
            var code = "#error This is an error\nclass C { }";
            var results = GetResults(code);
            Assert.Contains(results, r => r.RuleName == "SyntaxCheck" && r.Severity == "Error");
        }

        [Fact]
        public void Review_InvalidGenericTypeSyntax_ReturnsError()
        {
            var code = "class C<T { }"; // Missing closing angle bracket
            var results = GetResults(code);
            Assert.Contains(results, r => r.RuleName == "SyntaxCheck" && r.Severity == "Error");
        }

        [Fact]
        public void Review_InvalidLambdaSyntax_ReturnsError()
        {
            var code = "class C { void M() { var f = x => ; } }"; // Invalid lambda body
            var results = GetResults(code);
            Assert.Contains(results, r => r.RuleName == "SyntaxCheck" && r.Severity == "Error");
        }

        [Fact]
        public void Review_MissingNamespaceKeyword_ReturnsError()
        {
            var code = "namespace { class C {} }"; // Missing namespace name
            var results = GetResults(code);
            Assert.Contains(results, r => r.RuleName == "SyntaxCheck" && r.Severity == "Error");
        }

        [Fact]
        public void Review_MissingClassKeyword_ReturnsError()
        {
            var code = "C {}"; // Missing 'class' keyword
            var results = GetResults(code);
            Assert.Contains(results, r => r.RuleName == "SyntaxCheck" && r.Severity == "Error");
        }

        [Fact]
        public void Review_MissingParenthesisInMethod_ReturnsError()
        {
            var code = "class C { void M( { } }";
            var results = GetResults(code);
            Assert.Contains(results, r => r.Severity == "Error");
        }

        [Fact]
        public void Review_MissingCommaInParameterList_ReturnsError()
        {
            var code = "class C { void M(int a int b) { } }";
            var results = GetResults(code);
            Assert.Contains(results, r => r.Severity == "Error");
        }

        [Fact]
        public void Review_MissingSemicolonInField_ReturnsError()
        {
            var code = "class C { int x }";
            var results = GetResults(code);
            Assert.Contains(results, r => r.Severity == "Error");
        }

        [Fact]
        public void Review_MissingEqualsInAssignment_ReturnsError()
        {
            var code = "class C { void M() { int x 5; } }";
            var results = GetResults(code);
            Assert.Contains(results, r => r.Severity == "Error");
        }

        [Fact]
        public void Review_MissingTypeInField_ReturnsError()
        {
            var code = "class C { x = 5; }";
            var results = GetResults(code);
            Assert.Contains(results, r => r.Severity == "Error");
        }

        [Fact]
        public void Review_MissingIdentifierInField_ReturnsError()
        {
            var code = "class C { int = 5; }";
            var results = GetResults(code);
            Assert.Contains(results, r => r.Severity == "Error");
        }

        [Fact]
        public void Review_MissingOpenBraceInClass_ReturnsError()
        {
            var code = "class C void M() {} }";
            var results = GetResults(code);
            Assert.Contains(results, r => r.Severity == "Error");
        }

        [Fact]
        public void Review_MissingCloseBraceInClass_ReturnsError()
        {
            var code = "class C { void M() {} ";
            var results = GetResults(code);
            Assert.Contains(results, r => r.Severity == "Error");
        }

        [Fact]
        public void Review_MissingColonInInheritance_ReturnsError()
        {
            var code = "class C BaseClass { }";
            var results = GetResults(code);
            Assert.Contains(results, r => r.Severity == "Error");
        }

        [Fact]
        public void Review_MissingInterfaceKeyword_ReturnsError()
        {
            var code = "IExample { void M(); }";
            var results = GetResults(code);
            Assert.Contains(results, r => r.Severity == "Error");
        }

        [Fact]
        public void Review_MissingEnumKeyword_ReturnsError()
        {
            var code = "MyEnum { Value1, Value2 }";
            var results = GetResults(code);
            Assert.Contains(results, r => r.Severity == "Error");
        }

        [Fact]
        public void Review_MissingCommaInEnum_ReturnsError()
        {
            var code = "enum MyEnum { Value1 Value2 }";
            var results = GetResults(code);
            Assert.Contains(results, r => r.Severity == "Error");
        }

        [Fact]
        public void Review_MissingValueInEnum_ReturnsError()
        {
            var code = "enum MyEnum { , Value2 }";
            var results = GetResults(code);
            Assert.Contains(results, r => r.Severity == "Error");
        }

        [Fact]
        public void Review_MissingCloseParenInMethod_ReturnsError()
        {
            var code = "class C { void M( { } }";
            var results = GetResults(code);
            Assert.Contains(results, r => r.Severity == "Error");
        }

        [Fact]
        public void Review_MissingBodyInLambda_ReturnsError()
        {
            var code = "class C { void M() { var f = x => ; } }";
            var results = GetResults(code);
            Assert.Contains(results, r => r.Severity == "Error");
        }

        [Fact]
        public void Review_MissingSemicolonInUsing_ReturnsError()
        {
            var code = "using System";
            var results = GetResults(code);
            Assert.Contains(results, r => r.Severity == "Error");
        }

        [Theory]
        [InlineData("class C { void M() { throw new System.Exception(); } }", 0)] // Valid
        [InlineData("class C { void M() { throw new System.Exception() } }", 1)] // Missing semicolon
        public void Review_ThrowStatement_VariousCases_ReturnsExpectedErrorCount(string code, int expectedErrors)
        {
            var results = GetResults(code);
            Assert.Equal(expectedErrors, results.Count);
        }
    }
}
