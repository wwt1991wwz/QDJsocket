using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
//添加Socket类
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace QDJserver
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            TextBox.CheckForIllegalCrossThreadCalls = false;//关闭跨线程修改控件检查
        }
        public Socket clientSocket;//和客户端建立连接后系统返回的socket
        Thread startServer;//启动socket监听的线程
        Thread recMessage;//接收消息的线程
        private string recvStr = "";
        private byte[] recvBytes = new byte[1024];
        private int bytes;
        //启动监听服务

        public void listen()
        {
            int port = 2000;
            string host = "127.0.0.1";
            try
            {
                //创建终结点
                IPAddress ip = IPAddress.Parse(host);
                IPEndPoint ipe = new IPEndPoint(ip, port);

                //创建socket并开始监听

                Socket skt = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);//创建一个Socket对象，如果用UDP协议，则要用SocketTyype.Dgram类型的套接字
                skt.Bind(ipe);//绑定ip和端口
                skt.Listen(5);

                textBox1.AppendText("开始监听，等待客户端连接\r\n");

                //接受到Client连接，为此连接建立新的Socket，并接受消息
                clientSocket = skt.Accept();
                string str = clientSocket.RemoteEndPoint.ToString();
                textBox1.AppendText(str + "已连接\r\n");


            }
            catch (Exception e)
            {
                MessageBox.Show("连接错误：" + e.Message);

            }
        }

        public void waitForMessage()
        {
            while (true)
            {
                if (clientSocket != null)
                {
                    bytes = clientSocket.Receive(recvBytes, recvBytes.Length, 0);
                    if (bytes != 0)
                    {
                        recvStr = Encoding.ASCII.GetString(recvBytes, 0, bytes);
                        textBox1.AppendText(recvStr + "\r\n");
                    }
                }
                
            }
        }
        //发送消息
        private void button2_Click(object sender, EventArgs e)
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

        private void Form1_Load(object sender, EventArgs e)
        {
            //启动监听服务的线程
            startServer = new Thread(listen);
            startServer.IsBackground = true;
            startServer.Start();

            //启动接收消息的线程
            recMessage = new Thread(waitForMessage);
            recMessage.IsBackground = true;
            recMessage.Start(); 
        }
    }
}
