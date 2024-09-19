using System;
using System.Diagnostics;
using static Updater.Updater;
namespace Updater
{
    class Program
    {
        public const string PrgmName = "Updater";
        public const string Ver = "1.0.0.4";
        public static string Version = Ver;
        public static string path = System.IO.Directory.GetCurrentDirectory();
        static void Main(string[] args)
        {
            if (args.CaselessContains("OpenGL") && !args.CaselessContains("64"))
            {
                exe = BaseURL + "ClassiCube.opengl.exe";
            }
            else if (args.CaselessContains("OpenGL") && args.CaselessContains("64"))
            {
                exe = BaseURL + "ClassiCube.64-opengl.exe";
            }
            else if (!args.CaselessContains("OpenGL") && args.CaselessContains("64"))
            {
                exe = BaseURL + "ClassiCube.64.exe";
            }
            else if (args == null)
            {
                exe = BaseURL + "ClassiCube.64.exe";
            }
            else
            {
                exe = BaseURL + "ClassiCube.exe";
            }
            Console.Title = PrgmName + " " + Version;
            Console.WriteLine("Updating ClassiCube to latest dev build from: ");
            Console.WriteLine(exe);

            try
            {
                PerformUpdate(args);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error performing update:");
                Console.WriteLine(ex.ToString());
                Console.ReadKey(false);
                return;
            }
            /*try
            {
                Process.Start(path + "/ClassiCube.exe");
            }
            catch (Exception ex) 
            {
                Console.WriteLine("Error performing update:");
                Console.WriteLine(ex.ToString());
                Console.ReadKey(false);
                return;
            }*/

            Environment.Exit(0);
        }
    }
}