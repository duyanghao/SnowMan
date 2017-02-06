using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Collections;
using Google.ProtocolBuffers;

public class SocketGenerate : MonoBehaviour {

    private SocketHelper socket_instance = SocketHelper.GetInstance();

    //player
    private GameObject _pvpplayer;
    private PvpPlayer pvpplayer;
    private PvpEnemy pvpenemy;

    //ip
    public static string ip;

    //animal
    public GameObject AnimalPrefab;
    //bird
    public GameObject BirdPrefab;
    //food
    public GameObject FoodPrefab;
    //generate
    //public GameObject GenerateObject;

    // Use this for initialization
    void Start () {
        //player
        _pvpplayer = GameObject.FindWithTag("player");
        if (!_pvpplayer)
        {
            Debug.LogError("no player available");
        }
        pvpplayer = _pvpplayer.GetComponent<PvpPlayer>();
        //enemy
        _pvpplayer = GameObject.FindWithTag("enemy");
        if (!_pvpplayer)
        {
            Debug.LogError("no enemy available");
        }
        pvpenemy = _pvpplayer.GetComponent<PvpEnemy>();
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
            if (serverframe.Preframe.Died || serverframe.Laterframe.Died)
            {
                //do not generate object
                //GenerateObject.SetActive(false);
                //do something about finish the game
                if (serverframe.Preframe.Died)
                {
                    if (ip == serverframe.Preframe.Ip)
                    {
                        //show lose panel
                        pvpplayer.Die();
                        pvpplayer.Set_died(true);
                    }
                    else
                    {
                        //show success panel
                        pvpenemy.Die();
                        pvpenemy.Set_died(true);
                    }
                }
                else
                {
                    if (ip == serverframe.Laterframe.Ip)
                    {
                        //show lose panel
                        pvpplayer.Die();
                        pvpplayer.Set_died(true);
                    }
                    else
                    {
                        //show success panel
                        pvpenemy.Die();
                        pvpenemy.Set_died(true);
                    }
                }
                return;
            }
            //the game is not over...
            if (serverframe.Preframe.Ip != "")
            {
                if (serverframe.Preframe.Ip == ip)
                {   
                    //moved
                    if (serverframe.Preframe.Moved)
                    {
                        if (serverframe.Preframe.Direction.Left)
                        {
                            print("player left Button click receive:\n" + serverframe.Preframe);
                            pvpplayer.Set_Player_MoveState(PvpPlayer.MoveState.LEFT);
                        }   
                        else if (serverframe.Preframe.Direction.Right)
                        {
                            print("player right Button click receive:\n" + serverframe.Preframe);
                            pvpplayer.Set_Player_MoveState(PvpPlayer.MoveState.RIGHT);
                        }
                        else if (serverframe.Preframe.Direction.Up)
                        {
                            print("player up Button click receive:\n" + serverframe.Preframe);
                            pvpplayer.Set_Player_MoveState(PvpPlayer.MoveState.UP);
                        }
                        else
                        {
                            Debug.LogError("invalid direction:" + serverframe.Preframe.Direction);
                        }
                    }
                    //player snow created
                    if (serverframe.Preframe.Snow.Isgenerated)
                    {
                        //attack
                        print("player-side create snow:\n" + serverframe.Preframe);
                        //pvpplayer.Set_Player_OpeState(Player.OpeState.ATTACK);
                        Vector3 start_pos = new Vector3(serverframe.Preframe.Snow.Pos.X, serverframe.Preframe.Snow.Pos.Y, serverframe.Preframe.Snow.Pos.Z);
                        pvpplayer.Throw_Snow(start_pos);
                    }
                    //hpchanged
                    if (serverframe.Preframe.Hpchanged)
                    {
                        if (serverframe.Preframe.Playerhp.Ischanged)
                        {
                            pvpplayer.TakeDamage(serverframe.Preframe.Playerhp.Changevalue);
                        }
                        if (serverframe.Preframe.Enemyhp.Ischanged)
                        {
                            pvpenemy.TakeDamage(serverframe.Preframe.Enemyhp.Changevalue);
                        }
                    }
                }
                else
                {
                    //enemy moved
                    if (serverframe.Preframe.Moved)
                    {
                        if (serverframe.Preframe.Direction.Left)
                        {
                            print("enemy right Button click receive:\n" + serverframe.Preframe);
                            pvpenemy.Set_Player_MoveState(PvpEnemy.MoveState.RIGHT);
                        }
                        else if (serverframe.Preframe.Direction.Right)
                        {
                            print("enemy left Button click receive:\n" + serverframe.Preframe);
                            pvpenemy.Set_Player_MoveState(PvpEnemy.MoveState.LEFT);
                        }
                        else if (serverframe.Preframe.Direction.Up)
                        {
                            print("enemy up Button click receive:\n" + serverframe.Preframe);
                            pvpenemy.Set_Player_MoveState(PvpEnemy.MoveState.UP);
                        }
                        else
                        {
                            Debug.LogError("invalid direction:" + serverframe.Preframe.Direction);
                        }
                    }
                    //enemy snow created
                    if (serverframe.Preframe.Snow.Isgenerated)
                    {
                        //attack
                        print("enemy-side create snow:\n" + serverframe.Preframe);
                        //pvpenemy.Set_Player_OpeState(Enemy.OpeState.ATTACK);
                        Vector3 start_pos = new Vector3(-1*serverframe.Preframe.Snow.Pos.X, serverframe.Preframe.Snow.Pos.Y, serverframe.Preframe.Snow.Pos.Z);
                        pvpenemy.Throw_Snow(start_pos);
                    }
                    //hpchanged
                    if (serverframe.Preframe.Hpchanged)
                    {
                        if (serverframe.Preframe.Playerhp.Ischanged)
                        {
                            pvpenemy.TakeDamage(serverframe.Preframe.Playerhp.Changevalue);
                        }
                        if (serverframe.Preframe.Enemyhp.Ischanged)
                        {
                            pvpplayer.TakeDamage(serverframe.Preframe.Enemyhp.Changevalue);
                        }
                    }
                }
            }
            if (serverframe.Laterframe.Ip != "")
            {
                if (serverframe.Laterframe.Ip == ip)
                {
                    //moved
                    if (serverframe.Laterframe.Moved)
                    {
                        if (serverframe.Laterframe.Direction.Left)
                        {
                            print("player left Button click receive:\n" + serverframe.Laterframe);
                            pvpplayer.Set_Player_MoveState(PvpPlayer.MoveState.LEFT);
                        }
                        else if (serverframe.Laterframe.Direction.Right)
                        {
                            print("player right Button click receive:\n" + serverframe.Laterframe);
                            pvpplayer.Set_Player_MoveState(PvpPlayer.MoveState.RIGHT);
                        }
                        else if (serverframe.Laterframe.Direction.Up)
                        {
                            print("player up Button click receive:\n" + serverframe.Laterframe);
                            pvpplayer.Set_Player_MoveState(PvpPlayer.MoveState.UP);
                        }
                        else
                        {
                            Debug.LogError("invalid direction:" + serverframe.Laterframe.Direction);
                        }
                    }
                    //player snow created
                    if (serverframe.Laterframe.Snow.Isgenerated)
                    {
                        //attack
                        print("player-side create snow:\n" + serverframe.Laterframe);
                        Vector3 start_pos = new Vector3(serverframe.Laterframe.Snow.Pos.X, serverframe.Laterframe.Snow.Pos.Y, serverframe.Laterframe.Snow.Pos.Z);
                        pvpplayer.Throw_Snow(start_pos);
                    }
                    //hpchanged
                    if (serverframe.Laterframe.Hpchanged)
                    {
                        if (serverframe.Laterframe.Playerhp.Ischanged)
                        {
                            pvpplayer.TakeDamage(serverframe.Laterframe.Playerhp.Changevalue);
                        }
                        if (serverframe.Laterframe.Enemyhp.Ischanged)
                        {
                            pvpenemy.TakeDamage(serverframe.Laterframe.Enemyhp.Changevalue);
                        }
                    }
                }
                else
                {
                    //moved
                    if (serverframe.Laterframe.Moved)
                    {
                        if (serverframe.Laterframe.Direction.Left)
                        {
                            print("enemy right Button click receive:\n" + serverframe.Laterframe);
                            pvpenemy.Set_Player_MoveState(PvpEnemy.MoveState.RIGHT);
                        }
                        else if (serverframe.Laterframe.Direction.Right)
                        {
                            print("enemy left Button click receive:\n" + serverframe.Laterframe);
                            pvpenemy.Set_Player_MoveState(PvpEnemy.MoveState.LEFT);
                        }
                        else if (serverframe.Laterframe.Direction.Up)
                        {
                            print("enemy up Button click receive:\n" + serverframe.Laterframe);
                            pvpenemy.Set_Player_MoveState(PvpEnemy.MoveState.UP);
                        }
                        else
                        {
                            Debug.LogError("invalid direction:" + serverframe.Laterframe.Direction);
                        }
                    }
                    //enemy snow created
                    if (serverframe.Laterframe.Snow.Isgenerated)
                    {
                        //attack
                        print("enemy-side create snow:\n" + serverframe.Laterframe);
                        Vector3 start_pos = new Vector3(-1*serverframe.Laterframe.Snow.Pos.X, serverframe.Laterframe.Snow.Pos.Y, serverframe.Laterframe.Snow.Pos.Z);
                        pvpenemy.Throw_Snow(start_pos);
                    }
                    //hpchanged
                    if (serverframe.Laterframe.Hpchanged)
                    {
                        if (serverframe.Laterframe.Playerhp.Ischanged)
                        {
                            pvpenemy.TakeDamage(serverframe.Laterframe.Playerhp.Changevalue);
                        }
                        if (serverframe.Laterframe.Enemyhp.Ischanged)
                        {
                            pvpplayer.TakeDamage(serverframe.Laterframe.Enemyhp.Changevalue);
                        }
                    }
                }
            }

            //produce common frame
            if (serverframe.Comframe.Generated)
            {
                Vector3 start_position;
                if (serverframe.Comframe.Animal.Isgenerated)
                {
                    Debug.Log("Animal created");
                    //create animal
                    if (ip == serverframe.Comframe.Chooseip)
                    {
                        start_position = new Vector3(serverframe.Comframe.Animal.Pos.X, serverframe.Comframe.Animal.Pos.Y, serverframe.Comframe.Animal.Pos.Z);
                    }
                    else
                    {
                        start_position = new Vector3(-1 * serverframe.Comframe.Animal.Pos.X, serverframe.Comframe.Animal.Pos.Y, serverframe.Comframe.Animal.Pos.Z);
                    }
                    
                    Instantiate(AnimalPrefab, start_position, gameObject.transform.rotation);
                }
                if (serverframe.Comframe.Bird.Isgenerated)
                {
                    Debug.Log("Bird created");
                    //create bird
                    if (ip == serverframe.Comframe.Chooseip)
                    {
                        start_position = new Vector3(serverframe.Comframe.Bird.Pos.X, serverframe.Comframe.Bird.Pos.Y, serverframe.Comframe.Bird.Pos.Z);
                    }
                    else
                    {
                        start_position = new Vector3(-1 * serverframe.Comframe.Bird.Pos.X, serverframe.Comframe.Bird.Pos.Y, serverframe.Comframe.Bird.Pos.Z);
                    }
                    Instantiate(BirdPrefab, start_position, gameObject.transform.rotation);
                }
                if (serverframe.Comframe.Food.Isgenerated)
                {
                    Debug.Log("Food created");
                    //create food
                    if (ip == serverframe.Comframe.Chooseip)
                    {
                        start_position = new Vector3(serverframe.Comframe.Food.Pos.X, serverframe.Comframe.Food.Pos.Y, serverframe.Comframe.Food.Pos.Z);
                    }
                    else
                    {
                        start_position = new Vector3(-1 * serverframe.Comframe.Food.Pos.X, serverframe.Comframe.Food.Pos.Y, serverframe.Comframe.Food.Pos.Z);
                    }
                    Instantiate(FoodPrefab, start_position, gameObject.transform.rotation);
                }
            }

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
