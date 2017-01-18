using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Collections;
using Google.ProtocolBuffers;

public class SocketGenerate : MonoBehaviour {

    private SocketHelper socket_instance = SocketHelper.GetInstance();

    //player
    private GameObject _player;
    private Player player;
    private Enemy enemy;

    //ip
    public string ip;
    
    // Use this for initialization
    void Start () {
        //player
        _player = GameObject.FindWithTag("player");
        if (!_player)
        {
            Debug.LogError("no player available");
        }
        player = _player.GetComponent<Player>();
        //enemy
        _player = GameObject.FindWithTag("enemy");
        if (!_player)
        {
            Debug.LogError("no enemy available");
        }
        enemy = _player.GetComponent<Enemy>();
        //get ip
        ip = GetInternalIP();
        if (ip == "")
        {
            Debug.LogError("invalid ip,please check!");
        }
        else
        {
            Debug.Log("ip:" + ip);
        }
        //test for protobuf-csharp-port(google)
        //test for BytesToClient_Frame and Client_FrameToBytes
        /*
        CodeBattle.Client_Frame.Builder clientframeBuilder = new CodeBattle.Client_Frame.Builder();
        clientframeBuilder.Ip = ip;
        clientframeBuilder.Direction = 1;
        CodeBattle.Client_Frame tmp = clientframeBuilder.BuildPartial();
        print("init:\n" + tmp);
        byte[] data = Client_FrameToBytes(tmp);
        CodeBattle.Client_Frame tmp2 = BytesToClient_Frame(data);
        print("decoded:\n" + tmp2);
        //test for Server_FrameToBytes and BytesToServer_Frame
        CodeBattle.Server_Frame.Builder serverframeBuilder = new CodeBattle.Server_Frame.Builder();
        serverframeBuilder.Empty = true;
        serverframeBuilder.Frameseq = 20;
        serverframeBuilder.Preframe = tmp;
        serverframeBuilder.Laterframe = tmp2;
        CodeBattle.Server_Frame servertmp = serverframeBuilder.BuildPartial();
        print("init\n" + servertmp);
        byte[] data1 = Server_FrameToBytes(servertmp);
        CodeBattle.Server_Frame servertmp2 = BytesToServer_Frame(data1);
        print("decoded:\n" + servertmp2);
        */
    }

    private string GetInternalIP()
    {
        IPHostEntry host;
        string localIP = "";
        host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (IPAddress ip in host.AddressList)
        {
            if (ip.AddressFamily.ToString() == "InterNetwork")
            {
                localIP = ip.ToString();
                if (localIP != "127.0.0.1")
                {
                    break;
                }
            }
        }
        return localIP;
    }

    // Update is called once per frame
    void Update () {
        //receive frame
        if (socket_instance.is_connected)
        {
            //Receive_frame();
            // Wating for Param#1 Microseconds to check is there any data send from server.
            // 1 second == 1000 Millisecond == 1000 * 1000 Microseconds
            if (socket_instance.socket.Poll(10000, SelectMode.SelectRead))
            {
                try
                {
                    Receive_frame();
                }
                catch (Exception e)
                {
                    print(e);
                    socket_instance.is_connected = false;
                    return;
                }
            }
        }
    }

