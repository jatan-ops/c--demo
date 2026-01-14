using System;

namespace SimpleProject
{
  class Program
  {
    static void Main(string[] args)
    {
      // Check command line arguments
      if (args.Length < 2)
      {
        Console.WriteLine("Usage: SimpleProject.exe <filePath> <methodName>");
        Console.WriteLine("Example: SimpleProject.exe \"C:\\coding\\c# demo\\FirstClass.cs\" DoSomething");
        return;
      }
      
      string filePath = args[0];
      string methodName = args[1];
      
      Console.WriteLine($"Starting analysis...");
      Console.WriteLine($"File: {filePath}");
      Console.WriteLine($"Method: {methodName}\n");
      
      // Analyze the method using Roslyn
      CodeAnalyzer analyzer = new CodeAnalyzer();
      analyzer.AnalyzeMethod(filePath, methodName);
      
      Console.WriteLine("\nAnalysis completed.");
    }
  }
}
