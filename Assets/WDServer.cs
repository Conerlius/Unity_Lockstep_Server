using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using Unity.Collections;

namespace WDLockStep
{
    public class WDServer : MonoBehaviour
    {
        //将远程连接的客户端的IP地址和Socket存入集合中
        Dictionary<string, Socket> dicSocket = new Dictionary<string, Socket>();
        int port = 6000;
        string host = "127.0.0.1";
        Socket socket = null;
        //创建监听连接的线程
        Thread AcceptSocketThread;
        //接收客户端发送消息的线程
        Dictionary<string, Thread> threadReceives = new Dictionary<string, Thread>();
        Dictionary<string, Timer> threadSends = new Dictionary<string, Timer>();
        //用于通信的Socket
        Socket socketSend;
          //用于监听的SOCKET
          Socket socketWatch;
        int FrameIndex = 0;
        MoveData Tmd = null;
        private void Awake()
        {
            FrameIndex = 0;
            IPAddress ip = IPAddress.Parse(host);
            IPEndPoint ipe = new IPEndPoint(ip, port);

            socketWatch = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Udp);
            socketWatch.Bind(ipe);
            //开始监听:设置最大可以同时连接多少个请求
            socketWatch.Listen(10);

            AcceptSocketThread = new Thread(new ParameterizedThreadStart(StartListen));
            AcceptSocketThread.IsBackground = true;
            AcceptSocketThread.Start(socketWatch);
        }
        private void StartListen(object obj) {
            Socket socketWatch = obj as Socket;
            while (true) {
                //等待客户端的连接，并且创建一个用于通信的Socket
                socketSend = socketWatch.Accept();
                //获取远程主机的ip地址和端口号
                string strIp = socketSend.RemoteEndPoint.ToString();
                dicSocket.Add(strIp, socketSend);
                //this.cmb_Socket.Invoke(setCmbCallBack, strIp);
                string strMsg = "远程主机：" + socketSend.RemoteEndPoint + "连接成功";
                Debug.Log(strMsg);
                //定义接收客户端消息的线程
                Thread threadReceive = new Thread(new ParameterizedThreadStart(Receive));
                threadReceive.IsBackground = true;
                threadReceive.Start(socketSend);
                threadReceives.Add(strIp, threadReceive);
            }
        }
        /// <summary>
        /// 服务器端不停的接收客户端发送的消息
        /// </summary>
        /// <param name="obj"></param>
        private void Receive(object obj)
        {
            Socket socketSend = obj as Socket;
            while (true)
            {
                //客户端连接成功后，服务器接收客户端发送的消息
                byte[] buffer = new byte[2048];
                //实际接收到的有效字节数
                int count = socketSend.Receive(buffer);
                if (count == 0)//count 表示客户端关闭，要退出循环
                {
                    break;
                }
                else
                {
                    string str = Encoding.ASCII.GetString(buffer, 0, count);
                    string ip = socketSend.RemoteEndPoint.ToString();
                    Debug.Log(str);
                    if (str.StartsWith("Begin=>"))
                    {
                        string sendStr = "Begin";
                        byte[] sendBytes = Encoding.ASCII.GetBytes(sendStr);
                        dicSocket[ip].Send(sendBytes);
                        //启动定时
                        //定义接收客户端消息的线程
                        TimerCallback timerDelegate = new TimerCallback(SendInvoke);
                        Timer timer = new Timer(timerDelegate, socketSend, 100, 100);
                        threadSends.Add(ip, timer);
                    }
                    else if (str.StartsWith("Move=>"))
                    {
                        string tmp1 = str.Replace("Move=>", "");
                        string[] tmps = tmp1.Split(';');
                        lock (Tmd)
                        {
                            Tmd = MoveData.Parse(tmps);
                        }
                        //string sendStr = "frame=>" + str;
                        //byte[] sendBytes = Encoding.ASCII.GetBytes(sendStr);
                        //dicSocket[ip].Send(sendBytes);
                    }
                    else if (str.StartsWith("Add=>")) {
                        byte[] sendBytes = Encoding.ASCII.GetBytes(str);
                        dicSocket[ip].Send(sendBytes);
                    }
                    else
                    {
                        string tmp1 = str.Replace("Stop=>", "");
                        Tmd = null;
                    }
                }
            }
        }
        /// <summary>
        /// 服务器端不停的接收客户端发送的消息
        /// </summary>
        /// <param name="obj"></param>
        private void SendInvoke(object obj)
        {
            FrameIndex++;
            Socket socketSend = obj as Socket;
            string ip = socketSend.RemoteEndPoint.ToString();
            if (Tmd != null)
            {
                string sendStr = "Move=>" + FrameIndex + ';';
                lock (Tmd)
                {
                    sendStr += Tmd.ToString();
                }
                byte[] sendBytes = Encoding.ASCII.GetBytes(sendStr);
                dicSocket[ip].Send(sendBytes);
            }
            else {
                string sendStr = "Stop=>" + FrameIndex;
                byte[] sendBytes = Encoding.ASCII.GetBytes(sendStr);
                dicSocket[ip].Send(sendBytes);
            }
        }
        private void OnDestroy()
        {
            socketWatch.Close();
            socketSend.Close();
            //终止线程
            AcceptSocketThread.Abort();
            foreach (var t in threadReceives) {
                t.Value.Abort();
            }
            foreach (var t in threadSends)
            {
                t.Value.Dispose();
            }
        }
    }
}