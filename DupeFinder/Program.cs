using System;

namespace FileDupeFinder
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("path is missing or not exist");
                return;
            }
            
            // parse
             new DirectoryParser(args[0]).Parse();
            
            // avalize
            // var a = new Analyze(args[0]);
        }
    }
}