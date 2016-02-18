using Microsoft.Web.Administration;
using System;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace IISPoolMonitor
{
    class Program
    {

        static void Main(string[] args)
        {            
            var serverManager = new ServerManager();            
            var pool = serverManager.WorkerProcesses.Where(_ => _.AppPoolName == ConfigurationManager.AppSettings["PoolName"]).FirstOrDefault();
            Console.Title = "App Pool IIS Monitor";
            Console.WindowWidth = 120;
            Console.WindowHeight = 35;

            if (pool != null)
            {
                var proc = Process.GetProcessById(pool.ProcessId);
                var oldCPUTime = proc.TotalProcessorTime;
                var lastMonitorTime = DateTime.UtcNow;

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
                        Console.WriteLine("Paged: {0:N0}Kb", proc.PagedMemorySize64 / 1024f);
                        Console.WriteLine("Virtual: {0:N0}Kb", proc.VirtualMemorySize64 / 1024f);
                        Console.WriteLine("Private: {0:N0}Kb\r\n", proc.PrivateMemorySize64 / 1024f);
                        Console.ForegroundColor = ConsoleColor.DarkMagenta;
                        Console.WriteLine("Process");
                        Console.ForegroundColor = ConsoleColor.DarkGreen;
                        Console.WriteLine("State: {0} Uptime:{1:0.0}h", pool.State.ToString(), ((TimeSpan)(DateTime.Now - proc.StartTime)).TotalHours);
                        
                        var cpuUsage = (proc.TotalProcessorTime - oldCPUTime).TotalSeconds / (Environment.ProcessorCount * DateTime.UtcNow.Subtract(lastMonitorTime).TotalSeconds);
                        lastMonitorTime = DateTime.UtcNow;
                        oldCPUTime = proc.TotalProcessorTime;

                        Console.WriteLine("CPU: {0:0}%", cpuUsage*100);
                       
                        Console.WriteLine("Threads: {0}\r\n", proc.Threads.Count);
                        var requests = pool.GetRequests(0);

                        Console.WriteLine("Requests: {0}\r\n", requests.Count);
                        Console.ForegroundColor = ConsoleColor.DarkCyan;
                        Console.WriteLine("{0,-16} {1, 7}  {2}", "IP", "Time(ms)", "Request");
                        foreach (var r in requests.Take(8))
                        {
                            Console.WriteLine("{0,-16} {1, 7}  {2}", r.ClientIPAddr, r.TimeElapsed, r.Url);
                        }
                        Thread.Sleep(2000);
                        if (proc.HasExited)
                        {
                            Console.WriteLine("Pool restarting....");
                            Thread.Sleep(2000);
                            pool = serverManager.WorkerProcesses.Where(_ => _.AppPoolName == ConfigurationManager.AppSettings["PoolName"]).FirstOrDefault();
                            if (pool == null)
                            {
                                Console.WriteLine("IIS Pool Not Found!");
                                Console.ReadLine();
                                Thread.CurrentThread.Abort();
                            }
                            proc = Process.GetProcessById(pool.ProcessId);

                        }
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
    
