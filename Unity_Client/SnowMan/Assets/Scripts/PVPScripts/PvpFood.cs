using UnityEngine;
using System.Collections;
using Google.ProtocolBuffers;

public class PvpFood : MonoBehaviour
{
    //hurt value
    public float helpValue;
    //ladder
    private GameObject ladder;
    //snow sound
    private AudioManager audio;
    //socket_generate
    private SocketGenerate socket_generate;

    // Use this for initialization
    void Start()
    {
        //player sound
        audio = (GameObject.FindWithTag("audiomanager")).GetComponent<AudioManager>();
        //
        ladder = GameObject.FindWithTag("ladder");
        if (!ladder)
        {
            Debug.LogError("invalid ladder,please check!");
        }
        //socket_generate init
        socket_generate = GameObject.FindWithTag("MainCamera").GetComponent<SocketGenerate>();
        if (!socket_generate)
        {
            Debug.LogError("invalid socket_generate,please check!");
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (transform.position.y < ladder.transform.position.y)
        {
            Debug.Log("food position:" + transform.position);
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.tag == "player")
        {
            //play food sound
            audio.PlayOneShotIndex(5);
            //switch to help picture
            other.gameObject.GetComponent<PvpPlayer>().Set_dynamic_sprite(3);
            //other.gameObject.GetComponent<Player>().TakeHelp(helpValue);
            //construct the client frame
            //direction builder
            CodeBattle.Move_Direction.Builder movedirectionbuilder = new CodeBattle.Move_Direction.Builder();
            movedirectionbuilder.Left = false;
            movedirectionbuilder.Right = false;
            movedirectionbuilder.Up = false;
            //Position builder
            CodeBattle.Generated_Position.Builder positionbuilder = new CodeBattle.Generated_Position.Builder();
            positionbuilder.X = 0;
            positionbuilder.Y = 0;
            positionbuilder.Z = 0;
            //Client_Frame builder
            CodeBattle.Client_Frame.Builder clientframeBuilder = new CodeBattle.Client_Frame.Builder();
            clientframeBuilder.Ip = SocketGenerate.ip;
            clientframeBuilder.Died = false;
            clientframeBuilder.Moved = false;
            clientframeBuilder.Direction = movedirectionbuilder.BuildPartial();
            clientframeBuilder.Hpchanged = true;
            //true=player,false=enemy
            clientframeBuilder.Playertype = true;
            clientframeBuilder.Changevalue = helpValue;
            clientframeBuilder.Generated = false;
            clientframeBuilder.Objecttype = 0;
            clientframeBuilder.Pos = positionbuilder.BuildPartial();
            //
            CodeBattle.Client_Frame tmp = clientframeBuilder.BuildPartial();
            byte[] bytes = socket_generate.Client_FrameToBytes(tmp);
            socket_generate.Send_frame(bytes);
            print("food player help value has been sent:\n" + tmp);

            Destroy(gameObject);
        }
        else if (other.gameObject.tag == "enemy")
        {
            //switch to help picture
            other.gameObject.GetComponent<PvpEnemy>().Set_dynamic_sprite(3);
            //other.gameObject.GetComponent<Enemy>().TakeHelp(helpValue);
            //construct the client frame
            //direction builder
            CodeBattle.Move_Direction.Builder movedirectionbuilder = new CodeBattle.Move_Direction.Builder();
            movedirectionbuilder.Left = false;
            movedirectionbuilder.Right = false;
            movedirectionbuilder.Up = false;
            //Position builder
            CodeBattle.Generated_Position.Builder positionbuilder = new CodeBattle.Generated_Position.Builder();
            positionbuilder.X = 0;
            positionbuilder.Y = 0;
            positionbuilder.Z = 0;
            //Client_Frame builder
            CodeBattle.Client_Frame.Builder clientframeBuilder = new CodeBattle.Client_Frame.Builder();
            clientframeBuilder.Ip = SocketGenerate.ip;
            clientframeBuilder.Died = false;
            clientframeBuilder.Moved = false;
            clientframeBuilder.Direction = movedirectionbuilder.BuildPartial();
            clientframeBuilder.Hpchanged = true;
            //true=player,false=enemy
            clientframeBuilder.Playertype = false;
            clientframeBuilder.Changevalue = helpValue;
            clientframeBuilder.Generated = false;
            clientframeBuilder.Objecttype = 0;
            clientframeBuilder.Pos = positionbuilder.BuildPartial();
            //
            CodeBattle.Client_Frame tmp = clientframeBuilder.BuildPartial();
            byte[] bytes = socket_generate.Client_FrameToBytes(tmp);
            socket_generate.Send_frame(bytes);
            print("food enemy help value has been sent:\n" + tmp);

            Destroy(gameObject);
        }
        Debug.Log("invalid OnTriggerEnter2D");
    }
}
