using Xunit;
using ReviewSharp.Services;
using ReviewSharp.Models;
using ReviewSharpApp.Tests.TestHelpers;
using System.Collections.Generic;
using System.Linq;

namespace ReviewSharpApp.Tests.ServiceTests
{
    public class DuplicateCodeServiceTests
    {
        private static List<CodeReviewResult> GetResults(string source)
        {
            var service = new DuplicateCodeService();
            var root = CodeParsing.ParseCompilation(source);
            return service.Review(root);
        }

        [Fact]
        public void Review_NoMethods_NoDuplicates()
        {
            var code = "class C { int x; }";
            var results = GetResults(code);
            Assert.Empty(results);
        }

        [Fact]
        public void Review_SingleMethod_NoDuplicates()
        {
            var code = "class C { void M() { int x = 1; } }";
            var results = GetResults(code);
            Assert.Empty(results);
        }

        [Fact]
        public void Review_TwoIdenticalMethods_DetectsDuplicate()
        {
            var code = @"class C { void M1() { int x = 1; } void M2() { int x = 1; } }";
            var results = GetResults(code);
            Assert.Equal(2, results.Count);
            Assert.All(results, r => Assert.Contains("Duplicate code detected", r.Message));
        }

        [Fact]
        public void Review_TwoDifferentMethods_NoDuplicates()
        {
            var code = @"class C { void M1() { int x = 1; } void M2() { int y = 2; } }";
            var results = GetResults(code);
            Assert.Empty(results);
        }

        [Fact]
        public void Review_MethodAndLocalFunction_DetectsDuplicate()
        {
            var code = @"class C { void M1() { int x = 1; } void M2() { void L() { int x = 1; } } }";
            var results = GetResults(code);
            Assert.Equal(2, results.Count);
            Assert.All(results, r => Assert.Contains("Duplicate code detected", r.Message));
        }

        [Fact]
        public void Review_TwoIdenticalLocalFunctions_DetectsDuplicate()
        {
            var code = @"class C { void M() { void L1() { int x = 1; } void L2() { int x = 1; } } }";
            var results = GetResults(code);
            Assert.Equal(2, results.Count);
            Assert.All(results, r => Assert.Contains("Duplicate code detected", r.Message));
        }

        [Fact]
        public void Review_MethodsWithDifferentParameterNames_DetectsDuplicate()
        {
            var code = @"class C { void M1(int a) { int x = a; } void M2(int b) { int x = b; } }";
            var results = GetResults(code);
            Assert.Equal(2, results.Count);
            Assert.All(results, r => Assert.Contains("Duplicate code detected", r.Message));
        }

        [Fact]
        public void Review_NullRoot_ReturnsEmpty()
        {
            var service = new DuplicateCodeService();
            var results = service.Review(null);
            Assert.Empty(results);
        }

        [Fact]
        public void Review_DuplicateWithDifferentAccessModifiers_DetectsDuplicate()
        {
            var code = @"class C { public void M1() { int x = 1; } private void M2() { int x = 1; } }";
            var results = GetResults(code);
            Assert.Equal(2, results.Count);
            Assert.All(results, r => Assert.Contains("Duplicate code detected", r.Message));
        }

        [Fact]
        public void Review_DuplicateInNestedClasses_DetectsDuplicate()
        {
            var code = @"class Outer { class Inner1 { void M() { int x = 1; } } class Inner2 { void M() { int x = 1; } } }";
            var results = GetResults(code);
            Assert.Equal(2, results.Count);
            Assert.All(results, r => Assert.Contains("Duplicate code detected", r.Message));
        }

        [Fact]
        public void Review_ExpressionBodiedMethods_DetectsDuplicate()
        {
            var code = @"class C { int M1() => 42; int M2() => 42; }";
            var results = GetResults(code);
            Assert.Equal(2, results.Count);
            Assert.All(results, r => Assert.Contains("Duplicate code detected", r.Message));
        }
        
        [Fact]
        public void Review_LongDuplicateMethods_DetectsDuplicate()
        {
            var longBody = string.Join("\n", Enumerable.Repeat("int x = 1; x+=2;", 100));
            var code = $"class C {{ void M1() {{ {longBody} }} void M2() {{ {longBody} }} }}";
            var results = GetResults(code);
            Assert.Equal(2, results.Count);
            Assert.All(results, r => Assert.Contains("Duplicate code detected", r.Message));
        }

    }
}
