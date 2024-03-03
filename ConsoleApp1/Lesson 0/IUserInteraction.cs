using System;

 interface IUserInteraction
{
    public delegate void UserInputHandler(string input);
    public event UserInputHandler DataEntered;

    public void Display(string message);

    public void DisplayLine(string message);

    public string RequestInput();

    public string RequestInput(string message);
}
