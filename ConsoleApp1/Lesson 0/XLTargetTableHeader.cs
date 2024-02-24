using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

public class XLTargetTableHeader
{
    public IEnumerable<string> HeaderValues { get; private set; }
    public bool CaseSensitive { get; private set; }
    public bool OrderSensitive { get; private set; }
    public ConstructorInfo Constructor { get; private set; }

    public XLTargetTableHeader(IReadOnlyCollection<string> headerValues, ConstructorInfo constructor, bool caseSensitive = false, bool orderSensitive = false)
    {
        HeaderValues = headerValues;
        CaseSensitive = caseSensitive;
        OrderSensitive = orderSensitive;
        Constructor = constructor;
    }

    public int GetIndexInSonstructor(string headerValue)
    {
        var formattedHeaderValues = HeaderValues;

        if (!CaseSensitive)
        {
            formattedHeaderValues = formattedHeaderValues.Select(headerValue => headerValue.ToLower()).ToList();
            headerValue = headerValue.ToLower();
        }

        if (!formattedHeaderValues.Contains(headerValue))
            throw new ArgumentException($"The header \"{headerValue}\" is not among the targets.");

        return formattedHeaderValues.TakeWhile(targetHeaderValue => targetHeaderValue != headerValue).Count();
    }

    public bool AreEquals(IEnumerable<string> checkingHeaderValues)
    {
        var formattedHeaderValues = HeaderValues;

        if (!CaseSensitive)
        {
            checkingHeaderValues = checkingHeaderValues.Select(headerValue => headerValue.ToLower());
            formattedHeaderValues = formattedHeaderValues.Select(headerValue => headerValue.ToLower()).ToList();
        }

        if (!OrderSensitive)
        {
            checkingHeaderValues = checkingHeaderValues.OrderBy(headerValue => headerValue);
            formattedHeaderValues = formattedHeaderValues.OrderBy(headerValue => headerValue).ToList();
        }

        return checkingHeaderValues.SequenceEqual(formattedHeaderValues);
    }
}
