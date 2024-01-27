using ClosedXML.Excel;
using System;

public class XLTablePositioningInfo
{
    public IXLWorksheet Worksheet { get; private set; }
    public int FirstColumnNumber { get; private set; }
    public int FirstRowNumber { get; private set; }
    public int RecordsAmount { get; private set; }

    public XLTablePositioningInfo(XLWorkbook workbook, int worksheetPosition)
    {
        Worksheet = GetWorksheet(workbook, worksheetPosition);
        var firstCell = GetFirstUsedCell(Worksheet);
        FirstColumnNumber = firstCell.WorksheetColumn().ColumnNumber();
        FirstRowNumber = firstCell.WorksheetRow().RowNumber();
        RecordsAmount = firstCell.WorksheetColumn().LastCellUsed().WorksheetRow().RowNumber() - FirstRowNumber;
    }

    public static IXLWorksheet GetWorksheet(XLWorkbook workbook, int worksheetPosition)
    {
        if (worksheetPosition < 1)
            throw new ArgumentOutOfRangeException("Worksheet position can't be less then 1.");

        if (workbook.Worksheets.Count < worksheetPosition)
            throw new ArgumentOutOfRangeException($"Excel document has {workbook.Worksheets.Count} worksheets, tried to open {worksheetPosition}.");

        return workbook.Worksheet(worksheetPosition);
    }

    public static IXLCell GetFirstUsedCell(IXLWorksheet worksheet)
    {
        var firstCell = worksheet.FirstCellUsed();

        if (firstCell is null)
            throw new FormatException($"Worksheet {worksheet.Position} is empty");
        else if (firstCell.GetText() != "Id")
            throw new FormatException("Unexpected value of the first used cell.");

        return firstCell;
    }
}
