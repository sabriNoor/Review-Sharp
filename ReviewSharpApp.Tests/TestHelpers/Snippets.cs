namespace ReviewSharpApp.Tests.TestHelpers;

public static class Snippets
{
    public static string WrapInClass(string body, string className = "Test") => $"class {className}\n{{\n{body}\n}}";
    public static string WrapInNamespace(string body, string ns = "NS") => $"namespace {ns}\n{{\n{body}\n}}";
}


