using Microsoft.Web.Administration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IISPoolMonitorExample
{
    class Program
    {

        static void Main(string[] args)
        {
            var serverManager = new ServerManager();
            var pool = serverManager.WorkerProcesses.Where(_ => _.AppPoolName == "Default App Pool").FirstOrDefault();
            Console.Title = "App Pool IIS Monitor";

            if (pool != null)
            {
                var proc = Process.GetProcessById(pool.ProcessId);
                do
                {
                    while (!Console.KeyAvailable)
                    {
                        proc.Refresh();
                        Console.Clear();
                        Console.ResetColor();
                        Console.SetCursorPosition(0, Console.CursorTop);

                        Console.ForegroundColor = ConsoleColor.DarkMagenta;
                        Console.WriteLine("Press Space to exit");
                        Console.ForegroundColor = ConsoleColor.DarkGreen;
                        Console.WriteLine("IIS App Pool PID:{0}", pool.ProcessId);
                        Console.ForegroundColor = ConsoleColor.DarkMagenta;
                        Console.WriteLine("Memory");
                        Console.ForegroundColor = ConsoleColor.DarkGreen;
                        Console.WriteLine("NonPaged: {0:0.00}Kb", proc.NonpagedSystemMemorySize64 / 1024f);
                        Console.WriteLine("Paged: {0:0}Kb", proc.PagedMemorySize64 / 1024f);
                        Console.WriteLine("Virtual: {0:0}Kb", proc.VirtualMemorySize64 / 1024f);
                        Console.WriteLine("Private: {0:0}Kb\r\n", proc.PrivateMemorySize64 / 1024f);
                        Console.ForegroundColor = ConsoleColor.DarkMagenta;
                        Console.WriteLine("Process");
                        Console.ForegroundColor = ConsoleColor.DarkGreen;
                        Console.WriteLine("State: {0} Uptime:{1:0.0}h", pool.State.ToString(), ((TimeSpan)(DateTime.Now - proc.StartTime)).TotalHours);
                        Console.WriteLine("CPU: {0:0.0000}min", proc.TotalProcessorTime.TotalMinutes);
                        Console.WriteLine("Threads: {0}\r\n", proc.Threads.Count);
                        var requests = pool.GetRequests(0);

                        Console.WriteLine("Requests: {0}\r\n", requests.Count);
                        Console.ForegroundColor = ConsoleColor.DarkCyan;
                        foreach (var r in requests.Take(8))
                        {
                            Console.WriteLine("IP: {0} Url:{1} Time:{2}", r.ClientIPAddr, r.Url, r.TimeElapsed);
                        }
                        Thread.Sleep(2000);
                    }
                } while (Console.ReadKey(true).Key != ConsoleKey.Spacebar);
            }
            else
            {
                Console.WriteLine("IIS Pool Not Found!");
                Console.ReadKey();
            }
        }
    }
}
    
