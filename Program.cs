using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrackedLock
{
    class Program
    {
        static void Main(string[] args)
        {
            TestRunner runner = new TestRunner();
            runner.RunTests(typeof(BasicTests), Console.Out);
            runner.RunTests(typeof(ThreadTests), Console.Out);
            Console.ReadKey();
        }
    }
}
