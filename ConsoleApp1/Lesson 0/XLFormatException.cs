using System;

class XLFormatException: Exception
{
    public XLFormatException() : base() { }

    public XLFormatException(string message) : base(message) { }

    public XLFormatException(string message, Exception innerException) : base(message, innerException) { }
}
