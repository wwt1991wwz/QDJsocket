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
using System.Collections;
namespace QDJserver
{
    
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            //TextBox.CheckForIllegalCrossThreadCalls = false;//关闭跨线程修改控件检查
        }
        // Thread signal.     
        public static ManualResetEvent allDone = new ManualResetEvent(false);
        public static ManualResetEvent rcv = new ManualResetEvent(false);
        public Socket server;//和客户端建立连接后系统返回的socket
        public Socket skt;
        public Socket client;
        public EndPoint remote;
        Thread startServer;//启动socket监听的线程
        Thread recMessage;//接收消息的线程
        public Hashtable _sessionTable;//存放每次建立连接的套接字

        public delegate void MyInvoke(string str);//控件中显示消息的委托
        private string recvStr = "";
        private byte[] recvBytes = new byte[1024];
        private int bytes;
        public Thread myThread;
        //启动监听服务

        public void listen()
        {
            int port = 11000;
            string host = "127.0.0.1";
            //创建终结点
            IPAddress ip = IPAddress.Parse(host);
            IPEndPoint ipe = new IPEndPoint(ip, port);
            _sessionTable = new Hashtable(100);
            //创建socket并开始监听

            skt = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);//创建一个Socket对象，如果用UDP协议，则要用SocketTyype.Dgram类型的套接字
            //允许SOCKET被绑定在已使用的地址上。
            skt.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            try
            {
               
                skt.Bind(ipe);//绑定ip和端口
                skt.Listen(5);
                showClientMsg("开始监听，等待客户端连接\r\n");
                while (true)
                {
                    // Set the event to nonsignaled state.     
                    allDone.Reset();
                    //开始接受连接，异步。
                    skt.BeginAccept(new AsyncCallback(OnConnectRequest), skt);
                    // Wait until a connection is made before continuing.     
                    allDone.WaitOne();
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("连接错误：" + e.Message);

            }
        }
        //当有客户端连接时的处理
        public void OnConnectRequest(IAsyncResult ar)
        {
            // Signal the main thread to continue.     
            allDone.Set();
            //还原传入的原始套接字
            server = (Socket)ar.AsyncState;
            //初始化一个SOCKET，用于其它客户端的连接
            client = server.EndAccept(ar);
            //将要发送给连接上来的客户端的提示字符串
            DateTimeOffset now = DateTimeOffset.Now;
            string strDateLine = "welcome to server";
            Byte[] byteDateLine = System.Text.Encoding.UTF8.GetBytes(strDateLine);

            //将提示信息发送给客户端
            client.Send(byteDateLine, byteDateLine.Length, 0);
            //并在服务端显示连接信息。
            remote = client.RemoteEndPoint;
            showClientMsg(client.RemoteEndPoint.ToString() + "连接成功。" + now.ToString("G") + "\r\n");
            userListAdd(client.RemoteEndPoint.ToString());
            
            //把连接成功的客户端的SOCKET实例放入哈希表
            _sessionTable.Add(client.RemoteEndPoint.ToString(), client);
            
            //等待新的客户端连接
            //server.BeginAccept(new AsyncCallback(OnConnectRequest), server);
            Thread receiveThread = new Thread(receive);
            receiveThread.Start(client);

            

        }
        //异步接收数据方法
        public void receive(object clientSocket)
        {
            Socket myClientSocket = (Socket)clientSocket;
            while (true)
            {
                //rcv.Reset();
                if (myClientSocket != null)
                {
                    // Create the state object.
                    StateObject state = new StateObject();
                    state.workSocket = myClientSocket;
                    myClientSocket.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(receiveCallback), state);
                }
                //rcv.WaitOne();
            }
        }
        //回调接收数据的方法    
        public  void receiveCallback(IAsyncResult ar)
        {

            //rcv.Set();
            String content = String.Empty;
            // Retrieve the state object and the handler socket     
            // from the asynchronous state object.     
            StateObject state = (StateObject)ar.AsyncState;
            Socket handler = state.workSocket;
            // Read data from the client socket.     
            int bytesRead = handler.EndReceive(ar);
            if (bytesRead > 0)
            {
                // There might be more data, so store the data received so far.     
                state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));
                // Check for end-of-file tag. If it is not there, read     
                // more data.     
                content = state.sb.ToString();
                showClientMsg(content);
                //还可以使用 if(client.Available<=0) 来判断接收缓冲区中已经空了。
                //或者使用client.DataAvailable
                //if (content.IndexOf("<EOF>") == -1)
                //if (client.Available <= 0)
                //{
                //    // All the data has been read from the     
                //    // client. Display it on the console.     
                //    //Console.WriteLine("Read {0} bytes from socket. \n Data : {1}", content.Length, content);
                //    //在控件中将接收到的消息显示出来   
                //    showClientMsg(content);
                //}
                //else
                //{
                //    // Not all data received. Get more.     
                //    handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(receiveCallback), state);
                //}
            }
        }
        //用来往richtextbox框中显示消息
        public void showClientMsg(string msg)
        {
            //在线程里以安全方式调用控件
            if (textBox1.InvokeRequired)
            {
                MyInvoke _myinvoke = new MyInvoke(showClientMsg);
                textBox1.Invoke(_myinvoke, new object[] { msg });
            }
            else
            {
                textBox1.AppendText(msg);
            }
        }
        //用来往listbox里面添加用户
        public void userListAdd(string msg)
        {
            //在线程里以安全方式调用控件
            if (userList.InvokeRequired)
            {
                MyInvoke _myinvoke = new MyInvoke(userListAdd);
                userList.Invoke(_myinvoke, new object[] { msg });
            }
            else
            {
                userList.Items.Add(msg);
            }
        }
        //用来往listbox里面添加删除用户
        //public void userListRemove(string msg)
        //{
        //    //在线程里以安全方式调用控件
        //    if (userList.InvokeRequired)
        //    {
        //        MyInvoke _myinvoke = new MyInvoke(userListRemove);
        //        userList.Invoke(_myinvoke, new object[] { msg });
        //    }
        //    else
        //    {
        //        userList.Items.Remove(msg);
                
        //    }
        //}
        
        //发送消息
        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                string sendStr = textBox2.Text;
                //在已连接的列表里取出用户
                string endPointTostring = this.userList.CheckedItems.ToString();
                //如果选取多个用户，就依次发送消息
                for (int i = 0; i < userList.CheckedItems.Count; i++)
                {
                    if (userList.CheckedItems[i].Checked)
                    {
                        endPointTostring = userList.CheckedItems[i].SubItems[0].Text;
                        SendMsg(endPointTostring, sendStr);
                    }
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }

        //以下实现向指定客户端发送消息
        public void SendMsg(string endPonitToString,string msg)
        {
            
            Byte[] sendData = Encoding.UTF8.GetBytes(msg);
            //在哈希表里取出指定的套接字
            Socket temp = (Socket)_sessionTable[endPonitToString];
            temp.Send(sendData);
            //client.SendTo(sendData, temp);
            textBox2.Text = "";


        }

        private void Form1_Load(object sender, EventArgs e)
        {
            
            //监听的线程
            ThreadStart myThreadDelegateListen = new ThreadStart(listen);
            startServer = new Thread(myThreadDelegateListen);
            startServer.Start();

            
            //接收消息的线程
            //ThreadStart myThreaddelegateRecieve = new ThreadStart(recieve);
            //recMessage = new Thread(myThreaddelegateRecieve);
            //recMessage.Start();
        }

        
    }
    public class StateObject
    {
        // Client socket.     
        public Socket workSocket = null;
        // Size of receive buffer.     
        public const int BufferSize = 1024;
        // Receive buffer.     
        public byte[] buffer = new byte[BufferSize];
        // Received data string.     
        public StringBuilder sb = new StringBuilder();
    }
}
