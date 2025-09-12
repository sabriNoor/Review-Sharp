using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ReviewSharp.Models;

namespace ReviewSharp.Interfaces
{
    public interface ICodeReviewSemanticService 
    {
        List<CodeReviewResult> Review(CompilationUnitSyntax root, SemanticModel semanticModel);
    }
}