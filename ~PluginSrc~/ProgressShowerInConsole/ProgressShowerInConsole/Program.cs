using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ProgressShowerInConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            var pipeName = "";
            if (args != null && args.Length > 0)
            {
                pipeName = args[0];
            }
            if (pipeName == null)
            {
                pipeName = "";
            }

            Console.OutputEncoding = Encoding.UTF8;

            var thd_Read = new System.Threading.Thread(() =>
            {
                try
                {
                    using (var pipe = new System.IO.Pipes.NamedPipeClientStream(".", "ProgressShowerInConsole" + pipeName, System.IO.Pipes.PipeDirection.In))
                    {
                        pipe.Connect();
                        var sr = new System.IO.StreamReader(pipe);
                        while (true)
                        {
                            var line = sr.ReadLine();
                            if (line == "\uEE05Message")
                            {
                                var mess = sr.ReadLine();
                                Console.WriteLine(mess);
                            }
                            else if (line == "\uEE05Title")
                            {
                                var title = sr.ReadLine();
                                Console.Title = title;
                                Console.WriteLine("Showing progress of " + title);
                            }
                            else if (line == "\uEE05Quit")
                            {
                                break;
                            }
                        }
                    }
                }
                finally
                {
                    Console.WriteLine("Closing...");
                    System.Threading.Thread.Sleep(3000);
                }
            });
            thd_Read.Start();

            var thd_Control = new System.Threading.Thread(() =>
            {
                try
                {
                    while (true)
                    {
                        var kinfo = Console.ReadKey(true);
                        if (kinfo.Key == ConsoleKey.Q && (kinfo.Modifiers & ConsoleModifiers.Control) != 0)
                        {
                            Console.Error.WriteLine("\uEE05Quit");
                            Console.Error.Flush();
                            break;
                        }
                    }
                }
                finally
                {
                    thd_Read.Abort();
                }
            });
            thd_Control.Start();

            thd_Read.Join();
            Environment.Exit(0);
        }
    }
}
