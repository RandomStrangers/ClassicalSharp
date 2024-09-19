using System;
using System.Diagnostics;
using System.Net;
using System.Reflection;
namespace Updater
{
    public static class Updater
    {
        class CustomWebClient : WebClient
        {
            protected override WebRequest GetWebRequest(Uri address)
            {
                HttpWebRequest req = (HttpWebRequest)base.GetWebRequest(address);
                req.ServicePoint.BindIPEndPointDelegate = BindIPEndPointCallback;
                req.UserAgent = "Updater";
                return req;
            }
        }
        public static INetListen Listener = new TcpListen();
        static IPEndPoint BindIPEndPointCallback(ServicePoint servicePoint, IPEndPoint remoteEP, int retryCount)
        {
            IPAddress localIP;
            if (Listener.IP != null)
            {
                localIP = Listener.IP;
            }
            else if (!IPAddress.TryParse("0.0.0.0", out localIP))
            {
                return null;
            }
            if (remoteEP.AddressFamily != localIP.AddressFamily) return null;
            return new IPEndPoint(localIP, 0);
        }

        public static WebClient CreateWebClient() { return new CustomWebClient(); }
        public const string BaseURL = "http://cdn.classicube.net/client/latest/";
        public static string exe;
        public static void PerformUpdate(string[] args)
        {
            try
            {
                try
                {
                    DeleteFiles("CC.update", "CC_prev.exe");
                }
                catch(Exception e)
                {
                    Console.WriteLine("Error deleting files:");
                    Console.WriteLine(e.ToString());
                    Console.ReadKey(false);
                    return;
                }
                    try
                    {
                        WebClient client = HttpUtil.CreateWebClient();
                        AtomicIO.TryMove("ClassiCube.exe", "CC_prev.exe");
                        client.DownloadFile(exe, "CC.update");
                    }
                    catch (Exception x) 
                    {
                        Console.WriteLine("Error downloading update:");
                        Console.WriteLine(x.ToString());
                        Console.ReadKey(false);
                        return;
                    }
                AtomicIO.TryMove("CC.update", "ClassiCube.exe");
                DeleteFiles("CC_prev.exe");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error performing update:");
                Console.WriteLine(ex.ToString());
                Console.ReadKey(false);
                return;
            }
        }
        static void DeleteFiles(params string[] paths)
        {
            foreach (string path in paths) { AtomicIO.TryDelete(path); }
        }
    }
}
