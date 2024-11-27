using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using a6;
using a6_win;

namespace TestHarness
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Logger.Log("This is a test");

            Console.WriteLine("Program.Ended");
            Console.ReadKey();
        }
    }
}
