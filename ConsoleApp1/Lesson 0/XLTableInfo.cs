using ClosedXML.Excel;

public record XLTablePositioningInfo(IXLWorksheet Worksheet, int FirstColumnNumber, int FirstRowNumber, int RowsRangeCount);
