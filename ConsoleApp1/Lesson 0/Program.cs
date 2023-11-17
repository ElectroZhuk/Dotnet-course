using System.Text.Json;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using ClosedXML.Excel;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;

class ProgramZero
{
    public static void Start()
    {
        var workbook = new XLWorkbook("workBook.xlsx");
        var tanks = GetTanks(workbook);
        var units = GetUnits(workbook);
        var factories = GetFactories(workbook);
        Console.WriteLine($"Количество резервуаров: {tanks.Length}, установок: {units.Length}");
        
        string tankName = "Резервуар 20";
        Unit foundUnit;
        FindUnit(units, tanks, tankName, out foundUnit);

        if (foundUnit is null)
        {
            Console.WriteLine($"{tankName} не найден");
        }
        else
        {
            var factory = FindFactory(factories, foundUnit);
            Console.WriteLine($"Резервуар 2 принадлежит установке {foundUnit.Name} и заводу {factory.Name}");
        }

        var totalVolume = GetTotalMaxVolume(tanks);
        Console.WriteLine($"Общий объем резервуаров: {totalVolume}");

        PrintTanksFullInfo(tanks, units, factories);

        int totalCurrentVolume = GetTotalCurrentVolume(tanks);
        Console.WriteLine($"Текущий объем жидкости во всех резервуарах: {totalCurrentVolume}");

        Console.Write("Введите название объекта для поиска: ");
        string nameToFind = Console.ReadLine();

        if (nameToFind is not null)
        {
            INameable nameable;

            FindByName(factories, nameToFind, out nameable);
            if (nameable is Factory factory)
                Console.WriteLine($"Завод {factory.Name}, {factory.Description}");

            FindByName(units, nameToFind, out nameable);
            if (nameable is Unit unit)
                Console.WriteLine($"Установка {unit.Name}, {unit.Description} находится на заводе {FindFactory(factories, unit).Name}");

            FindByName(tanks, nameToFind, out nameable);
            if (nameable is Tank tank)
                Console.WriteLine($"Резервуар {tank.Name}, {tank.Description} (объем {tank.Volume}/{tank.MaxVolume}) в составе установки {FindUnit(units, tank).Name}");
            
            if (nameable is null)
                Console.WriteLine("Не найдено объектов с указанным названием");
        }
        else
        {
            Console.WriteLine("Вы не ввели название объекта");
        }

        Dictionary<string, object> savingData = new Dictionary<string, object>() {
            { "factories", factories },
            { "units", units },
            { "tanks", tanks }
        };
        SaveToJsonFile(savingData, "fullData.json");
    }

    public static void SaveToJsonFile(object savingData, string fileName)
    {
        using (FileStream stream = new FileStream(fileName, FileMode.Create))
        {
            JsonSerializer.Serialize(stream, savingData, new JsonSerializerOptions()
            {
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            });
            Console.WriteLine($"Data has been saved to file: {stream.Name}");
        }
    }

    public static int GetCellValueInt(IXLCell cell)
    {
        var cellValue = cell.Value;

        if (!cellValue.IsNumber)
            throw new FormatException($"Cell {cell.WorksheetColumn().ColumnLetter() + cell.WorksheetRow().RowNumber().ToString()} has unexpected type");

        return (int)cell.Value.GetNumber();
    }

    public static string GetCellValueString(IXLCell cell)
    {
        var cellValue = cell.Value;
        string textValue;

        if (!cellValue.TryGetText(out textValue))
            throw new FormatException($"Cell {cell.WorksheetColumn().ColumnLetter() + cell.WorksheetRow().RowNumber().ToString()} has unexpected type");

        return textValue;
    }

    public static IXLWorksheet GetWorksheet(XLWorkbook workbook, int worksheetPosition)
    {
        if (worksheetPosition < 1)
            throw new ArgumentException("Worksheet position can't be less then 1");

        if (workbook.Worksheets.Count < worksheetPosition)
            throw new FormatException($"Excel document has {workbook.Worksheets.Count} worksheets, tried to open {worksheetPosition}");

        return workbook.Worksheet(worksheetPosition);
    }

