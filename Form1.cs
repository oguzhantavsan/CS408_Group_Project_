using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Security;
namespace server
{
    public partial class Form1 : Form
    {
        // static string filePath = @"C:\Users\asus\Desktop\user_db.txt";
        static string filePath = @"C:\Users\Oğuzhan Berberoğlu\Desktop\user_db.txt";
        string[] ClientList = System.IO.File.ReadAllLines(filePath);
        List<string> added = new List<string>();
        Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        List<Socket> clientSockets = new List<Socket>();
        //string[] NameList=new string[300];

        bool terminating = false;
        bool listening = false;

        string nick="";
        public Form1()
        {
            Control.CheckForIllegalCrossThreadCalls = false;
            this.FormClosing += new FormClosingEventHandler(Form1_FormClosing);
            InitializeComponent();
        }

        private void button_listen_Click(object sender, EventArgs e)
        {
            int serverPort;

            if(Int32.TryParse(textBox_port.Text, out serverPort))
            {
                IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, serverPort);
                serverSocket.Bind(endPoint);
                serverSocket.Listen(100);

                listening = true;
                button_listen.Enabled = false;

                Thread acceptThread = new Thread(Accept);
                acceptThread.Start();

                logs.AppendText("Started listening on port: " + serverPort + "\n");

            }
            else
            {
                logs.AppendText("Please check port number \n");
            }
        }

        private void Accept()
        {
            while(listening)
            {
                try
                {
                    Socket newClient = serverSocket.Accept();
                    Byte[] buffer = new Byte[128];
                    newClient.Receive(buffer);
                    nick = Encoding.ASCII.GetString(buffer);   // Encodes the name of the client and eliminates the
                    nick = nick.Substring(0, nick.IndexOf("\0"));
                    int count = 0;
                    foreach (string item in ClientList)
                    {
                        if (item == nick)
                        {
                            //Socket thisClient = clientSockets[clientSockets.Count() - 1];
                            if (!clientSockets.Contains(newClient) && !added.Contains(nick))
                            {
                                //Socket newClient = serverSocket.Accept();
                                count++;
                                clientSockets.Add(newClient);
                                added.Add(nick);
                                logs.AppendText(nick + " has connected to server\n");
                                buffer = Encoding.Default.GetBytes(nick + " has connected to server\n");
                                newClient.Send(buffer);
                                Thread receiveThread = new Thread(Receive);
                                receiveThread.Start();
                            }
                        }
                    }

                    if (count == 0)
                    {
                        logs.AppendText(nick + " can not connected to server\n");
                        buffer = Encoding.Default.GetBytes(nick + " can not connected to server\n");
                        newClient.Send(buffer);
                        newClient.Dispose();
                        newClient.Disconnect(false);
                        //newClient.Shutdown();
                        newClient.Close();
                        //clientSockets.Remove(newClient);
                    }
                }
                catch
                {
                    if (terminating)
                    {
                        listening = false;
                    }
                    else
                    {
                        logs.AppendText("The socket stopped working.\n");
                    }

                }
            }
        }
        private void Receive()
        {
            Socket thisClient = clientSockets[clientSockets.Count() - 1];
            bool connected = true;
           
            while(connected && !terminating)
            {
                try
                {
      
                    Byte[] buffer = new Byte[128];
                    thisClient.Receive(buffer);
                    string incomingMessage = Encoding.Default.GetString(buffer);
                    incomingMessage = incomingMessage.Substring(0, incomingMessage.IndexOf("\0"));
                    foreach (Socket client in clientSockets) // Let all clients know the round number.
                    {
                        try
                        {
                            if (incomingMessage!="")
                            {
                                buffer = Encoding.Default.GetBytes(incomingMessage + "\n");
                                if(client != thisClient)
                                {
                                    client.Send(buffer);
                                }
                            }
                        }

                        catch
                        {
                            logs.AppendText("There is a problem! Check the connection...");
                            terminating = true;
                            serverSocket.Close();
                        }
                    }
                }
                catch
                {
                    if(!terminating)
                    {
                        int index = clientSockets.IndexOf(thisClient);
                        clientSockets.Remove(thisClient);
                        logs.AppendText(added[index] + " client has disconnected\n");
                        added.RemoveAt(index);
                    }
                    thisClient.Close();
                    clientSockets.Remove(thisClient);
                    connected = false;
                }
            }
        }

        private void Form1_FormClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            logs.AppendText("Server has been terminated");
            listening = false;
            terminating = true;
            Environment.Exit(0);
        }

       
    }
}
