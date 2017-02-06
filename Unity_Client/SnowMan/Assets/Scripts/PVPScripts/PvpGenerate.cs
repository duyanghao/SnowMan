using UnityEngine;
using System.Collections;
using System.Security.Permissions;
using Google.ProtocolBuffers;

public class PvpGenerate : MonoBehaviour
{
    //ladder
    private GameObject ladder;
    //player
    private GameObject player;
    //border
    private float left_border;
    private float right_border;
    //bird
    //public GameObject BirdPrefab;
    //animal
    //public GameObject AnimalPrefab;
    //background snow
    public GameObject BgsnowPrefab;
    //socket_generate
    private SocketGenerate socket_generate;

    // Use this for initialization
    void Start()
    {
        ladder = GameObject.FindWithTag("ladder");
        if (!ladder)
        {
            Debug.LogError("invalid ladder,please check!");
        }
        player = GameObject.FindWithTag("player");
        if (!player)
        {
            Debug.LogError("invalid player,please check!");
        }
        left_border = -1 * player.GetComponent<PvpPlayer>().right_border;
        right_border = -1 * player.GetComponent<PvpPlayer>().left_border;
        //repeated create bird
        InvokeRepeating("CreateBird", 1f, 4.0f);
        InvokeRepeating("CreateAnimal", 1f, 5.0f);
        InvokeRepeating("CreateSnow", 1f, 0.05f);
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
        //...    
    }

    void CreateBird()
    {
        Debug.Log("CreateBird");
        //start x cur
        float start_x = Random.Range(left_border, right_border);
        //direction
        int tmp_dir = Random.Range(0, 2);
        if (tmp_dir == 0)
        {
            tmp_dir = -1;
        }
        start_x *= tmp_dir;
        Vector3 start_position = new Vector3(start_x, ladder.transform.position.y + 8f, transform.position.z);
        //Instantiate(BirdPrefab, start_position, gameObject.transform.rotation);
        //direction builder
        CodeBattle.Move_Direction.Builder movedirectionbuilder = new CodeBattle.Move_Direction.Builder();
        movedirectionbuilder.Left = false;
        movedirectionbuilder.Right = false;
        movedirectionbuilder.Up = false;
        //Position builder
        CodeBattle.Generated_Position.Builder positionbuilder = new CodeBattle.Generated_Position.Builder();
        positionbuilder.X = start_position.x;
        positionbuilder.Y = start_position.y;
        positionbuilder.Z = start_position.z;
        //Client_Frame builder
        CodeBattle.Client_Frame.Builder clientframeBuilder = new CodeBattle.Client_Frame.Builder();
        clientframeBuilder.Ip = SocketGenerate.ip;
        clientframeBuilder.Died = false;
        clientframeBuilder.Moved = false;
        clientframeBuilder.Direction = movedirectionbuilder.BuildPartial();
        clientframeBuilder.Hpchanged = false;
        clientframeBuilder.Playertype = false;
        clientframeBuilder.Changevalue = 0f;
        clientframeBuilder.Generated = true;
        //1=snow,2=animal,3=bird,4=food
        clientframeBuilder.Objecttype = 3;
        clientframeBuilder.Pos = positionbuilder.BuildPartial();
        //
        CodeBattle.Client_Frame tmp = clientframeBuilder.BuildPartial();
        byte[] bytes = socket_generate.Client_FrameToBytes(tmp);
        socket_generate.Send_frame(bytes);
        print("create bird sent:\n" + tmp);
    }

    void CreateAnimal()
    {
        Debug.Log("CreateAnimal");
        //start x cur
        float start_x = Random.Range(left_border, right_border);
        //direction
        int tmp_dir = Random.Range(0, 2);
        if (tmp_dir == 0)
        {
            tmp_dir = -1;
        }
        start_x *= tmp_dir;
        Vector3 start_position = new Vector3(start_x, ladder.transform.position.y, transform.position.z);
        //Instantiate(AnimalPrefab, start_position, gameObject.transform.rotation);
        //direction builder
        CodeBattle.Move_Direction.Builder movedirectionbuilder = new CodeBattle.Move_Direction.Builder();
        movedirectionbuilder.Left = false;
        movedirectionbuilder.Right = false;
        movedirectionbuilder.Up = false;
        //Position builder
        CodeBattle.Generated_Position.Builder positionbuilder = new CodeBattle.Generated_Position.Builder();
        positionbuilder.X = start_position.x;
        positionbuilder.Y = start_position.y;
        positionbuilder.Z = start_position.z;
        //Client_Frame builder
        CodeBattle.Client_Frame.Builder clientframeBuilder = new CodeBattle.Client_Frame.Builder();
        clientframeBuilder.Ip = SocketGenerate.ip;
        clientframeBuilder.Died = false;
        clientframeBuilder.Moved = false;
        clientframeBuilder.Direction = movedirectionbuilder.BuildPartial();
        clientframeBuilder.Hpchanged = false;
        clientframeBuilder.Playertype = false;
        clientframeBuilder.Changevalue = 0f;
        clientframeBuilder.Generated = true;
        //1=snow,2=animal,3=bird,4=food
        clientframeBuilder.Objecttype = 2;
        clientframeBuilder.Pos = positionbuilder.BuildPartial();
        //
        CodeBattle.Client_Frame tmp = clientframeBuilder.BuildPartial();
        byte[] bytes = socket_generate.Client_FrameToBytes(tmp);
        socket_generate.Send_frame(bytes);
        print("create animal sent:\n" + tmp);
    }

    void CreateSnow()
    {
        //Debug.Log("CreateSnow");
        Camera cam = Camera.main;
        float height = 2f * cam.orthographicSize;
        float width = height * cam.aspect;
        //start x cur
        float start_x = Random.Range(-1 * (width / 2), width / 2);
        Vector3 start_position = new Vector3(start_x, height / 2, transform.position.z);
        Instantiate(BgsnowPrefab, start_position, gameObject.transform.rotation);
    }
}
