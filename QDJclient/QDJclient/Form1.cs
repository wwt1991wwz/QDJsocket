using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Threading;

using System.Net;
using System.Net.Sockets;

namespace QDJclient
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            TextBox.CheckForIllegalCrossThreadCalls = false;//关闭跨线程修改控件检查
        }
        public Socket clientSocket;
        private string recvStr = "";
        private byte[] recvBytes = new byte[1024];
        private int bytes;
        private Thread startConnect;
        private Thread startRecieve;
        private void button1_Click(object sender, EventArgs e)
        {
            startConnect = new Thread(connect);
            startConnect.IsBackground = true;
            startConnect.Start();

            startRecieve = new Thread(waitForMessage);
            startRecieve.IsBackground = true;
            startRecieve.Start();

        }
        public void connect()
        {
            try
            {
                int port = 2000;
                string host = "127.0.0.1";
                //创建终结点EndPoint
                IPAddress ip = IPAddress.Parse(host);
                IPEndPoint ipe = new IPEndPoint(ip, port);   //把ip和端口转化为IPEndPoint的实例

                //创建Socket并连接到服务器
                clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);   //  创建Socket

                clientSocket.Connect(ipe); //连接到服务器
            }
            catch (Exception exp)
            {
                textBox1.AppendText(exp.Message);
            }
        }
    


        //发送消息
        private void button3_Click(object sender, EventArgs e)
        {
            try
            {
                string sendStr = textBox2.Text;
                byte[] bs = Encoding.ASCII.GetBytes(sendStr);
                clientSocket.Send(bs, bs.Length, 0);
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }
        //接收消息的方法
        public void waitForMessage()
        {
            while (true)
            {
                if (clientSocket != null)
                {
                    bytes = clientSocket.Receive(recvBytes, recvBytes.Length, 0);    //从服务器端接受返回信息
                    recvStr = Encoding.ASCII.GetString(recvBytes, 0, bytes);
                    textBox1.AppendText(recvStr + "\r\n");
                }
            }
        }
    }
}