    //Client_Frame To Bytes
    public byte[] Client_FrameToBytes(CodeBattle.Client_Frame clientframe)
    {
        //...
        byte[] buffer = new byte[clientframe.SerializedSize];
        CodedOutputStream stream = CodedOutputStream.CreateInstance(buffer);
        clientframe.WriteTo(stream);
        byte[] binary = new byte[buffer.Length + 4];
        int len = IPAddress.HostToNetworkOrder(buffer.Length);
        byte[] lenBytes = BitConverter.GetBytes(len);
        lenBytes.CopyTo(binary, 0);
        buffer.CopyTo(binary, 4);
        return binary;
    }
    //Server_Frame To Bytes
    public byte[] Server_FrameToBytes(CodeBattle.Server_Frame serverframe)
    {
        //...
        byte[] buffer = new byte[serverframe.SerializedSize];
        CodedOutputStream stream = CodedOutputStream.CreateInstance(buffer);
        serverframe.WriteTo(stream);
        byte[] binary = new byte[buffer.Length + 4];
        int len = IPAddress.HostToNetworkOrder(buffer.Length);
        byte[] lenBytes = BitConverter.GetBytes(len);
        lenBytes.CopyTo(binary, 0);
        buffer.CopyTo(binary, 4);
        return binary;
    }
    //Bytes To Client_Frame
    public CodeBattle.Client_Frame BytesToClient_Frame(byte[] bytes)
    {
        CodedInputStream stream = CodedInputStream.CreateInstance(bytes);
        CodeBattle.Client_Frame clientframe = CodeBattle.Client_Frame.ParseFrom(stream);
        return clientframe;
    }
    //Bytes To Server_Frame
    public CodeBattle.Server_Frame BytesToServer_Frame(byte[] bytes)
    {
        //...
        CodedInputStream stream = CodedInputStream.CreateInstance(bytes);
        CodeBattle.Server_Frame serverframe = CodeBattle.Server_Frame.ParseFrom(stream);
        return serverframe;
    }
    
    
    private void Receive_frame()
    {
        byte[] bytes = socket_instance.ReceiveMessage();
        CodeBattle.Server_Frame serverframe = BytesToServer_Frame(bytes);
        if (serverframe.Empty)
        {
            //Debug.Log("empty frame received!");
            //print("serverframe:\n"+serverframe);
        }
        else if (serverframe.Frameseq > 0 && !serverframe.Empty)
        {
            Debug.Log("frame receive:" + serverframe);
            if (serverframe.Preframe.Ip != "")
            {
                if (serverframe.Preframe.Ip == ip)
                {
                    switch (serverframe.Preframe.Direction)
                    {
                        case 1:
                            //Debug.Log("left Button click receive");
                            print("player left Button click receive:\n" + serverframe.Preframe);
                            player.Set_Player_MoveState(Player.MoveState.LEFT);
                            break;
                        case 2:
                            //Debug.Log("right Button click receive");
                            print("player right Button click receive:\n" + serverframe.Preframe);
                            player.Set_Player_MoveState(Player.MoveState.RIGHT);
                            break;
                        case 3:
                            //Debug.Log("up Button click receive");
                            print("player up Button click receive:\n" + serverframe.Preframe);
                            player.Set_Player_MoveState(Player.MoveState.UP);
                            break;
                        case 4:
                            //Debug.Log("attack Button click receive");
                            print("player attack Button click receive:\n" + serverframe.Preframe);
                            player.Set_Player_OpeState(Player.OpeState.ATTACK);
                            break;
                        default:
                            Debug.LogError("invalid direction:" + serverframe.Preframe.Direction);
                            break;
                    }
                }
                else
                {
                    switch (serverframe.Preframe.Direction)
                    {
                        case 1:
                            print("enemy right Button click receive:\n" + serverframe.Preframe);
                            enemy.Set_Player_MoveState(Enemy.MoveState.RIGHT);
                            break;
                        case 2:
                            print("enemy left Button click receive:\n" + serverframe.Preframe);
                            enemy.Set_Player_MoveState(Enemy.MoveState.LEFT);
                            break;
                        case 3:
                            print("enemy up Button click receive:\n" + serverframe.Preframe);
                            enemy.Set_Player_MoveState(Enemy.MoveState.UP);
                            break;
                        case 4:
                            print("enemy attack Button click receive:\n" + serverframe.Preframe);
                            enemy.Set_Player_OpeState(Enemy.OpeState.ATTACK);
                            break;
                        default:
                            Debug.LogError("invalid direction:" + serverframe.Preframe.Direction);
                            break;
                    }
                }
            }
            if (serverframe.Laterframe.Ip != "" )
            {
                if (serverframe.Laterframe.Ip == ip)
                {
                    switch (serverframe.Laterframe.Direction)
                    {
                        case 1:
                            //Debug.Log("left Button click receive");
                            print("player left Button click receive:\n" + serverframe.Laterframe);
                            player.Set_Player_MoveState(Player.MoveState.LEFT);
                            break;
                        case 2:
                            //Debug.Log("right Button click receive");
                            print("player right Button click receive:\n" + serverframe.Laterframe);
                            player.Set_Player_MoveState(Player.MoveState.RIGHT);
                            break;
                        case 3:
                            //Debug.Log("up Button click receive");
                            print("player up Button click receive:\n" + serverframe.Laterframe);
                            player.Set_Player_MoveState(Player.MoveState.UP);
                            break;
                        case 4:
                            //Debug.Log("attack Button click receive");
                            print("player attack Button click receive:\n" + serverframe.Laterframe);
                            player.Set_Player_OpeState(Player.OpeState.ATTACK);
                            break;
                        default:
                            Debug.LogError("invalid direction:" + serverframe.Laterframe.Direction);
                            break;
                    }
                }
                else
                {
                    switch (serverframe.Laterframe.Direction)
                    {
                        case 1:
                            print("enemy right Button click receive:\n" + serverframe.Laterframe);
                            enemy.Set_Player_MoveState(Enemy.MoveState.RIGHT);
                            break;
                        case 2:
                            print("enemy left Button click receive:\n" + serverframe.Laterframe);
                            enemy.Set_Player_MoveState(Enemy.MoveState.LEFT);
                            break;
                        case 3:
                            print("enemy up Button click receive:\n" + serverframe.Laterframe);
                            enemy.Set_Player_MoveState(Enemy.MoveState.UP);
                            break;
                        case 4:
                            print("enemy attack Button click receive:\n" + serverframe.Laterframe);
                            enemy.Set_Player_OpeState(Enemy.OpeState.ATTACK);
                            break;
                        default:
                            Debug.LogError("invalid direction:" + serverframe.Laterframe.Direction);
                            break;
                    }
                }
            }
            
            /*
            else
            {
                Debug.LogError("invalid ip received");
                Debug.LogError("local ip:" + ip);
                Debug.LogError("preframe ip:" + serverframe.Preframe.Ip);
                Debug.LogError("laterframe ip:" + serverframe.Laterframe.Ip);
            }
            */
        }
        else
        {
            Debug.LogError("invalid frame received" + serverframe);
        }
    }

    public void Send_frame(byte[] bytes)
    {
        socket_instance.SendMessage(bytes);
    }
    
}
