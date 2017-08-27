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
            var v = new ParseDirectory(args[0]);
        }
    }
}