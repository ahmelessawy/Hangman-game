﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Forms;

namespace Server
{
    public partial class Controller : Form
    {
        private Dictionary<int, Clients> clients;
        private static Dictionary<int, string> ClientsData;
        private Thread thread1, thread2, thread3;

        public Controller()
        {
            CheckForIllegalCrossThreadCalls = false;
            InitializeComponent();
            List_Connected.Items.Clear();
            List_Disonnected.Items.Clear();
            List_ClientMsgs.Items.Clear();
            clients = new Dictionary<int, Clients>();
            ClientsData = new Dictionary<int, string>();
        }

        private void ClientCreator()
        {
            TcpListener listener = new TcpListener(new IPAddress(new byte[] { 127, 0, 0, 1 }), 5000);
            listener.Start();
            while (true)
            {
                Socket socket = listener.AcceptSocket();
                if (socket.Connected)
                {
                    Clients temp_client = new Clients(socket);
                    clients.Add(temp_client.Endpoint, temp_client);
                    List_Connected.Items.Add(temp_client.Name);
                }
            }
        }

        private void DataReader()
        {
            while (true)
            {
                foreach (int port in ClientsData.Keys.ToList())
                {
                    List_ClientMsgs.Items.Add(clients[port].Name + ": " + ClientsData[port]);
                    ClientsData.Remove(port);
                }
            }
        }

        private void Check_disconnected()
        {
            while (true)
            {
                foreach (int index in clients.Keys.ToList())
                {
                    if (index != null && !clients[index].isConnected())
                    {
                        List_Disonnected.Items.Add(index);
                        List_Connected.Items.RemoveAt(List_Connected.FindStringExact(index.ToString()));
                        clients[index] = null;
                        clients.Remove(index);
                    }
                }
            }
        }

        public static ReaderInfo Strings
        {
            set
            {
                if (ClientsData.ContainsKey(value.port))
                    ClientsData[value.port] = value.reader;
                else
                    ClientsData.Add(value.port, value.reader);
            }
        }

        private void Button_Send_Click(object sender, EventArgs e)
        {
            foreach (string item in List_Connected.SelectedItems)
            {
                int index = int.Parse(item);
                clients[index].bWriter = textBox1.Text;
            }
        }

        private void Button_Terminate_Click(object sender, EventArgs e)
        {
            /*foreach (string item in List_Connected.SelectedItems)
            {
                int index = int.Parse(item);
                List_Disonnected.Items.Add(index);
                List_Connected.Items.RemoveAt(List_Connected.FindStringExact(index.ToString()));
                clients[index] = null;
                clients.Remove(index);
            }*/
        }

        private void Button_Exit_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void Button_Start_Click(object sender, EventArgs e)
        {
            thread1 = new Thread(ClientCreator);
            thread2 = new Thread(DataReader);
            thread3 = new Thread(Check_disconnected);
            thread1.Start();
            thread2.Start();
            thread3.Start();
            Button_Start.Enabled = false;
            Button_Stop.Enabled = true;
        }

        private void Button_Stop_Click(object sender, EventArgs e)
        {
            thread1.Abort();
            thread2.Abort();
            thread3.Abort();
            Button_Start.Enabled = true;
            Button_Stop.Enabled = false;
        }
    }

    public struct ReaderInfo
    {
        public int port;
        public string reader;
    }

    public class Clients
    {
        private Socket socket;
        private NetworkStream nStream;
        private BinaryReader breader;
        private BinaryWriter bwriter;
        private Thread thread;
        private int endpoint;
        private string name;
        public string bWriter { set { try { bwriter.Write(value); } catch (Exception) { Disconnect(); } } }
        public int Endpoint { get { return endpoint; } }
        public string Name { get { return name; } }

        public Clients(Socket socket)
        {
            this.socket = socket;
            endpoint = int.Parse(socket.RemoteEndPoint.ToString().Split(':')[1]);
            name += endpoint;
            nStream = new NetworkStream(socket);
            breader = new BinaryReader(nStream);
            bwriter = new BinaryWriter(nStream);
            thread = new Thread(new ThreadStart(DataRead));
            thread.Start();
        }

        private void DataRead()
        {
            while (true)
            {
                try
                {
                    ReaderInfo data = new ReaderInfo
                    {
                        port = endpoint,
                        reader = breader.ReadString()
                    };
                    Controller.Strings = data;
                }
                catch (Exception) { Disconnect(); }
            }
        }

        public bool isConnected()
        {
            bool connected = true;
            if (socket.Poll(1000, SelectMode.SelectRead) && socket.Available == 0)
                connected = false;
            return connected;
        }

        private void Disconnect()
        {
            thread.Abort();
            breader.Close();
            bwriter.Close();
            nStream.Close();
            socket.Shutdown(SocketShutdown.Both);
            socket.Close();
        }
    }
}