using System;
using System.Threading.Tasks;

namespace GoPlay.Services.Core.Utils
{
    public static class AppUtil
    {
        /// <summary>
        /// 退出使用： kill -15
        /// </summary>
        /// <returns></returns>
        public static Task WaitForKill()
        {
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var sigintReceived = false;

            Console.WriteLine("Waiting for SIGINT/SIGTERM");

            Console.CancelKeyPress += (_, ea) =>
            {
                if (sigintReceived) return;
                
                // Tell .NET to not terminate the process
                sigintReceived = true;
                ea.Cancel = true;

                Console.WriteLine("Received SIGINT (Ctrl+C)");
                tcs.SetResult(true);
            };

            AppDomain.CurrentDomain.ProcessExit += (o, eventArgs) =>
            {
                if (!sigintReceived)
                {
                    Console.WriteLine("Received SIGTERM");
                    tcs.SetResult(true);
                }
                else
                {
                    Console.WriteLine("Received SIGTERM, ignoring it because already processed SIGINT");
                }
            };

            return tcs.Task;
        }
    }
}