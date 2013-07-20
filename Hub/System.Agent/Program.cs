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
            bool isTunnelClient = true;
            try
            {
                if (isTunnelClient)
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
                    return;
                }

            test:
                Console.Out.WriteInfo("Start test...");
                CodeTimer.Time("Test:", 1, Action);
                Console.Out.WriteInfo("Press enter to continue...");
                var key = Console.ReadKey();
                if (key.Key == ConsoleKey.Enter)
                {
                    goto test;
                }
            }
            catch (Exception ex)
            {
                Hub.LogError(ex, Console.Title);
                Console.Out.WriteError(ex.Message);
            }
            finally
            {
                if (!isTunnelClient)
                {
                    Console.Out.WriteInfo("Press any key to exit...");
                    Console.Read();
                }
            }
        }

        private static void Action()
        {
            //var mgr = new NoSQLTest();
            //mgr.Linq2Cache();
            //mgr.SqlChangeMonitor();
            //mgr.Linq2CacheWithSqlChangeMonitor();
        }
    }
}