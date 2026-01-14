using System;

namespace SimpleProject
{
  public class SecondClass
  {
    // Property that can be accessed from FirstClass
    public string Name { get; set; } = "SecondClass Instance";
    
    public string GetMessage()
    {
      Console.WriteLine("SecondClass: GetMessage() method called!");
      
      // Create an instance of ThirdClass
      ThirdClass third = new ThirdClass();
      
      // Call a method from ThirdClass
      string processedData = third.ProcessData();
      
      Console.WriteLine($"SecondClass: Received from ThirdClass - {processedData}");
      
      return "Hello from SecondClass!";
    }
  }
}
