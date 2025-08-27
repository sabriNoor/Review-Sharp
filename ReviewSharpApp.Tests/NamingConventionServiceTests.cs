using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ReviewSharp.Models;
using ReviewSharp.Services;
using ReviewSharpApp.Tests.TestHelpers;

namespace ReviewSharpApp.Tests;

public class NamingConventionServiceTests
{
    private static CompilationUnitSyntax Parse(string source) => CodeParsing.ParseCompilation(source);

    private static List<CodeReviewResult> GetResults(string source)
    {
        // Arrange
        var service = new NamingConventionService();
        var root = Parse(source);

        // Act
        var results = service.Review(root);
        return results;
    }

  
    [Fact]
    public void ClassName_NotPascalCase_ReturnsWarningWithLine1()
    {
        var results = GetResults("class badClass { }");
        Assert.Contains(results, r => r.RuleName == "Class Naming Convention" && r.Severity == "Warning" && r.LineNumber == 1);
    }

    [Fact]
    public void ClassName_Compliant_NoViolation()
    {
        var results = GetResults("class GoodClass { }");
        Assert.DoesNotContain(results, r => r.RuleName == "Class Naming Convention");
    }

    [Fact]
    public void InterfaceName_NotPrefixedWithI_ReturnsErrorWithLine1()
    {
        var results = GetResults("interface Service { }");
        Assert.Contains(results, r => r.RuleName == "Interface Naming Convention" && r.Severity == "Error" && r.LineNumber == 1);
    }

    [Fact]
    public void InterfaceName_Compliant_NoViolation()
    {
        var results = GetResults("interface IGoodService { }");
        Assert.DoesNotContain(results, r => r.RuleName == "Interface Naming Convention");
    }

    [Fact]
    public void MethodName_NotPascalCase_ReturnsWarningWithLine1()
    {
        var results = GetResults("class Test { void doThing() { } }");
        Assert.Contains(results, r => r.RuleName == "Method Naming Convention" && r.Severity == "Warning" && r.LineNumber == 1);
    }

    [Fact]
    public void MethodName_Compliant_NoViolation()
    {
        var results = GetResults("class Test { void DoThing() { } }");
        Assert.DoesNotContain(results, r => r.RuleName == "Method Naming Convention");
    }

    [Fact]
    public void ParameterName_NotCamelCase_ReturnsInfoWithLine1()
    {
        var results = GetResults("class Test { void DoThing(int NotCamel) { } }");
        Assert.Contains(results, r => r.RuleName == "Parameter Naming Convention" && r.Severity == "Info" && r.LineNumber == 1);
    }

    [Fact]
    public void ParameterName_Compliant_NoViolation()
    {
        var results = GetResults("class Test { void DoThing(int someValue) { } }");
        Assert.DoesNotContain(results, r => r.RuleName == "Parameter Naming Convention");
    }

    [Fact]
    public void PrivateField_NotUnderscoreCamel_ReturnsWarningWithLine1()
    {
        var results = GetResults("class Test { private int fieldName; }");
        Assert.Contains(results, r => r.RuleName == "Field Naming Convention" && r.Severity == "Warning" && r.LineNumber == 1);
    }

    [Fact]
    public void PrivateField_Compliant_NoViolation()
    {
        var results = GetResults("class Test { private int _count; }");
        Assert.DoesNotContain(results, r => r.RuleName == "Field Naming Convention");
    }

    [Fact]
    public void Constant_NotAllCaps_ReturnsWarningWithLine1()
    {
        var results = GetResults("class Test { public const int Not_All_Caps = 1; }");
        Assert.Contains(results, r => r.RuleName == "Constant Naming Convention" && r.Severity == "Warning" && r.LineNumber == 1);
    }

    [Fact]
    public void Constant_Compliant_NoViolation()
    {
        var results = GetResults("class Test { public const int MAX_COUNT = 1; }");
        Assert.DoesNotContain(results, r => r.RuleName == "Constant Naming Convention");
    }

    [Fact]
    public void Property_NotPascalCase_ReturnsWarningWithLine1()
    {
        var results = GetResults("class Test { public int propName { get; set; } }");
        Assert.Contains(results, r => r.RuleName == "Property Naming Convention" && r.Severity == "Warning" && r.LineNumber == 1);
    }

    [Fact]
    public void Property_Compliant_NoViolation()
    {
        var results = GetResults("class Test { public int Count { get; set; } }");
        Assert.DoesNotContain(results, r => r.RuleName == "Property Naming Convention");
    }

    [Fact]
    public void Enum_NotPascalCase_ReturnsWarningWithLine1()
    {
        var results = GetResults("enum badEnum { A }");
        Assert.Contains(results, r => r.RuleName == "Enum Naming Convention" && r.Severity == "Warning" && r.LineNumber == 1);
    }

