using Microsoft.WindowsAzure.ServiceRuntime.Internal;
using System;
using System.Diagnostics;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            var result = InteropRoleManager.Initialize();
            WriteLine("Initiliaze:" + result);

            var getResult = InteropRoleManager.GetConfigurationSetting("foo", out string value);
            WriteLine("GetCscfgSetting('foo'):" + value);

            var regStatus = InteropRoleManager.RegisterRoleStatusCallBack(RoleStatusCallback);
            WriteLine("Register status callback:" + regStatus);

            var regShutdown = InteropRoleManager.RegisterRoleShutdownCallBack(RoleShutdownCallback);
            WriteLine("Register shutdown callback:" + regShutdown);

            Console.Write("Enter key...");
            Console.ReadLine();
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