    public static IXLCell GetFirstUsedCell(IXLWorksheet worksheet)
    {
        var firstCell = worksheet.FirstCellUsed();

        if (firstCell is null)
            throw new FormatException($"Worksheet {worksheet.Position} is empty");
        else if (firstCell.GetText() != "Id")
            throw new FormatException("Unexpected value of the first used cell");

        return firstCell;
    }

    public static Tank[] GetTanks()
    {
        Tank[] tanks = {
            new Tank(1, "Резервуар 1", "Надземный - вертикальный", 1500, 2000, 1),
            new Tank(2, "Резервуар 2", "Надземный - горизонтальный", 2500, 3000, 1),
            new Tank(3, "Дополнительный резервуар 24", "Надземный - горизонтальный", 3000, 3000, 2),
            new Tank(4, "Резервуар 35", "Надземный - вертикальный", 3000, 3000, 2),
            new Tank(5, "Резервуар 47", "Подземный - двустенный", 4000, 5000, 2),
            new Tank(6, "Резервуар 256", "Подводный", 500, 500, 3)
        };

        return tanks;
    }

    public static Tank[] GetTanks(XLWorkbook workbook)
    {
        var worksheet = GetWorksheet(workbook, 3);
        var firstCell = GetFirstUsedCell(worksheet);
        firstCell = firstCell.CellBelow();
        int firstColumnNumber = firstCell.WorksheetColumn().ColumnNumber();
        int firstRowNumber = firstCell.WorksheetRow().RowNumber();
        int rowsRangeCount = firstCell.WorksheetColumn().LastCellUsed().WorksheetRow().RowNumber() - firstRowNumber + 1;
        Tank[] tanks = new Tank[rowsRangeCount];

        for (int i = 0; i < rowsRangeCount; i++)
        {
            int currentRowNumber = firstRowNumber + i;
            var targetCell = worksheet.Cell(currentRowNumber, firstColumnNumber);
            
            int id = GetCellValueInt(targetCell);
            string name = GetCellValueString(targetCell.CellRight(1));
            string description = GetCellValueString(targetCell.CellRight(2));
            int volume = GetCellValueInt(targetCell.CellRight(3));
            int maxVolume = GetCellValueInt(targetCell.CellRight(4));
            int unitID = GetCellValueInt(targetCell.CellRight(5));
            tanks.SetValue(new Tank(id, name, description, volume, maxVolume, unitID), i);
        }

        return tanks;
    }

    public static Unit[] GetUnits()
    {
        Unit[] units = {
            new Unit(1, "ГФУ-2", "Газофракционирующая установка", 1),
            new Unit(2, "АВТ-6", "Атмосферно-вакуумная трубчатка", 1),
            new Unit(3, "АВТ-10", "Атмосферно-вакуумная трубчатка", 2)
        };

        return units;
    }

    public static Unit[] GetUnits(XLWorkbook workbook)
    {
        var worksheet = GetWorksheet(workbook, 2);
        var firstCell = GetFirstUsedCell(worksheet);
        firstCell = firstCell.CellBelow();
        int firstColumnNumber = firstCell.WorksheetColumn().ColumnNumber();
        int firstRowNumber = firstCell.WorksheetRow().RowNumber();
        int rowsRangeCount = firstCell.WorksheetColumn().LastCellUsed().WorksheetRow().RowNumber() - firstRowNumber + 1;
        Unit[] units = new Unit[rowsRangeCount];

        for (int i = 0; i < rowsRangeCount; i++)
        {
            int currentRowNumber = firstRowNumber + i;
            var targetCell = worksheet.Cell(currentRowNumber, firstColumnNumber);

            int id = GetCellValueInt(targetCell);
            string name = GetCellValueString(targetCell.CellRight(1));
            string description = GetCellValueString(targetCell.CellRight(2));
            int factoryID = GetCellValueInt(targetCell.CellRight(3));
            units.SetValue(new Unit(id, name, description, factoryID), i);
        }

        return units;
    }

