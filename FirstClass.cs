using System;

namespace SimpleProject
{
  public class FirstClass
  {
    public void DoSomething()
    {
      Console.WriteLine("FirstClass: Starting to do something...");
      
      // Create an instance of SecondClass
      SecondClass second = new SecondClass();
      
      // Access a property from SecondClass
      Console.WriteLine($"FirstClass: Accessing property from SecondClass - Name: {second.Name}");
      
      // Access integer property from SecondClass
      Console.WriteLine($"FirstClass: Accessing integer property from SecondClass - Id: {second.Id}");
      
      // Call a method from SecondClass
      string result = second.GetMessage();
      
      Console.WriteLine($"FirstClass: Received from SecondClass - {result}");
    }
  }
}
