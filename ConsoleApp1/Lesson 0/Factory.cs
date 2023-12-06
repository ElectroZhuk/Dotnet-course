using System;

public class Factory : INameable
{
    public int Id { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }

    public Factory(int id, string name, string description)
    {
        if (id < 1)
            throw new ArgumentException("ID cannot be less then 1");

        Id = id;
        Name = name;
        Description = description;
    }
}