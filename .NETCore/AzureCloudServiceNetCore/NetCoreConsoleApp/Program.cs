using Microsoft.WindowsAzure.ServiceRuntime.Internal;
using System;
using System.Diagnostics;
using System.Threading;

namespace ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var result = Microsoft.WindowsAzure.ServiceRuntime.Internal.InteropRoleManager.Initialize();
            WriteLine("Initiliaze:" + result);

            var getResult = Microsoft.WindowsAzure.ServiceRuntime.Internal.InteropRoleManager.GetConfigurationSetting("foo", out string value);
            WriteLine("GetCscfgSetting('foo'):" + value);

            var regStatus = Microsoft.WindowsAzure.ServiceRuntime.Internal.InteropRoleManager.RegisterRoleStatusCallBack(RoleStatusCallback);
            WriteLine("Register status callback:" + regStatus);

            var regShutdown = Microsoft.WindowsAzure.ServiceRuntime.Internal.InteropRoleManager.RegisterRoleShutdownCallBack(RoleShutdownCallback);
            WriteLine("Register shutdown callback:" + regShutdown);

            Run();
        }

        static void Run()
        {
            while(true)
            {
                Thread.Sleep(5000);
                WriteLine("Running ... ");
            }
        }

        static void WriteLine(string text)
        {
            Console.Out.WriteLine(Process.GetCurrentProcess().ProcessName + ":" + text);
        }

        static int RoleStatusCallback(out RoleStatus status)
        {
            WriteLine("Status callback check");
            status = RoleStatus.RoleStatusHealthy;

            return 0;
        }

        static int RoleShutdownCallback()
        {
            WriteLine("Shutdown called.");
            Environment.Exit(0);

            return 0;
        }
    }
}
