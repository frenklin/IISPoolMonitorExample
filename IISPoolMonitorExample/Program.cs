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

        static private WorkerProcess _poolWorker = null;
        static private WorkerProcess poolWorker
        {
            get
            {
                try
                {
                    if (_poolWorker == null || _poolWorker.State != WorkerProcessState.Running)
                    {
                        var serverManager = new ServerManager();                        
                        _poolWorker = serverManager.WorkerProcesses.Where(_ => _.AppPoolName == ConfigurationManager.AppSettings["PoolName"]).FirstOrDefault();
                    }
                    return _poolWorker;
                }catch(Exception ex)
                {
                    Console.WriteLine("Get new poolWorker");
                    Console.WriteLine(ex.Message);
                    Thread.Sleep(1000);
                    _poolWorker = null;
                }
                return null;
            }
        }

        static private Process _poolProcess = null;
        static private Process poolProcess
        {
            get
            {
                try {
                    if (poolWorker != null && (_poolProcess==null || _poolProcess.HasExited))
                    {
                        _poolProcess=Process.GetProcessById(poolWorker.ProcessId);
                    }
                    return _poolProcess;
                }
                catch(Exception ex)
                {
                    _poolProcess = null;
                    _poolWorker = null;
                    Console.WriteLine("Get new poolProcess");
                    Console.WriteLine(ex.Message);
                    Thread.Sleep(1000);
                }
                return null;
            }
        }

        static void Main(string[] args)
        {            
                      
            Console.Title = "App Pool IIS Monitor";
            Console.WindowWidth = 120;
            Console.WindowHeight = 35;

            if (poolProcess != null)
            {
                
                var oldCPUTime = poolProcess.TotalProcessorTime;
                var lastMonitorTime = DateTime.UtcNow;

                do
                {
                    while (!Console.KeyAvailable)
                    {
                        try {
                            poolProcess.Refresh();
                            Console.Clear();
                            Console.ResetColor();
                            Console.SetCursorPosition(0, Console.CursorTop);

                            Console.ForegroundColor = ConsoleColor.DarkMagenta;
                            Console.WriteLine("Press Space to exit");
                            Console.ForegroundColor = ConsoleColor.DarkGreen;
                            Console.WriteLine("IIS App Pool PID:{0}", poolWorker.ProcessId);
                            Console.ForegroundColor = ConsoleColor.DarkMagenta;
                            Console.WriteLine("Memory");
                            Console.ForegroundColor = ConsoleColor.DarkGreen;
                            Console.WriteLine("NonPaged: {0:0.00}Kb", poolProcess.NonpagedSystemMemorySize64 / 1024f);
                            Console.WriteLine("Paged: {0:N0}Kb", poolProcess.PagedMemorySize64 / 1024f);
                            Console.WriteLine("Virtual: {0:N0}Kb", poolProcess.VirtualMemorySize64 / 1024f);
                            Console.WriteLine("Private: {0:N0}Kb\r\n", poolProcess.PrivateMemorySize64 / 1024f);
                            Console.ForegroundColor = ConsoleColor.DarkMagenta;
                            Console.WriteLine("Process");
                            Console.ForegroundColor = ConsoleColor.DarkGreen;
                            Console.WriteLine("State: {0} Uptime:{1:0.0}h", poolWorker.State.ToString(), ((TimeSpan)(DateTime.Now - poolProcess.StartTime)).TotalHours);

                            var cpuUsage = (poolProcess.TotalProcessorTime - oldCPUTime).TotalSeconds / (Environment.ProcessorCount * DateTime.UtcNow.Subtract(lastMonitorTime).TotalSeconds);
                            lastMonitorTime = DateTime.UtcNow;
                            oldCPUTime = poolProcess.TotalProcessorTime;

                            Console.WriteLine("CPU: {0:0}%", cpuUsage * 100);

                            Console.WriteLine("Threads: {0}\r\n", poolProcess.Threads.Count);
                            var requests = poolWorker.GetRequests(0);

                            Console.WriteLine("Requests: {0}\r\n", requests.Count);
                            Console.ForegroundColor = ConsoleColor.DarkCyan;
                            Console.WriteLine("{0,-16} {1, 7}  {2}", "IP", "Time(ms)", "Request");
                            foreach (var r in requests.Take(8))
                            {
                                Console.WriteLine("{0,-16} {1, 7}  {2}", r.ClientIPAddr, r.TimeElapsed, r.Url);
                            }
                            Thread.Sleep(2000);                            
                        }catch (Exception ex){                          
                            
                            if (poolProcess == null)
                            {
                                Console.WriteLine("poolProcess is null");
                                Console.WriteLine(ex.Message);                              
                            }                            
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
    
