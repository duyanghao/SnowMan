using UnityEngine;
using System.Collections;
using Google.ProtocolBuffers;
public class ui_pvp : MonoBehaviour {
    //socket generate
    private GameObject _socket;
    private SocketGenerate socket_generate;
    
    // Use this for initialization
    void Start () {
        _socket = GameObject.FindWithTag("MainCamera");
        if (!_socket)
        {
            Debug.LogError("no socketgenerate available");
        }
        socket_generate = _socket.GetComponent<SocketGenerate>();
    }
	
	// Update is called once per frame
	void Update () {
	    //...
	}
    
    //button click
    public void OnMyButtonClickLeft()
    {
        //Debug.Log("left Button click sent");
        CodeBattle.Client_Frame.Builder clientframeBuilder = new CodeBattle.Client_Frame.Builder();
        clientframeBuilder.Ip = socket_generate.ip;
        clientframeBuilder.Direction = 1;
        CodeBattle.Client_Frame tmp = clientframeBuilder.BuildPartial();
        byte[] bytes = socket_generate.Client_FrameToBytes(tmp);
        socket_generate.Send_frame(bytes);
        print("left Button click sent:\n" + tmp);
    }

    public void OnMyButtonClickRight()
    {
        //Debug.Log("right Button click sent");
        CodeBattle.Client_Frame.Builder clientframeBuilder = new CodeBattle.Client_Frame.Builder();
        clientframeBuilder.Ip = socket_generate.ip;
        clientframeBuilder.Direction = 2;
        CodeBattle.Client_Frame tmp = clientframeBuilder.BuildPartial();
        byte[] bytes = socket_generate.Client_FrameToBytes(tmp);
        socket_generate.Send_frame(bytes);
        print("right Button click sent:\n" + tmp);
    }
    public void OnMyButtonClickUp()
    {
        //Debug.Log("up Button click sent");
        CodeBattle.Client_Frame.Builder clientframeBuilder = new CodeBattle.Client_Frame.Builder();
        clientframeBuilder.Ip = socket_generate.ip;
        clientframeBuilder.Direction = 3;
        CodeBattle.Client_Frame tmp = clientframeBuilder.BuildPartial();
        byte[] bytes = socket_generate.Client_FrameToBytes(tmp);
        socket_generate.Send_frame(bytes);
        print("up Button click sent:\n" + tmp);
    }
    public void OnMyButtonClickAttack()
    {
        //Debug.Log("attack Button click");
        CodeBattle.Client_Frame.Builder clientframeBuilder = new CodeBattle.Client_Frame.Builder();
        clientframeBuilder.Ip = socket_generate.ip;
        clientframeBuilder.Direction = 4;
        CodeBattle.Client_Frame tmp = clientframeBuilder.BuildPartial();
        byte[] bytes = socket_generate.Client_FrameToBytes(tmp);
        socket_generate.Send_frame(bytes);
        print("attack Button click sent:\n" + tmp);
    }

}
