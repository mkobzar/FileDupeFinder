using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace FileDupeFinder
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                PrintUsage();
                return;
            }
            switch (args[0])
            {
                case "/a":
                    // avalize
                    new Analyzer(args[1]).Analyze();
                    break;
                case "/p":
                    // parse
                    new DirectoryParser(args[1]).Parse();
                    break;
                default:
                    PrintUsage();
                    break;
            }
        }

        static void PrintUsage()
        {
            Console.WriteLine("invalid arguments\n");
            Console.WriteLine("input example to parse directory:\n\tFileDupeFinder.exe /p c:\\Pictures");
            Console.WriteLine(
                "\ninput example to analyze csv file with parsed data:\n\tFileDupeFinder.exe /a c:\\Files\\myFiles.csv");
        }
    }
}