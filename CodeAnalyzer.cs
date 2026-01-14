using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SimpleProject
{
  public class CodeAnalyzer
  {
    public void AnalyzeDoSomethingMethod(string filePath)
    {
      // Read the source file
      string sourceCode = File.ReadAllText(filePath);
      
      // Parse the code into a syntax tree
      SyntaxTree tree = CSharpSyntaxTree.ParseText(sourceCode);
      CompilationUnitSyntax root = tree.GetCompilationUnitRoot();
      
      // Find the FirstClass
      var firstClass = root.DescendantNodes()
        .OfType<ClassDeclarationSyntax>()
        .FirstOrDefault(c => c.Identifier.ValueText == "FirstClass");
      // find first class from syntax tree

      if (firstClass == null)
      {
        Console.WriteLine("FirstClass not found!");
        return;
      }
      
      // Find the DoSomething method
      var doSomethingMethod = firstClass.DescendantNodes()
        .OfType<MethodDeclarationSyntax>()
        .FirstOrDefault(m => m.Identifier.ValueText == "DoSomething");
      // find do something method from first class

      if (doSomethingMethod == null)
      {
        Console.WriteLine("DoSomething method not found!");
        return;
      }
      
      Console.WriteLine("=== Analysis of DoSomething Method ===\n");
      
      // Collect properties and methods
      var properties = new List<string>();
      var methods = new List<string>();
      
      // Analyze all member access expressions and invocations in do something method
      var memberAccesses = doSomethingMethod.DescendantNodes()
        .OfType<MemberAccessExpressionSyntax>();
      
      var invocations = doSomethingMethod.DescendantNodes()
        .OfType<InvocationExpressionSyntax>();
      
      foreach (var memberAccess in memberAccesses)
      {
        string memberName = memberAccess.Name.Identifier.ValueText;
        string expression = memberAccess.Expression.ToString();
        
        // Check if it's a property access (not followed by parentheses)
        if (!memberAccess.Parent.IsKind(SyntaxKind.InvocationExpression))
        {
          properties.Add($"{expression}.{memberName}");
        }
      }
      
      foreach (var invocation in invocations)
      {
        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
        {
          string methodName = memberAccess.Name.Identifier.ValueText;
          string expression = memberAccess.Expression.ToString();
          methods.Add($"{expression}.{methodName}()");
        }
        else if (invocation.Expression is IdentifierNameSyntax identifier)
        {
          methods.Add($"{identifier.Identifier.ValueText}()");
        }
      }
      
      // Display results
      Console.WriteLine("Properties Accessed:");
      if (properties.Count > 0)
      {
        foreach (var prop in properties.Distinct())
        {
          Console.WriteLine($"  - {prop}");
        }
      }
      else
      {
        Console.WriteLine("  (none)");
      }
      
      Console.WriteLine("\nMethods Called:");
      if (methods.Count > 0)
      {
        foreach (var method in methods.Distinct())
        {
          Console.WriteLine($"  - {method}");
        }
      }
      else
      {
        Console.WriteLine("  (none)");
      }
    }
  }
}
