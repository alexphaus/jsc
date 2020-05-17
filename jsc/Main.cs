using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jsc
{
    class Program
    {
        static void Main(string[] args)
        {
            string src = System.IO.File.ReadAllText(args.Length == 0 ? "test.js" : args[0]);
            var w = new System.Diagnostics.Stopwatch();
            w.Start();
            var lang = new jsc(src);
            lang.Run();
            w.Stop();
            Console.WriteLine("~" + w.Elapsed.TotalSeconds.ToString());
        }
    }
}