    [Fact]
    public void Enum_Compliant_NoViolation()
    {
        var results = GetResults("enum Color { Red }");
        Assert.DoesNotContain(results, r => r.RuleName == "Enum Naming Convention");
    }

    [Fact]
    public void EnumMember_NotPascalCase_ReturnsWarningWithLine1()
    {
        var results = GetResults("enum Colors { red }");
        Assert.Contains(results, r => r.RuleName == "Enum Member Naming Convention" && r.Severity == "Warning" && r.LineNumber == 1);
    }

    [Fact]
    public void EnumMember_Compliant_NoViolation()
    {
        var results = GetResults("enum Colors { Red }");
        Assert.DoesNotContain(results, r => r.RuleName == "Enum Member Naming Convention");
    }

    [Fact]
    public void LocalVariable_NotCamelCase_ReturnsInfoWithLine1()
    {
        var results = GetResults("class Test { void Do() { int NotCamel = 0; } }");
        Assert.Contains(results, r => r.RuleName == "Local Variable Naming Convention" && r.Severity == "Info" && r.LineNumber == 1);
    }

    [Fact]
    public void LocalVariable_Compliant_NoViolation()
    {
        var results = GetResults("class Test { void Do() { int count = 0; } }");
        Assert.DoesNotContain(results, r => r.RuleName == "Local Variable Naming Convention");
    }

    [Fact]
    public void EventHandler_NotOnOrHandler_ReturnsInfoWithLine1()
    {
        var results = GetResults("using System; class Test { void Handle(int x, EventArgs e) {} }");
        Assert.Contains(results, r => r.RuleName == "Event Handler Naming Convention" && r.Severity == "Info" && r.LineNumber == 1);
    }

    [Fact]
    public void EventHandler_Compliant_NoViolation()
    {
        var results = GetResults("using System; class Test { void OnClicked(object sender, EventArgs e) {} }");
        Assert.DoesNotContain(results, r => r.RuleName == "Event Handler Naming Convention");
    }

    [Fact]
    public void DelegateType_NotHandlerOrCallback_ReturnsInfoWithLine1()
    {
        var results = GetResults("delegate void MyDelegate();");
        Assert.Contains(results, r => r.RuleName == "Delegate Type Naming Convention" && r.Severity == "Info" && r.LineNumber == 1);
    }

    [Fact]
    public void DelegateType_Compliant_NoViolation()
    {
        var results = GetResults("delegate void CompletedHandler();");
        Assert.DoesNotContain(results, r => r.RuleName == "Delegate Type Naming Convention");
    }

    [Fact]
    public void GenericTypeParam_NotSingleUppercase_ReturnsInfoWithLine1()
    {
        var results = GetResults("class Box<TItem> { }");
        Assert.Contains(results, r => r.RuleName == "Generic Type Parameter Naming Convention" && r.Severity == "Info" && r.LineNumber == 1);
    }

    [Fact]
    public void GenericTypeParam_Compliant_NoViolation()
    {
        var results = GetResults("class Box<T> { }");
        Assert.DoesNotContain(results, r => r.RuleName == "Generic Type Parameter Naming Convention");
    }

    [Fact]
    public void Review_MultipleViolationsInOneSource_ReportsAllWithCorrectLines()
    {
        var source = @"
            class badClass
            {
                private int fieldName;
                public const int Not_All_Caps = 1;
                public int prop_name { get; set; }
                void doThing(int NotCamel)
                {
                    int NotLocal = 0;
                }
            }
            interface Service { }
            enum badEnum { a, B }";

        var results = GetResults(source);

        // Assert
        Assert.Contains(results, r => r.RuleName == "Class Naming Convention" && r.LineNumber == 2);
        Assert.Contains(results, r => r.RuleName == "Field Naming Convention" && r.LineNumber == 4);
        Assert.Contains(results, r => r.RuleName == "Constant Naming Convention" && r.LineNumber == 5);
        Assert.Contains(results, r => r.RuleName == "Property Naming Convention" && r.LineNumber == 6);
        Assert.Contains(results, r => r.RuleName == "Method Naming Convention" && r.LineNumber == 7);
        Assert.Contains(results, r => r.RuleName == "Parameter Naming Convention" && r.LineNumber == 7);
        Assert.Contains(results, r => r.RuleName == "Local Variable Naming Convention" && r.LineNumber == 9);
        Assert.Contains(results, r => r.RuleName == "Interface Naming Convention" && r.LineNumber == 12);
        Assert.Contains(results, r => r.RuleName == "Enum Naming Convention" && r.LineNumber == 13);
        Assert.Contains(results, r => r.RuleName == "Enum Member Naming Convention" && r.LineNumber == 13);
    }

    [Fact]
    public void Review_EmptyOrMinimalCode_ReturnsNoViolations()
    {
        var empty = string.Empty;
        var minimal = "\n\n";
        Assert.Empty(GetResults(empty));
        Assert.Empty(GetResults(minimal));
    }
}


