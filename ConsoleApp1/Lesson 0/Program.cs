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
        IEnumerable<Unit> units = new List<Unit>();
        IEnumerable<Factory> factories = new List<Factory>();
        try
        {
            tanks = GetFromXL<Tank>(workbook.Worksheet("Tanks"));
            units = GetFromXL<Unit>(workbook.Worksheet("Units"));
            factories = GetFromXL<Factory>(workbook.Worksheet("Factories"));
        }
        catch (XLFormatException e)
        {
            Console.WriteLine($"Something went wrong while parsing from excel: {e.Message}");
            return;
        }

        Console.WriteLine($"Количество резервуаров: {tanks.Count()}, установок: {units.Count()}");
        
        string tankName = "Резервуар 20";
        Unit foundUnit;

        try
        {
            foundUnit = FindUnit(units, tanks, tankName);

            try
            {
                var factory = FindFactory(factories, foundUnit);
                Console.WriteLine($"Резервуар 2 принадлежит установке {foundUnit.Name} и заводу {factory.Name}.");
            }
            catch (Exception e) when (e is ArgumentNullException || e is InvalidOperationException)
            {
                Console.WriteLine($"Error when searching for the factory: {e.Message}.");
            }
        }
        catch (Exception e) when (e is ArgumentException || e is InvalidOperationException)
        {
            Console.WriteLine($"Error when searching for the unit: {e.Message}.");
        }

        try
        {
            var totalVolume = GetTotalMaxVolume(tanks);
            Console.WriteLine($"Общий объем резервуаров: {totalVolume}.");
        }
        catch (InvalidOperationException e)
        {
            Console.WriteLine($"Error while getting total max tanks capacity: {e.Message}.");
        }

        try
        {
            PrintTanksFullInfo(tanks, units, factories);
        }
        catch (Exception e) when (e is ArgumentException || e is InvalidOperationException)
        {
            Console.WriteLine($"Error while printing tanks full info: {e.Message}.");
        }

        try
        {
            int totalCurrentVolume = GetTotalCurrentVolume(tanks);
            Console.WriteLine($"Текущий объем жидкости во всех резервуарах: {totalCurrentVolume}.");
        }
        catch (Exception e) when (e is ArgumentException || e is InvalidOperationException)
        {
            Console.WriteLine($"Error while getting total current tanks capacity: {e.Message}.");
        }

        Console.Write("Введите название объекта для поиска: ");
        string nameToFind = Console.ReadLine();

        if (nameToFind is not null)
        {
            try
            {
                IEnumerable<INameable> allObjects = factories.Cast<INameable>().Concat(units.Cast<INameable>()).Concat(tanks.Cast<INameable>());
                var nameables = FindByName(allObjects, nameToFind);

                foreach (var nameable in nameables)
                {
                    if (nameable is Factory factory)
                    {
                        Console.WriteLine($"Factory {factory.Name}, {factory.Description}.");
                    }
                    else if (nameable is Unit unit)
                    {
                        try
                        {
                            Console.WriteLine($"Unit {unit.Name}, {unit.Description} located on factory {FindFactory(factories, unit).Name}.");
                        }
                        catch (Exception e) when (e is ArgumentException || e is InvalidOperationException)
                        {
                            Console.WriteLine($"Error while getting factory for found unit ID={unit.Id}: {e.Message}");
                        }
                    }
                    else if (nameable is Tank tank)
                    {
                        try
                        {
                            Console.WriteLine($"Tank {tank.Name}, {tank.Description} (volume {tank.Volume}/{tank.MaxVolume}) in unit {FindUnit(units, tank).Name}.");
                        }
                        catch (Exception e) when (e is ArgumentException || e is InvalidOperationException)
                        {
                            Console.WriteLine($"Error while getting unit for found tank ID={tank.Id}: {e.Message}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Got unexpected object type for the specified input.");
                    }
                }
            }
            catch (ArgumentException e)
            {
                Console.WriteLine($"Error while searching for object: {e.Message}.");
            }

        }
        else
        {
            Console.WriteLine("The input is empty.");
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
            Console.WriteLine($"Data has been saved to file: {stream.Name}.");
        }
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

        List<string> tableHeaderCellsValues = new ();
        foreach (var headerCell in tableHeaderCells)
        {
            string headerValue;

            if (!headerCell.TryGetValue<string>(out headerValue))
                throw new XLFormatException($"Table header value {headerCell.Address.ToString(XLReferenceStyle.Default, true)} has unexpected type.");

            tableHeaderCellsValues.Append(headerValue);
        }

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

    public static void PrintTanksFullInfo(IEnumerable<Tank> tanks, IEnumerable<Unit> units, IEnumerable<Factory> factories)
    {
        foreach (Tank tank in tanks)
        {
            Unit unit = FindUnit(units, tank);
            Factory factory = FindFactory(factories, unit);

            Console.WriteLine($"{tank.Name}, {tank.Description}: объем - {tank.Volume}/{tank.MaxVolume}; установка - {unit.Name}; завод - {factory.Name}");
        }
    }

    public static IEnumerable<INameable> FindByName(IEnumerable<INameable> nameables, string name)
    {
        var foundNameables = nameables.Where(nameable => nameable.Name == name);

        if (foundNameables.Count() == 0)
            throw new ArgumentException($"There is no nameables with name \"{name}\".");

        return foundNameables;
    }

    public static Unit FindUnit(IEnumerable<Unit> units, IEnumerable<Tank> tanks, string tankName)
    {
        IEnumerable<Tank> targetTanks;
        try
        {
            targetTanks = FindByName(tanks, tankName).Cast<Tank>();
        }
        catch (ArgumentException e)
        {
            throw new ArgumentException($"There is no tanks with name \"{tankName}\".", e);
        }

        if (targetTanks.Count() > 1)
            throw new InvalidOperationException($"There are several tanks with name \"{tankName}\".");

        Tank targetTank = targetTanks.ElementAt(0);
        
        return FindUnit(units, targetTank);
    }

    public static Unit FindUnit(IEnumerable<Unit> units, Tank tank)
    {
        var foundUnits = units.Where(unit => unit.Id == tank.UnitId);

        if (foundUnits.Count() > 1)
            throw new InvalidOperationException($"Several units correspond to the specified tank ID={tank.Id}.");

        if (foundUnits.Count() == 0)
            throw new ArgumentException($"None of the units correspond to the specified tank ID={tank.Id}.");

        return foundUnits.ElementAt(0);
    }

    public static Factory FindFactory(IEnumerable<Factory> factories, Unit unit)
    {
        var foundFactories = factories.Where(factory => factory.Id == unit.FactoryId);

        if (foundFactories.Count() > 1)
            throw new InvalidOperationException($"Several factories correspond to the specified unit ID={unit.Id}.");

        if (foundFactories.Count() == 0)
            throw new ArgumentException($"None of the factories correspond to the specified unit ID={unit.Id}.");

        return foundFactories.ElementAt(0);
    }

    public static int GetTotalCurrentVolume(IEnumerable<Tank> tanks)
    {
        if (tanks.Count() == 0)
            throw new InvalidOperationException("Tanks has not been passed.");

        return tanks.Sum(tank => tank.Volume);
    }

    public static int GetTotalMaxVolume(IEnumerable<Tank> tanks)
    {
        if (tanks.Count() == 0)
            throw new InvalidOperationException("Tanks has not been passed.");

        return tanks.Sum(tank => tank.MaxVolume);
    }
}
