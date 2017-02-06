using UnityEngine;
using System.Collections;
using Google.ProtocolBuffers;

public class PvpAnimal : MonoBehaviour
{
    //hurt value
    public float hurtValue;
    //horizontal speed
    public float horizontal_speed;
    //target position
    private Vector3 target;
    //player
    private GameObject pvpplayer;
    //eps
    private float eps;
    //snow sound
    private AudioManager audio;
    //socket_generate
    private SocketGenerate socket_generate;

    // Use this for initialization
    void Start()
    {
        //sound
        audio = (GameObject.FindWithTag("audiomanager")).GetComponent<AudioManager>();
        //
        Debug.Log("animal created position:" + transform.position);
        pvpplayer = GameObject.FindWithTag("player");
        if (!pvpplayer)
        {
            Debug.LogError("invalid player,please check!");
        }
        float left_border = pvpplayer.GetComponent<PvpPlayer>().left_border;
        float right_border = pvpplayer.GetComponent<PvpPlayer>().right_border;
        if (transform.position.x <= right_border)
        {
            //right move
            target = new Vector3(-1 * left_border, transform.position.y, transform.position.z);
        }
        else
        {
            //left move
            target = new Vector3(left_border, transform.position.y, transform.position.z);
        }
        eps = 0.001f;
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
        if ((transform.position - target).sqrMagnitude > eps)
        {
            transform.position -= (transform.position - target).normalized * horizontal_speed * Time.deltaTime;
        }
        else
        {
            Debug.Log("Animal Position:" + transform.position);
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.tag == "player")
        {
            //play bite sound
            audio.PlayOneShotIndex(3);
            //switch to bite picture
            other.gameObject.GetComponent<PvpPlayer>().Set_dynamic_sprite(2);
            //other.gameObject.GetComponent<Player>().TakeDamage(hurtValue);

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
            clientframeBuilder.Changevalue = hurtValue;
            clientframeBuilder.Generated = false;
            clientframeBuilder.Objecttype = 0;
            clientframeBuilder.Pos = positionbuilder.BuildPartial();
            //
            CodeBattle.Client_Frame tmp = clientframeBuilder.BuildPartial();
            byte[] bytes = socket_generate.Client_FrameToBytes(tmp);
            socket_generate.Send_frame(bytes);
            print("animal player hurt value has been sent:\n" + tmp);

            Destroy(gameObject);
        }
        else if (other.gameObject.tag == "enemy")
        {
            //switch to bite picture
            other.gameObject.GetComponent<PvpEnemy>().Set_dynamic_sprite(2);
            //other.gameObject.GetComponent<Enemy>().TakeDamage(hurtValue);

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
            clientframeBuilder.Changevalue = hurtValue;
            clientframeBuilder.Generated = false;
            clientframeBuilder.Objecttype = 0;
            clientframeBuilder.Pos = positionbuilder.BuildPartial();
            //
            CodeBattle.Client_Frame tmp = clientframeBuilder.BuildPartial();
            byte[] bytes = socket_generate.Client_FrameToBytes(tmp);
            socket_generate.Send_frame(bytes);
            print("animal enemy hurt value has been sent:\n" + tmp);

            Destroy(gameObject);
        }
        Debug.Log("invalid OnTriggerEnter2D");
    }
}
