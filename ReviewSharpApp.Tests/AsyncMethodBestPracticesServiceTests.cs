using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ReviewSharp.Services;
using ReviewSharp.Models;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using ReviewSharpApp.Tests.TestHelpers;

namespace ReviewSharp.Tests
{
    public class AsyncMethodBestPracticesServiceTests
    {
        private static List<CodeReviewResult> GetResults(string source)
        {
            // Arrange
            var service = new AsyncMethodBestPracticesService();
            var root = CodeParsing.ParseCompilation(source);

            // Act
            var results = service.Review(root);
            return results;
        }

        [Fact]
        public void DoSomethingAsync_MissingAwait_EmitsWarning()
        {
            var source = @"
                using System.Threading.Tasks;
                class TestClass
                {
                    public async Task DoSomethingAsync()
                    {
                        int x = 1;
                    }
                }";
            var results = GetResults(source);
            var result = results.FirstOrDefault(r => r.RuleName == "Async Method Missing Await");
            Assert.NotNull(result);
            Assert.Contains("DoSomethingAsync", result.Message);
            Assert.Equal(5, result.LineNumber);
        }

        [Fact]
        public void DoSomethingAsyncVoid_ReturnsVoid_EmitsWarning()
        {
            var source = @"
                using System.Threading.Tasks;
                class TestClass
                {
                    public async void DoSomethingAsyncVoid() { }
                }";
            var results = GetResults(source);
            var result = results.FirstOrDefault(r => r.RuleName == "Async Method Returns Void");
            Assert.NotNull(result);
            Assert.Contains("DoSomethingAsyncVoid", result.Message);
            Assert.Equal(5, result.LineNumber);
        }

        [Fact]
        public void DoSomethingAsync_BlockingCall_EmitsWarning()
        {
            var source = @"
                using System.Threading.Tasks;
                class TestClass
                {
                    public async Task DoSomethingAsync()
                    {
                        Task.Delay(100).Wait();
                    }
                }";
            var results = GetResults(source);
            var result = results.FirstOrDefault(r => r.RuleName == "Async Method Blocking Call");
            Assert.NotNull(result);
            Assert.Contains("DoSomethingAsync", result.Message);
            Assert.Equal(5, result.LineNumber);
        }

        [Fact]
        public void DoSomethingAsync_CallWithoutAwait_EmitsWarning()
        {
            var source = @"
                using System.Threading.Tasks;
                class TestClass
                {
                    public async Task DoSomethingAsync()
                    {
                        AnotherAsync();
                    }
                    public async Task AnotherAsync() { }
                }";
            var results = GetResults(source);
            var result = results.FirstOrDefault(r => r.RuleName == "Async Call Without Await");
            Assert.NotNull(result);
            Assert.Contains("AnotherAsync", result.Message);
            Assert.Equal(7, result.LineNumber); // The line where AnotherAsync is called
        }

        [Fact]
        public void DoSomethingAsync_InterfaceMethodWithNonTaskReturn_EmitsWarning()
        {
            var source = @"
                using System.Threading.Tasks;
                interface ITestInterface
                {
                    void DoSomethingAsync();
                }";
            var results = GetResults(source);
            var result = results.FirstOrDefault(r => r.RuleName == "Interface Async Method Return Type");
            Assert.NotNull(result);
            Assert.Contains("DoSomethingAsync", result.Message);
            Assert.Equal(5, result.LineNumber);
        }
        
        [Fact]
        public void DoSomethingAsync_CorrectAsyncMethod_NoWarning()
        {
            var source = @"
                using System.Threading.Tasks;
                class TestClass
                {
                    public async Task DoSomethingAsync()
                    {
                        await Task.Delay(100);
                    }
                }";
            var results = GetResults(source);
            Assert.Empty(results); // No warnings should be returned
        }
    }
}
