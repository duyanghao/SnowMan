using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

/* 
 *  
 *Socket客户端通信类 
 *  
 */
public class SocketHelper {
    //instance
    private static SocketHelper socketHelper = new SocketHelper();

    //socket
    public Socket socket;

    //ip and port(server)
    private string server_ip;
    private int server_port;

    //connected
    public bool is_connected;

    //单例模式  
    public static SocketHelper GetInstance()
    {
            return socketHelper;
    }

    private SocketHelper()
    {
        server_ip = "x.x.x.x";
        server_port = xxxx;

        //采用TCP方式连接  
        socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        //服务器IP地址  
        IPAddress address = IPAddress.Parse(server_ip);

        //服务器端口  
        IPEndPoint endpoint = new IPEndPoint(address, server_port);

        //异步连接,连接成功调用connectCallback方法  
        IAsyncResult result = socket.BeginConnect(endpoint, new AsyncCallback(ConnectCallback), socket);

        //这里做一个超时的监测，当连接超过50毫秒还没成功表示超时  
        bool success = result.AsyncWaitHandle.WaitOne(50, true);
        if (!success)
        {
            //超时  
            Closed();
            is_connected = false;
            Debug.Log("connect Time Out");
        }
        is_connected = true;
        /*
        else
        {
            //与socket建立连接成功，开启线程接受服务端数据。  
            Thread thread = new Thread(new ThreadStart(ReceiveSorket));
            thread.IsBackground = true;
            thread.Start();
        }
        */

    }

    private void ConnectCallback(IAsyncResult asyncConnect)
    {
        Debug.Log("connect success");
    }

    public byte[] ReceiveMessage()
    {
        //在这个线程中接受服务器返回的数据  
        /*while (true)
        {

            if (!socket.Connected)
            {
                //与服务器断开连接跳出循环  
                Debug.Log("Failed to clientSocket server.");
                socket.Close();
                break;
            }
            try
            {
                //接受数据保存至bytes当中  
                byte[] bytes = new byte[4096];
                //Receive方法中会一直等待服务端回发消息  
                //如果没有回发会一直在这里等着。  
                int i = socket.Receive(bytes);
                if (i <= 0)
                {
                    socket.Close();
                    break;
                }
                Debug.Log(System.Text.Encoding.Default.GetString(bytes));
            }
            catch (Exception e)
            {
                Debug.Log("Failed to clientSocket error." + e);
                socket.Close();
                break;
            }
        }
        */
        byte[] lenBytes = new byte[4];
        int rec = socket.Receive(lenBytes, 4, SocketFlags.None);
        if (rec == 0)
        {
            throw new Exception("Remote Closed the connection");
        }
        int len = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(lenBytes, 0));
        //Debug.Log("len:"+len);
        byte[] data = new byte[len];
        rec = socket.Receive(data, len, SocketFlags.None);
        if (rec == 0)
        {
            throw new Exception("Remote Closed the connection");
        }
        return data;
    }

    //关闭Socket  
    private void Closed()
    {
        if (socket != null && socket.Connected)
        {
            socket.Shutdown(SocketShutdown.Both);
            socket.Close();
        }
        else if (socket != null)
        {
            socket.Close();
        }
        socket = null;
    }

    //向服务端发送一条字符串  
    //一般不会发送字符串 应该是发送数据包  
    public void SendMessage(byte[] data)
    {
        /*
        byte[] msg = Encoding.UTF8.GetBytes(str);

        if (!socket.Connected)
        {
            socket.Close();
            return;
        }
        try
        {
            IAsyncResult asyncSend = socket.BeginSend(msg, 0, msg.Length, SocketFlags.None, new AsyncCallback(SendCallback), socket);
            bool success = asyncSend.AsyncWaitHandle.WaitOne(5000, true);
            if (!success)
            {
                socket.Close();
                Debug.Log("Failed to SendMessage server.");
            }
        }
        catch
        {
            Debug.Log("send message error");
        }
        */
        /*
        IAsyncResult asyncSend = socket.BeginSend(frame, 0, frame.Length, SocketFlags.None, new AsyncCallback(SendCallback), socket);
        bool success = asyncSend.AsyncWaitHandle.WaitOne(50, true);
        if (!success)
        {
            socket.Close();
            Debug.Log("Failed to SendMessage to server.");
        }*/
        socket.Send(data, data.Length, SocketFlags.None);
    }



    private void SendCallback(IAsyncResult asyncConnect)
    {
        Debug.Log("send success");
    }


}