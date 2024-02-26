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

        IEnumerable<Tank> tanks = new List<Tank>();
        try
        {
            tanks = GetFromXL<Tank>(workbook.Worksheet("Tanks"));
        }
        catch (XLFormatException e)
        {
            Console.WriteLine($"Something went wrong while parsing tanks from excel: {e.Message}");
            return;
        }

        IEnumerable<Unit> units = new List<Unit>();
        try
        {
            units = GetFromXL<Unit>(workbook.Worksheet("Units"));
        }
        catch (XLFormatException e)
        {
            Console.WriteLine($"Something went wrong while parsing units from excel: {e.Message}");
            return;
        }

        IEnumerable<Factory> factories = new List<Factory>();
        try
        {
            factories = GetFromXL<Factory>(workbook.Worksheet("Factories"));
        }
        catch (XLFormatException e)
        {
            Console.WriteLine($"Something went wrong while parsing factories from excel: {e.Message}");
            return;
        }

        Console.WriteLine($"Количество резервуаров: {tanks.Count()}, установок: {units.Count()}");
        
        string tankName = "Резервуар 20";
        Unit foundUnit;
        FindUnit(units, tanks, tankName, out foundUnit);

        if (foundUnit is null)
        {
            Console.WriteLine($"{tankName} не найден");
        }
        else
        {
            try
            {
                var factory = FindFactory(factories, foundUnit);
                Console.WriteLine($"Резервуар 2 принадлежит установке {foundUnit.Name} и заводу {factory.Name}.");
            }
            catch (ArgumentNullException)
            {
                Console.WriteLine($"Ошибка. Установка с ID={foundUnit.Id} не привязана ни к одному заводу.");
                return;
            }
            catch (InvalidOperationException)
            {
                Console.WriteLine($"Ошибка. Установка с ID={foundUnit.Id} привязана к нескольким заводам.");
                return;
            }
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
            IEnumerable<INameable> allObjects = factories.Cast<INameable>().Concat(units.Cast<INameable>()).Concat(tanks.Cast<INameable>());
            FindByName(allObjects, nameToFind, out nameable);

            if (nameable is Factory factory)
                Console.WriteLine($"Завод {factory.Name}, {factory.Description}");
            else if (nameable is Unit unit)
                Console.WriteLine($"Установка {unit.Name}, {unit.Description} находится на заводе {FindFactory(factories, unit).Name}");
            else if (nameable is Tank tank)
                Console.WriteLine($"Резервуар {tank.Name}, {tank.Description} (объем {tank.Volume}/{tank.MaxVolume}) в составе установки {FindUnit(units, tank).Name}");
            else if (nameable is null)
                Console.WriteLine("Не найдено объектов с указанным названием");
            else
            {
                Console.WriteLine("По названию получен неожиданный тип объекта.");
                return;
            }
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

    public static IEnumerable<T> GetFromXL<T>(IXLWorksheet worksheet)
    {
        var firstCell = worksheet.FirstCellUsed();

        if (firstCell is null)
            throw new XLFormatException($"Worksheet \"{worksheet.Name}\" is empty");

        var firstColumnNumber = firstCell.WorksheetColumn().ColumnNumber();
        var firstRowNumber = firstCell.WorksheetRow().RowNumber();
        var firstRecordRowNumber = firstRowNumber + 1;
        var recordsAmount = firstCell.WorksheetColumn().LastCellUsed().WorksheetRow().RowNumber() - firstRowNumber;
        var tableHeaderCells = worksheet.Cells().Where(cell => cell.Address.RowNumber == firstRowNumber &&
            cell.Address.ColumnNumber >= firstColumnNumber &&
            cell.Address.ColumnNumber <= worksheet.Row(firstRowNumber).LastCellUsed().Address.ColumnNumber);
        IEnumerable<string> tableHeaderCellsValues = new List<string>();

        if (!tableHeaderCells.All(cell => cell.Value.IsText))
            throw new XLFormatException($"Table header values {tableHeaderCells.First().Address.ToString(XLReferenceStyle.Default, false)}:{tableHeaderCells.Last().Address.ToString(XLReferenceStyle.Default, false)} on worksheet \"{worksheet.Name}\" has unexpected type.");

        tableHeaderCellsValues = tableHeaderCells.Select(cell => cell.Value.GetText());

        TableObjectsCreator<T> configuredCreator;

        try
        {
            if (!TableObjectsCreator<T>.TryMatch(tableHeaderCellsValues, false, false, out configuredCreator))
                throw new XLFormatException($"Worksheet \"{worksheet.Name}\" format doesn't matches {typeof(T).Name}.");
        }
        catch (ArgumentException exception)
        {
            throw new XLFormatException($"Table header on worksheet \"{worksheet.Name}\" has wrong format.", exception);
        }

        T[] objects = new T[recordsAmount];

        for (int i = 0; i < recordsAmount; i++)
        {
            int currentRowNumber = firstRecordRowNumber + i;
            var rowFirstCell = worksheet.Cell(currentRowNumber, firstColumnNumber);
            int parametersAmount = tableHeaderCellsValues.Count();
            object[] parameters = new object[parametersAmount];

            for (var rowCellIndex = 0; rowCellIndex < parametersAmount; rowCellIndex++)
            {
                var checkingCell = rowFirstCell.CellRight(rowCellIndex);
                var targetType = configuredCreator.GetParameterType(rowCellIndex);

                if (targetType == typeof(int))
                {
                    int cellIntValue;
                    
                    if (!checkingCell.TryGetValue<int>(out cellIntValue))
                            throw new XLFormatException($"Cell {checkingCell.Address.ToString(XLReferenceStyle.Default, true)} value is not an integer.");

                    parameters[rowCellIndex] = cellIntValue;
                }
                else if (targetType == typeof(string))
                {
                    string cellStringValue;

                    if (!checkingCell.TryGetValue<string>(out cellStringValue))
                        throw new XLFormatException($"Cell {checkingCell.Address.ToString(XLReferenceStyle.Default, true)} value is not a text.");

                    parameters[rowCellIndex] = cellStringValue;
                }
                else
                {
                    throw new XLFormatException($"Cell {checkingCell.Address.ToString(XLReferenceStyle.Default, true)} has unexpected type. Expected cast to {targetType.Name}.");
                }
            }

            objects[i] = configuredCreator.Create(parameters);
        }

        return objects;
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

    public static Factory[] GetFactories()
    {
        Factory[] factories = {
            new Factory(1, "НПЗ№1", "Первый нефтеперерабатывающий завод"),
            new Factory(2, "НПЗ№2", "Второй нефтеперерабатывающий завод")
        };

        return factories;
    }

    public static void PrintTanksFullInfo(IEnumerable<Tank> tanks, IEnumerable<Unit> units, IEnumerable<Factory> factories)
    {
        foreach (Tank tank in tanks)
        {
            Unit unit = units.Where(localUnit => localUnit.Id == tank.UnitId).Single();
            Factory factory = factories.Where(localFactory => localFactory.Id == unit.FactoryId).Single();

            Console.WriteLine($"{tank.Name}, {tank.Description}: объем - {tank.Volume}/{tank.MaxVolume}; установка - {unit.Name}; завод - {factory.Name}");
        }
    }

    public static void FindByName(IEnumerable<INameable> nameables, string name, out INameable foundNameable)
    {
        foundNameable = nameables.Where(nameable => nameable.Name == name).SingleOrDefault();
    }

    // реализуйте этот метод, чтобы он возвращал установку (Unit), которой
    // принадлежит резервуар (Tank), найденный в массиве резервуаров по имени
    // учтите, что по заданному имени может быть не найден резервуар
    public static void FindUnit(IEnumerable<Unit> units, IEnumerable<Tank> tanks, string tankName, out Unit foundUnit)
    {
        foundUnit = units.Where(unit => unit.Id == tanks.Where(tank => tank.Name == tankName).SingleOrDefault()?.UnitId).SingleOrDefault();
    }

    public static Unit FindUnit(IEnumerable<Unit> units, Tank tank)
    {
        return units.Where(unit => unit.Id == tank.UnitId).Single();
    }

    // реализуйте этот метод, чтобы он возвращал объект завода, соответствующий установке
    public static Factory FindFactory(IEnumerable<Factory> factories, Unit unit)
    {
        return factories.Where(factory => factory.Id == unit.FactoryId).Single();
    }

    public static int GetTotalCurrentVolume(IEnumerable<Tank> tanks)
    {
        return tanks.Sum(tank => tank.Volume);
    }

    // реализуйте этот метод, чтобы он возвращал суммарный объем резервуаров в массиве
    public static int GetTotalMaxVolume(IEnumerable<Tank> tanks)
    {
        return tanks.Sum(tank => tank.MaxVolume);
    }
}
