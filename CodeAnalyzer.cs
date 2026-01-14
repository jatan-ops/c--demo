using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SimpleProject
{
  public class CodeAnalyzer
  {
    public void AnalyzeMethod(string filePath, string methodName)
    {
      // Use the absolute file path provided
      string fullFilePath = Path.GetFullPath(filePath);
      if (!File.Exists(fullFilePath))
      {
        Console.WriteLine($"File not found: {fullFilePath}");
        return;
      }
      
      // Get the directory containing the file
      string? directory = Path.GetDirectoryName(fullFilePath);
      if (string.IsNullOrEmpty(directory))
      {
        Console.WriteLine("Could not determine directory!");
        return;
      }
      
      // Read all C# files in the directory to include in compilation
      var csFiles = Directory.GetFiles(directory, "*.cs");
      var syntaxTrees = new List<SyntaxTree>();
      SyntaxTree? mainTree = null;
      
      foreach (var csFile in csFiles)
      {
        string sourceCode = File.ReadAllText(csFile);
        var tree = CSharpSyntaxTree.ParseText(sourceCode, path: csFile);
        syntaxTrees.Add(tree);
        
        // Find the main file's syntax tree
        if (Path.GetFullPath(csFile).Equals(fullFilePath, StringComparison.OrdinalIgnoreCase))
        {
          mainTree = tree;
        }
      }
      
      if (mainTree == null)
      {
        Console.WriteLine("Could not find the main file in the compilation!");
        return;
      }
      
      CompilationUnitSyntax root = mainTree.GetCompilationUnitRoot();
      
      // Create a compilation with references to enable semantic analysis
      var references = new MetadataReference[]
      {
        MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(Console).Assembly.Location)
      };
      
      var compilation = CSharpCompilation.Create("TempAssembly", syntaxTrees, references);
      var semanticModel = compilation.GetSemanticModel(mainTree);
      
      //! Get all types defined in the current compilation (user-defined classes)
      var userDefinedTypes = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);
      foreach (var tree in syntaxTrees)
      {
        var model = compilation.GetSemanticModel(tree);
        var treeRoot = tree.GetCompilationUnitRoot();
        foreach (var classDecl in treeRoot.DescendantNodes().OfType<ClassDeclarationSyntax>())
        {
          var classSymbol = model.GetDeclaredSymbol(classDecl);
          if (classSymbol != null)
          {
            userDefinedTypes.Add(classSymbol);
          }
        }
      }
      
      // Helper method to check if a symbol belongs to a user-defined type
      bool IsUserDefinedType(ISymbol? symbol)
      {
        if (symbol == null) return false;
        var containingType = symbol.ContainingType;
        if (containingType == null) return false;
        
        // Check if the containing type is in our user-defined types
        return userDefinedTypes.Contains(containingType);
      }
      
      // Find the method in the specified file
      MethodDeclarationSyntax? seedMethod = null;
      IMethodSymbol? seedMethodSymbol = null;
      
      // Search all classes in the main file for the specified method
      foreach (var classDecl in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
      {
        var methodDecl = classDecl.DescendantNodes()
          .OfType<MethodDeclarationSyntax>()
          .FirstOrDefault(m => m.Identifier.ValueText == methodName);
        
        if (methodDecl != null)
        {
          seedMethod = methodDecl;
          var symbol = semanticModel.GetDeclaredSymbol(methodDecl) as IMethodSymbol;
          if (symbol != null)
          {
            seedMethodSymbol = symbol;
            break;
          }
        }
      }

      if (seedMethod == null || seedMethodSymbol == null)
      {
        Console.WriteLine($"Method '{methodName}' not found in file: {fullFilePath}");
        return;
      }
      
      Console.WriteLine($"=== Recursive Analysis Starting from {seedMethodSymbol.ContainingType.Name}.{methodName} Method ===\n");
      
      // Initialize sets to track all symbols found and resolved
      var allFoundMethods = new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default);
      var allFoundProperties = new HashSet<IPropertySymbol>(SymbolEqualityComparer.Default);
      var resolvedMethods = new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default);
      
      // Add seed method to the found methods list
      allFoundMethods.Add(seedMethodSymbol);
      
      //! Recursively analyze methods until all are resolved
      while (true)
      {
        //! Find first unresolved method
        var unresolvedMethod = allFoundMethods.FirstOrDefault(m => !resolvedMethods.Contains(m));
        
        if (unresolvedMethod == null)
        {
          // All methods are resolved
          break;
        }
        
        //! Mark this method as resolved
        resolvedMethods.Add(unresolvedMethod);
        
        // Find the method declaration syntax for this symbol
        MethodDeclarationSyntax? methodSyntax = null;
        SemanticModel? methodSemanticModel = null;
        
        foreach (var tree in syntaxTrees)
        {
          var model = compilation.GetSemanticModel(tree);
          var treeRoot = tree.GetCompilationUnitRoot();
          
          foreach (var methodDecl in treeRoot.DescendantNodes().OfType<MethodDeclarationSyntax>())
          {
            var methodSymbol = model.GetDeclaredSymbol(methodDecl) as IMethodSymbol;
            if (methodSymbol != null && SymbolEqualityComparer.Default.Equals(methodSymbol, unresolvedMethod))
            {
              methodSyntax = methodDecl;
              methodSemanticModel = model;
              break;
            }
          }
          
          if (methodSyntax != null) break;
        }
        
        if (methodSyntax == null || methodSemanticModel == null)
        {
          // Could not find method declaration, skip it
          continue;
        }
        
        // Analyze this method to find properties and methods it accesses/calls
        var memberAccesses = methodSyntax.DescendantNodes().OfType<MemberAccessExpressionSyntax>();
        var invocations = methodSyntax.DescendantNodes().OfType<InvocationExpressionSyntax>();
        
        //! Process property accesses
        foreach (var memberAccess in memberAccesses)
        {
          // Check if it's a property access (not followed by parentheses)
          if (!memberAccess.Parent.IsKind(SyntaxKind.InvocationExpression))
          {
            var symbolInfo = methodSemanticModel.GetSymbolInfo(memberAccess);
            if (symbolInfo.Symbol is IPropertySymbol propertySymbol)
            {
              // Only add if the property type is string AND it belongs to a user-defined type
              if (propertySymbol.Type.SpecialType == SpecialType.System_String && 
                  IsUserDefinedType(propertySymbol))
              {
                allFoundProperties.Add(propertySymbol);
              }
            }
          }
        }
        
        //! Process method invocations
        foreach (var invocation in invocations)
        {
          if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
          {
            var symbolInfo = methodSemanticModel.GetSymbolInfo(memberAccess);
            if (symbolInfo.Symbol is IMethodSymbol methodSymbol && IsUserDefinedType(methodSymbol))
            {
              allFoundMethods.Add(methodSymbol);
            }
          }
          else if (invocation.Expression is IdentifierNameSyntax identifier)
          {
            var symbolInfo = methodSemanticModel.GetSymbolInfo(identifier);
            if (symbolInfo.Symbol is IMethodSymbol methodSymbol && IsUserDefinedType(methodSymbol))
            {
              allFoundMethods.Add(methodSymbol);
            }
          }
        }
      }
      
      // Display results
      Console.WriteLine("Properties Accessed:");
      if (allFoundProperties.Count > 0)
      {
        foreach (var prop in allFoundProperties)
        {
          string displayName = $"{prop.ContainingType.Name}.{prop.Name}";
          Console.WriteLine($"  - {displayName}");
        }
      }
      else
      {
        Console.WriteLine("  (none)");
      }
      
      Console.WriteLine("\nMethods Called:");
      if (allFoundMethods.Count > 0)
      {
        foreach (var method in allFoundMethods)
        {
          string displayName = $"{method.ContainingType.Name}.{method.Name}()";
          Console.WriteLine($"  - {displayName}");
        }
      }
      else
      {
        Console.WriteLine("  (none)");
      }
      
      // Create JSON output
      var outputData = new
      {
        seedFile = fullFilePath,
        seedMethod = methodName,
        properties = allFoundProperties.Select(p => new
        {
          name = p.Name,
          containingType = p.ContainingType.Name,
          fullName = $"{p.ContainingType.Name}.{p.Name}",
          type = p.Type.ToString(),
          location = p.Locations.FirstOrDefault()?.ToString() ?? "unknown"
        }).OrderBy(p => p.fullName).ToList(),
        methods = allFoundMethods.Select(m => new
        {
          name = m.Name,
          containingType = m.ContainingType.Name,
          fullName = $"{m.ContainingType.Name}.{m.Name}()",
          returnType = m.ReturnType.ToString(),
          parameters = m.Parameters.Select(p => new
          {
            name = p.Name,
            type = p.Type.ToString()
          }).ToList(),
          location = m.Locations.FirstOrDefault()?.ToString() ?? "unknown"
        }).OrderBy(m => m.fullName).ToList()
      };
      
      // Serialize to JSON
      var jsonOptions = new JsonSerializerOptions
      {
        WriteIndented = true
      };
      string json = JsonSerializer.Serialize(outputData, jsonOptions);
      
      // Write to file
      string outputFileName = $"analysis_{Path.GetFileNameWithoutExtension(fullFilePath)}_{methodName}.json";
      string outputPath = Path.Combine(directory, outputFileName);
      File.WriteAllText(outputPath, json);
      
      Console.WriteLine($"\nAnalysis results saved to: {outputPath}");
    }
  }
}
