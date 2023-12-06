using System;

[MatchXL]
public class Tank : INameable
{
    [MatchXLColumn("Id")] public int Id { get; private set; }
    [MatchXLColumn("Name")] public string Name { get; private set; }
    [MatchXLColumn("Description")] public string Description { get; private set; }
    [MatchXLColumn("Volume")] public int Volume { get; private set; }
    [MatchXLColumn("MaxVolume")] public int MaxVolume { get; private set; }
    [MatchXLColumn("UnitId")] public int UnitId { get; private set; }

    [MatchXLConstructor]
    public Tank([MatchXLColumn("Id")] int id, string name, string description, int volume, int maxVolume, int unitId)
    {
        if (id < 1)
            throw new ArgumentException("ID cannot be less then 1");

        Id = id;
        Name = name;
        Description = description;

        if (maxVolume < 0)
            throw new ArgumentException("Maximum volume cannot be less then 0");

        MaxVolume = maxVolume;

        if (volume < 0)
            throw new ArgumentException("Volume cannot be less then 0");

        if (volume > maxVolume)
            throw new ArgumentException("Volume cannot be greater then maximum volume");

        Volume = volume;

        if (unitId < 1)
            throw new ArgumentException("Unit ID cannot be less then 1");

        UnitId = unitId;
    }
}