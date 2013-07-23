using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace System.Agent
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            SecurityPolicy.Check();
            try
            {
                LockEntry();
                return;
                short cmd;
                if (args.Length > 0 && short.TryParse(args[0], out cmd))
                {
                    switch (cmd)
                    {
                        case 1:
                            LockEntry();
                            break;
                        default:
                            Hub.LogDebug("Unknow Cmd: {0}.", cmd);
                            break;
                    }
                    return;
                }
                TunnelEntry();

            test:
                Console.Out.WriteInfo("Start test...");
                CodeTimer.Time("Test:", Iteration, Action);
                Console.Out.WriteInfo("Press enter to continue...");
                var key = Console.ReadKey();
                if (key.Key == ConsoleKey.Enter)
                {
                    goto test;
                }
                Console.Out.WriteInfo("Press any key to exit...");
                Console.Read();
            }
            catch (Exception ex)
            {
                Hub.LogError(ex, Console.Title);
                Console.Out.WriteError(ex.Message);
            }
        }

        private const int Iteration = 1;
        private static void Action()
        {
            //var mgr = new NoSQLTest();
            //mgr.Linq2Cache();
            //mgr.Linq2CacheWithSqlChangeMonitor();
        }

        private static void TunnelEntry()
        {
            string name = "飞檐走壁", ver = ConsoleNotify.GetVersion();
            Console.Title = string.Format("{0} {1} - 专注网络通讯", name, ver);
            Console.Out.WriteInfo(@"{0} Agent [Version {1}]
Copyright (c) 2012 JeansMan Studio。
", name, ver);
            //Mutex如不在这里，则不会生效
            bool createNew;
            var mutex = new Mutex(false, typeof(Program).FullName, out createNew);
            var console = new ConsoleNotify(name, createNew, true);
            console.Run(new AgentApp(), new MainForm());
        }

        private static void LockEntry()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new System.Agent.Privacy.IdleForm());
        }
    }
}