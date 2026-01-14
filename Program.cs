using System;

namespace SimpleProject
{
  class Program
  {
    static void Main(string[] args)
    {
      Console.WriteLine("Starting the application...\n");
      
      // Create an instance of FirstClass
      FirstClass first = new FirstClass();
      
      // Call the method that uses SecondClass
      first.DoSomething();
      
      Console.WriteLine("\n" + new string('=', 50) + "\n");
      
      // Analyze the DoSomething method using Roslyn
      CodeAnalyzer analyzer = new CodeAnalyzer();
      analyzer.AnalyzeDoSomethingMethod("FirstClass.cs");
      
      Console.WriteLine("\nApplication completed.");
    }
  }
}