    public static Factory[] GetFactories()
    {
        Factory[] factories = {
            new Factory(1, "НПЗ№1", "Первый нефтеперерабатывающий завод"),
            new Factory(2, "НПЗ№2", "Второй нефтеперерабатывающий завод")
        };

        return factories;
    }

    public static Factory[] GetFactories(XLWorkbook workbook)
    {
        var worksheet = GetWorksheet(workbook, 1);
        var firstCell = GetFirstUsedCell(worksheet);
        firstCell = firstCell.CellBelow();
        int firstColumnNumber = firstCell.WorksheetColumn().ColumnNumber();
        int firstRowNumber = firstCell.WorksheetRow().RowNumber();
        int rowsRangeCount = firstCell.WorksheetColumn().LastCellUsed().WorksheetRow().RowNumber() - firstRowNumber + 1;
        Factory[] factories = new Factory[rowsRangeCount];

        for (int i = 0; i < rowsRangeCount; i++)
        {
            int currentRowNumber = firstRowNumber + i;
            var targetCell = worksheet.Cell(currentRowNumber, firstColumnNumber);

            int id = GetCellValueInt(targetCell);
            string name = GetCellValueString(targetCell.CellRight(1));
            string description = GetCellValueString(targetCell.CellRight(2));
            factories.SetValue(new Factory(id, name, description), i);
        }

        return factories;
    }



    public static void PrintTanksFullInfo(Tank[] tanks, Unit[] units, Factory[] factories)
    {
        foreach (Tank tank in tanks)
        {
            Unit unit = units.Where(localUnit => localUnit.Id == tank.UnitId).Single();
            Factory factory = factories.Where(localFactory => localFactory.Id == unit.FactoryId).Single();

            Console.WriteLine($"{tank.Name}, {tank.Description}: объем - {tank.Volume}/{tank.MaxVolume}; установка - {unit.Name}; завод - {factory.Name}");
        }
    }

    public static void FindByName(INameable[] nameables, string name, out INameable foundNameable)
    {
        foundNameable = nameables.Where(nameable => nameable.Name == name).SingleOrDefault();
    }

    // реализуйте этот метод, чтобы он возвращал установку (Unit), которой
    // принадлежит резервуар (Tank), найденный в массиве резервуаров по имени
    // учтите, что по заданному имени может быть не найден резервуар
    public static void FindUnit(Unit[] units, Tank[] tanks, string tankName, out Unit foundUnit)
    {
        foundUnit = units.Where(unit => unit.Id == tanks.Where(tank => tank.Name == tankName).SingleOrDefault()?.UnitId).SingleOrDefault();
    }

    public static Unit FindUnit(Unit[] units, Tank tank)
    {
        return units.Where(unit => unit.Id == tank.UnitId).Single();
    }

    // реализуйте этот метод, чтобы он возвращал объект завода, соответствующий установке
    public static Factory FindFactory(Factory[] factories, Unit unit)
    {
        return factories.Where(factory => factory.Id == unit.FactoryId).Single();
    }

    public static int GetTotalCurrentVolume(Tank[] tanks)
    {
        return tanks.Sum(tank => tank.Volume);
    }

    // реализуйте этот метод, чтобы он возвращал суммарный объем резервуаров в массиве
    public static int GetTotalMaxVolume(Tank[] tanks)
    {
        return tanks.Sum(tank => tank.MaxVolume);
    }
}

public interface INameable
{
    public string Name { get; }
}

/// <summary>
/// Установка
/// </summary>
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
}

/// <summary>
/// Завод
/// </summary>
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

/// <summary>
/// Резервуар
/// </summary>
public class Tank : INameable
{
    public int Id { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public int Volume { get; private set; }
    public int MaxVolume { get; private set; }
    public int UnitId { get; private set; }

    public Tank(int id, string name, string description, int volume, int maxVolume, int unitId)
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