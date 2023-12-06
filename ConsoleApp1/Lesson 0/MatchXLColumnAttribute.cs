using System;

public class MatchXLColumnAttribute : Attribute
{
    public string Name { get; }
    public MatchXLColumnAttribute(string columnName) => Name = columnName;
}
