using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

class TableObjectsCreator<T>
{
    private static List<ConstructorInfo> _constructors = new();

    private ConstructorInfo _targetConstructor;
    private IEnumerable<int> _tableColumnsPositionInConstructor;

    static TableObjectsCreator()
    {
        _constructors = typeof(T).GetConstructors().Where(constructor => constructor.GetCustomAttributes(false).Any(attribute => attribute is TableImportableAttribute)).ToList();

        if (_constructors.Count == 0)
            throw new ArgumentException($"{typeof(T).Name} does not have a constructor with the {typeof(TableImportableAttribute).Name} attribute.");
    }

    public static bool TryMatch(IEnumerable<string> tableHeaderValues, bool caseSensitive, bool orderSensitive, out TableObjectsCreator<T> configuredCreator)
    {
        configuredCreator = null;
        var formattedTableHeaderValues = tableHeaderValues;

        if (!caseSensitive)
            formattedTableHeaderValues = formattedTableHeaderValues.Select(headerValue => headerValue.ToLower());

        if (!orderSensitive)
            formattedTableHeaderValues = formattedTableHeaderValues.OrderBy(headerValue => headerValue);

        foreach (var constructor in _constructors)
        {
            var constructorParameters = constructor.GetParameters().Select(parameter => parameter.Name);
            var formattedConstructorParameters = constructorParameters;

            if (!caseSensitive)
                formattedConstructorParameters = formattedConstructorParameters.Select(headerValue => headerValue.ToLower());

            if (!orderSensitive)
                formattedConstructorParameters = formattedConstructorParameters.OrderBy(headerValue => headerValue);

            if (formattedConstructorParameters.SequenceEqual(formattedTableHeaderValues))
            {
                constructorParameters = constructorParameters.Select(constructorParameter => constructorParameter.ToLower());
                tableHeaderValues = tableHeaderValues.Select(tableHeaderValue => tableHeaderValue.ToLower());
                List<int> tableColumnsPositionInConstructor = new();

                foreach (var tableHeaderValue in tableHeaderValues)
                {
                    tableColumnsPositionInConstructor.Add(constructorParameters.TakeWhile(targetHeaderValue => targetHeaderValue != tableHeaderValue).Count());
                }
                
                if (tableColumnsPositionInConstructor.Count() != tableColumnsPositionInConstructor.Distinct().Count())
                {
                    throw new ArgumentException("Multiple table headers correspond to the same index in the constructor.");
                }

                configuredCreator = new TableObjectsCreator<T>(tableColumnsPositionInConstructor, constructor);

                return true;
            }
        }

        return false;
    }

    public TableObjectsCreator(IEnumerable<int> tableColumnsPositionInConstructor, ConstructorInfo constructor)
    {
        if (constructor.DeclaringType != typeof(T))
            throw new ArgumentException($"Provided constructor is not {typeof(T).Name} constructor");

        if (tableColumnsPositionInConstructor.Count() != constructor.GetParameters().Length)
            throw new ArgumentException("Number of table columns positions is not equal to constructor parameters amount.");

        _tableColumnsPositionInConstructor = tableColumnsPositionInConstructor;
        _targetConstructor = constructor;
    }

#nullable enable
    public T Create(IEnumerable<object?>? parameters)
    {
        return (T)_targetConstructor.Invoke(parameters?.ToArray());
    }
#nullable disable

    public Type GetParameterType(int tableColumnIndex)
    {
        if (tableColumnIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(tableColumnIndex), "Table column index can't be less then 0.");

        if (tableColumnIndex > _tableColumnsPositionInConstructor.Count() - 1)
            throw new ArgumentOutOfRangeException(nameof(tableColumnIndex), "Provided table column index out of constructor parameters range.");

        return _targetConstructor.GetParameters()[tableColumnIndex].ParameterType;
    }
}
