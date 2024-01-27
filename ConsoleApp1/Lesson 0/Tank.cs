using System;
using ClosedXML.Excel;

public class Tank : INameable
{
    public int Id { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public int Volume { get; private set; }
    public int MaxVolume { get; private set; }
    public int UnitId { get; private set; }

    public static int XLWorksheetNumber = 3;

    public Tank(int id, string name, string description, int volume, int maxVolume, int unitId)
    {
        ValidateID(id);
        Id = id;
        Name = name;
        Description = description;

        if (maxVolume < 0)
            throw new ArgumentOutOfRangeException("Maximum volume cannot be less then 0.");

        MaxVolume = maxVolume;

        if (volume < 0)
            throw new ArgumentOutOfRangeException("Volume cannot be less then 0.");

        if (volume > maxVolume)
            throw new ArgumentOutOfRangeException("Volume cannot be greater then maximum volume.");

        Volume = volume;

        Unit.ValidateID(unitId);
        UnitId = unitId;
    }

    [MatchXLConstructor(XLConstructorArgumentsNamesFormat.ToTitleCase)]
    public Tank(XLCellValue id, XLCellValue name, XLCellValue description, XLCellValue volume, XLCellValue maxVolume, XLCellValue unitId)
        : this(XLCellValueGetter.GetInt(id), XLCellValueGetter.GetString(name), XLCellValueGetter.GetString(description),
        XLCellValueGetter.GetInt(volume), XLCellValueGetter.GetInt(maxVolume), XLCellValueGetter.GetInt(unitId))
    {
        
    }

    public static bool ValidateID(int id)
    {
        if (id < 1)
            throw new ArgumentOutOfRangeException("Tank ID cannot be less then 1.");

        return true;
    }
}