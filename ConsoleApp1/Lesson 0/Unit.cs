using System;

public class Unit : INameable
{
    public int Id { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public int FactoryId { get; private set; }

    public Unit(int id, string name, string description, int factoryId)
    {
        if (id < 1)
            throw new ArgumentException("ID cannot be less then 1");

        Id = id;
        Name = name;
        Description = description;

        if (factoryId < 1)
            throw new ArgumentException("Factory ID cannot be less then 1");

        FactoryId = factoryId;
    }

    public static bool ValidateID(int id)
    {
        if (id < 1)
            throw new ArgumentOutOfRangeException("Unit ID cannot be less then 1.");

        return true;
    }
}
