using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.Threading;

namespace ProxyServerCs
{
    public partial class Form1 : Form
    {
        delegate void RefreshFormDelegate();
        Thread thread;
        int upTraf, downTraf;
        public Form1()
        {
            InitializeComponent();            
        }
        public void RefreshStats()
        {
            textBox3.Text = upTraf.ToString();
            textBox4.Text = downTraf.ToString();
        }
        private void TransferData(object arg)
        {
            ThreadProcParams tpp = arg as ThreadProcParams;
            int port = tpp.port;
            string host = tpp.host;
            RefreshFormDelegate refresh = new RefreshFormDelegate(RefreshStats);
            while (true)
            {
                TcpClient clientProxy = null;
                TcpListener listener = null;
                TcpClient clientReal = null;
                NetworkStream nsProxy = null;
                NetworkStream nsReal = null;
                try
                {
                    clientProxy = new TcpClient(host, 80);
                    listener = new TcpListener(IPAddress.Any, port);
                    nsProxy = clientProxy.GetStream();
                    listener.Start();
                    clientReal = listener.AcceptTcpClient();
                    nsReal = clientReal.GetStream();
                    int bufSize = 1024;
                    byte[] buf = new byte[bufSize];
                    int charsReaded = bufSize;
                    do
                    {
                        Thread.Sleep(100);
                        charsReaded = nsReal.Read(buf, 0, bufSize);
                        nsProxy.Write(buf, 0, charsReaded);
                        upTraf += charsReaded;
                    }
                    while (nsReal.DataAvailable);
                    charsReaded = bufSize;
                    do
                    {
                        Thread.Sleep(100);
                        charsReaded = nsProxy.Read(buf, 0, bufSize);
                        nsReal.Write(buf, 0, charsReaded);
                        downTraf += charsReaded;
                        this.Invoke(refresh);
                    }
                    while (nsProxy.DataAvailable);
                    listener.Stop();
                    clientProxy.Close();
                    clientReal.Close();
                }
                catch
                {
                    try
                    {
                        listener.Stop();
                        nsProxy.Close();
                        nsReal.Close();
                        clientProxy.Close();
                        clientReal.Close();
                    }
                    catch { }
                }
            }
        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            thread.Abort();
        }
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            thread.Abort();
            Close();
        }
        private void startListenerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int port;
            string host;
            try
            {
                port = int.Parse(textBox1.Text);
                host = textBox2.Text;
            }
            catch
            {
                return;
            }
            ThreadProcParams tpp = new ThreadProcParams(port, host);
            thread = new Thread(new ParameterizedThreadStart(TransferData));
            thread.Start(tpp);
        }
    }
    public class ThreadProcParams
    {
        public int port;
        public string host;
        public ThreadProcParams(int port, string host)
        {
            this.port = port;
            this.host = host;
        }
    }
}