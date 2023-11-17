using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Encodings;
using System.Text;

class ProgramOne
{
    static void Main(string[] args)
    {
        Console.WriteLine(ParseJson("JSON_sample_1.json"));
    }

    static Deal[] ParseJson(string fileName)
    {
        using (FileStream stream = new FileStream(fileName, FileMode.Open))
        {
            return JsonSerializer.Deserialize<Deal[]>(stream);
        };
    }
}

class Deal
{
    public DateTime Date { get; set; }
    public string Id { get; set; }
    public int Sum { get; set; }
    
    public Deal(int sum, string id, DateTime dateTime)
    {
        if (sum <= 0)
            throw new ArgumentException("Sum of deal can't be less then 1");

        Sum = sum;
        Id = id;
        Date = dateTime;
    }
}