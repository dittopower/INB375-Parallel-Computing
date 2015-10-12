using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace WpfApplication1
{
    public class Time
    {
        long nanosecPerTick = (1000L * 1000L * 1000L) / Stopwatch.Frequency;
        Stopwatch timeOperations = Stopwatch.StartNew();

        public Time(){
            timeOperations.Start();
        }
        public void start()
        {
            timeOperations.Reset();
            timeOperations.Start();
        }

        public void end(string what = ""){
            timeOperations.Stop();
            long timetaken = timeOperations.ElapsedTicks * nanosecPerTick;
            long ms = timeOperations.ElapsedMilliseconds;
            Console.WriteLine("{1}:\n\t{2}ms\n\t{0}ns",timetaken,what,ms);
        }

        public void next(string what = ""){
            end(what);
            start();
        }
    }
    
}
