using System;

public class MatchXLConstructorAttribute : Attribute
{
    public XLConstructorArgumentsNamesFormat ArgumentsNamesFormat { get; private set; }

    public MatchXLConstructorAttribute()
    {
        ArgumentsNamesFormat = XLConstructorArgumentsNamesFormat.NoFormat;
    }

    public MatchXLConstructorAttribute(XLConstructorArgumentsNamesFormat argumentsFormat)
    {
        ArgumentsNamesFormat = argumentsFormat;
    }
}

public enum XLConstructorArgumentsNamesFormat
{
    NoFormat,
    ToTitleCase
}
