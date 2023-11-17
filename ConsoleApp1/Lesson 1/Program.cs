using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Encodings;
using System.Text;
using System.Linq;
using System.Globalization;

class ProgramOne
{
    public static void Start()
    {
        IList<Deal> deals = ParseJson<IList<Deal>>("JSON_sample_1.json");
        Console.WriteLine(deals.Count);
        Console.WriteLine(string.Join(", ", GetNumbersOfDeals(deals)));
        Console.WriteLine(string.Join("\n", GetSumsByMonth(deals).OrderBy(sum => sum.Month).Select(sum => $"{CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(sum.Month.Month).ToUpperInvariant()}: {sum.Sum}")));
    }

    record SumByMonth(DateTime Month, int Sum);

    static IList<SumByMonth> GetSumsByMonth(IEnumerable<Deal> deals)
    {
        return deals.GroupBy(deal => new DateTime(deal.Date.Year, deal.Date.Month, 1)).Select(group => new SumByMonth(group.Key, group.Sum(deal => deal.Sum))).ToList();
    }

    static IList<string> GetNumbersOfDeals(IEnumerable<Deal> deals)
    {
        return deals.Where(deal => deal.Sum >= 100).OrderBy(deal => deal.Date).Take(5).OrderByDescending(deal => deal.Sum).Select(deal => deal.Id).ToList();
    }

    static T ParseJson<T>(string fileName)
    {
        using (FileStream stream = new FileStream(fileName, FileMode.Open))
        {
            return JsonSerializer.Deserialize<T>(stream);
        };
    }
}

class Deal
{
    public DateTime Date { get; set; }
    public string Id { get; set; }
    public int Sum { get; set; }
}