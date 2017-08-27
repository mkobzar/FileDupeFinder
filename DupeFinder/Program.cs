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
            //  var p = new ParseDirectory(args[0]);

            // avalize
            var a = new Analyze(args[0]);
        }
    }
}