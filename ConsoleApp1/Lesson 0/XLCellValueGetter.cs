using System;
using ClosedXML.Excel;

public static class XLCellValueGetter
{
    public static int GetInt(XLCellValue cellValue)
    {
        if (!cellValue.IsNumber)
            throw new InvalidCastException($"Cell value \"{cellValue}\" is not a number.");

        double cellNumberValue = cellValue.GetNumber();
        int cellIntValue = (int)cellNumberValue;

        if (cellIntValue != cellNumberValue)
            throw new InvalidCastException($"Cell value \"{cellValue}\" is not an integer.");

        return cellIntValue;
    }

    public static string GetString(XLCellValue cellValue)
    {
        string textValue;

        if (!cellValue.TryGetText(out textValue))
            throw new InvalidCastException($"Cell value \"{cellValue}\" is not a text.");

        return textValue;
    }
}