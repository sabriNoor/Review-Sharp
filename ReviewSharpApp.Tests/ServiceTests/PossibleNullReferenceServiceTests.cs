using Xunit;
using ReviewSharp.Services;
using ReviewSharp.Models;
using ReviewSharpApp.Tests.TestHelpers;
using System.Collections.Generic;

namespace ReviewSharpApp.Tests.ServiceTests
{
    public class PossibleNullReferenceServiceTests
    {
        private static List<CodeReviewResult> GetResults(string source)
        {
            var service = new PossibleNullReferenceService();
            var root = CodeParsing.ParseCompilation(source);
            return service.Review(root);
        }

        [Fact]
        public void Review_NullableLocalVariableInBinaryExpression_Warns()
        {
            var code = "class C { void M() { int? x = null; int y = x + 1; } }";
            var results = GetResults(code);
            Assert.Contains(results, r => r.Message.Contains("Possible null reference on 'x'"));
        }

        [Fact]
        public void Review_NullablePropertyInBinaryExpression_Warns()
        {
            var code = "class C { public int? p { get; set; } void M() { int y = p + 1; } }";
            var results = GetResults(code);
            Assert.Contains(results, r => r.Message.Contains("Possible null reference on 'p'"));
        }

        [Fact]
        public void Review_NullableFieldInAssignment_Warns()
        {
            var code = "class C { int? f; void M() { int y; y = f; } }";
            var results = GetResults(code);
            Assert.Contains(results, r => r.Message.Contains("Possible null reference on 'f'"));
        }

        [Fact]
        public void Review_NullableParameterInReturn_Warns()
        {
            var code = "class C { int M(int? x) { return x; } }";
            var results = GetResults(code);
            Assert.Contains(results, r => r.Message.Contains("Possible null reference on 'x'"));
        }

        [Fact]
        public void Review_NullableParameterInMemberAccess_Warns()
        {
            var code = "class C { void M(User? user) { user.Login(); } }";
            var results = GetResults(code);
            Assert.Contains(results, r => r.Message.Contains("Possible null reference on 'user'"));
        }

        [Fact]
        public void Review_NullableParameterInAssignment_Warns()
        {
            var code = "class C { void M(int? x) { int y; y = x; } }";
            var results = GetResults(code);
            Assert.Contains(results, r => r.Message.Contains("Possible null reference on 'x'"));
        }

        [Fact]
        public void Review_NullableParameterWithNullCheck_NoWarning()
        {
            var code = "class C { void M(int? x) { if(x != null) { int y = x + 1; } } }";
            var results = GetResults(code);
            Assert.DoesNotContain(results, r => r.Message.Contains("Possible null reference"));
        }

        [Fact]
        public void Review_NullablePropertyWithNullCheck_NoWarning()
        {
            var code = "class C { public int? p { get; set; } void M() { if(p != null) { int y = p + 1; } } }";
            var results = GetResults(code);
            Assert.DoesNotContain(results, r => r.Message.Contains("Possible null reference"));
        }

        [Fact]
        public void Review_NullableFieldWithNullCheck_NoWarning()
        {
            var code = "class C { int? f; void M() { if(f != null) { int y = f + 1; } } }";
            var results = GetResults(code);
            Assert.DoesNotContain(results, r => r.Message.Contains("Possible null reference"));
        }

        [Fact]
        public void Review_NullableParameterWithIsNotNullCheck_NoWarning()
        {
            var code = "class C { void M(int? x) { if(x is not null) { int y = x + 1; } } }";
            var results = GetResults(code);
            Assert.DoesNotContain(results, r => r.Message.Contains("Possible null reference"));
        }

        [Fact]
        public void Review_NonNullableVariable_NoWarning()
        {
            var code = "class C { void M() { int x = 1; int y = x + 1; } }";
            var results = GetResults(code);
            Assert.DoesNotContain(results, r => r.Message.Contains("Possible null reference"));
        }

        [Fact]
        public void Review_NullableCheckedWithNestedIf_NoWarning()
        {
            var code = "class C { public int? p { get; set; } void M() { if(p != null) { if(p != null) { int y = p + 1; } } } }";
            var results = GetResults(code);
            Assert.DoesNotContain(results, r => r.Message.Contains("Possible null reference on 'p'"));
        }

        [Fact]
        public void Review_NullableCheckedWithPatternMatching_NoWarning()
        {
            var code = "class C { public int? p { get; set; } void M() { if(p is int value) { int y = value; } } }";
            var results = GetResults(code);
            Assert.DoesNotContain(results, r => r.Message.Contains("Possible null reference on 'p'"));
        }

        [Fact]
        public void Review_NullableCheckedWithMultipleChecks_NoWarning()
        {
            var code = "class C { public int? p { get; set; } void M() { if(p != null) { int y = p + 1; } if(p is not null) { int z = p + 2; } } }";
            var results = GetResults(code);
            Assert.DoesNotContain(results, r => r.Message.Contains("Possible null reference on 'p'"));
        }

    
    }
}
