using System;
using System.Collections.Generic;

class Program
{
    static int Solve(List<string> lines)
    {
        // TODO: Реализация алгоритма
        return 0;
    }

    static void Main()
    {
        var lines = new List<string>();
        string line;

        while ((line = Console.ReadLine()) != null)
        {
            lines.Add(line);
        }

        int result = Solve(lines);
        Console.WriteLine(result);
    }
}