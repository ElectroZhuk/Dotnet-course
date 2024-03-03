using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

class UserConsoleInteraction : IUserInteraction
{
    public event IUserInteraction.UserInputHandler DataEntered;

    public void Display(string message)
    {
        Console.Write(message);
    }

    public void DisplayLine(string message)
    {
        Console.WriteLine(message);
    }

    public string RequestInput()
    {
        string input = Console.ReadLine();
        DataEntered?.Invoke(input);

        return input;
    }

    public string RequestInput(string message)
    {
        Console.Write(message);
        return RequestInput();
    }
}
