using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading;
using System.Runtime.InteropServices;
using System.IO;

namespace System.AgentHub
{
    public static class CodeTimer
    {
        [DllImport("kernel32.dll")]
        static extern IntPtr GetCurrentThread();
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool GetThreadTimes(IntPtr hThread, out long lpCreationTime, out long lpExitTime, out long lpKernelTime, out long lpUserTime);
        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool QueryThreadCycleTime(IntPtr threadHandle, ref ulong cycleTime);

        static CodeTimer()
        {
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;
            Thread.CurrentThread.Priority = ThreadPriority.Highest;
            Time(string.Empty, 1, () => { });
        }

        public static void Time(string name, int iteration, Action action)
        {
            if (String.IsNullOrEmpty(name))
            {
                return;
            }

            // 1. Print name
            ConsoleColor currentForeColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(name);

            // 2. Record the latest GC counts
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
            int[] gcCounts = new int[GC.MaxGeneration + 1];
            for (int i = 0; i <= GC.MaxGeneration; i++)
            {
                gcCounts[i] = GC.CollectionCount(i);
            }

            // 3. Run action
            Stopwatch watch = new Stopwatch();
            watch.Start();
            long ticksFst = GetCurrentThreadTimes(); //100 nanosecond one tick
            for (int i = 0; i < iteration; i++)
            {
                action();
            }
            long ticks = GetCurrentThreadTimes() - ticksFst;
            watch.Stop();

            // 4. Print CPU
            Console.ForegroundColor = currentForeColor;
            Console.WriteLine("\tTime Elapsed:\t\t" + watch.ElapsedMilliseconds.ToString("N0") + "ms");
            Console.WriteLine("\tTime Elapsed(Per):\t" + (watch.ElapsedMilliseconds / iteration).ToString("N0") + "ms");
            Console.WriteLine("\tCPU Time:\t\t" + (ticks * 100).ToString("N0") + "ns");
            Console.WriteLine("\tCPU Time(Per):\t\t" + (ticks * 100 / iteration).ToString("N0") + "ns");

            // 5. Print GC
            for (int i = 0; i <= GC.MaxGeneration; i++)
            {
                int count = GC.CollectionCount(i) - gcCounts[i];
                Console.WriteLine("\tGen " + i + ": \t\t\t" + count);
            }

            Console.WriteLine();
        }

        public static void VistaAbove_Time(string name, int iteration, Action action)
        {
            if (String.IsNullOrEmpty(name))
            {
                return;
            }

            // 1.
            ConsoleColor currentForeColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(name);

            // 2.
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
            int[] gcCounts = new int[GC.MaxGeneration + 1];
            for (int i = 0; i <= GC.MaxGeneration; i++)
            {
                gcCounts[i] = GC.CollectionCount(i);
            }

            // 3.
            Stopwatch watch = new Stopwatch();
            watch.Start();
            ulong cycleCount = GetCycleCount();
            for (int i = 0; i < iteration; i++)
            {
                action();
            }
            ulong cpuCycles = GetCycleCount() - cycleCount;
            watch.Stop();

            // 4.
            Console.ForegroundColor = currentForeColor;
            Console.WriteLine("\tTime Elapsed:\t" + watch.ElapsedMilliseconds.ToString("N0") + "ms");
            Console.WriteLine("\tCPU Cycles:\t" + cpuCycles.ToString("N0"));

            // 5.
            for (int i = 0; i <= GC.MaxGeneration; i++)
            {
                int count = GC.CollectionCount(i) - gcCounts[i];
                Console.WriteLine("\tGen " + i + ": \t\t" + count);
            }

            Console.WriteLine();
        }

        private static long GetCurrentThreadTimes()
        {
            long l;
            long kernelTime, userTimer;
            GetThreadTimes(GetCurrentThread(), out l, out l, out kernelTime, out userTimer);
            return kernelTime + userTimer;
        }
        private static ulong GetCycleCount()
        {
            ulong cycleCount = 0;
            QueryThreadCycleTime(GetCurrentThread(), ref cycleCount);
            return cycleCount;
        }
    }

    public static class TextWriterExtensions
    {
        public static void WriteInfo(this TextWriter writer, string format, params object[] args)
        {
            writer.WriteLine(ConsoleColor.White, format, args);
        }
        public static void WriteTip(this TextWriter writer, string format, params object[] args)
        {
            writer.WriteLine(ConsoleColor.Green, format, args);
        }
        public static void WriteWarning(this TextWriter writer, string format, params object[] args)
        {
            writer.WriteLine(ConsoleColor.Yellow, format, args);
        }
        public static void WriteError(this TextWriter writer, string format, params object[] args)
        {
            writer.WriteLine(ConsoleColor.Red, format, args);
        }

        public static void WriteLine(this TextWriter writer, ConsoleColor color, string format, params object[] args)
        {
            Console.ForegroundColor = color;
            writer.WriteLine(format, args);
            Console.ResetColor();
        }
    }
}