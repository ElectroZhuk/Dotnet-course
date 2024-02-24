using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class XLCompatible
{
    public static IEnumerable<XLTargetTableHeader> XLTableHeaderVariants;

    static XLCompatible()
    {
        var xlImportableConstructors = typeof().GetConstructors().Where(constructor => constructor.GetCustomAttributes(false).Any(attribute => attribute is TableImportableAttribute));
        List<XLTargetTableHeader> xlTableHeaderVariants = new();

        foreach (var constructor in xlImportableConstructors)
        {
            xlTableHeaderVariants.Add(new XLTargetTableHeader(constructor.GetParameters().Select(parameter => parameter.Name).ToList(), constructor));
        }

        XLTableHeaderVariants = xlTableHeaderVariants;
    }
}
