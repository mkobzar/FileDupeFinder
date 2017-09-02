using System;

namespace FileDupeFinder
{
    class Program
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {

            if (args.Length < 2)
            {
                PrintUsage();
                return;
            }
            switch (args[0])
            {
                case "/a":
                    // analyze
                    new Analyzer(args).Analyze();
                    break;
                case "/u":
                    // analyze diff for update 
                    new UpdateDiff(args).Run();
                    break;
                case "/p":
                    // parse
                    new DirectoryParser(args[1]).Parse();
                    break;
                case "/m":
                    // move file
                    if (args.Length < 3)
                    {
                        PrintUsage();
                        return;
                    }
                    new Merge(args[1], args[2], true).Run();
                    break;
                case "/c":
                    // move file
                    if (args.Length < 3)
                    {
                        PrintUsage();
                        return;
                    }
                    new Merge(args[1], args[2], false).Run();
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