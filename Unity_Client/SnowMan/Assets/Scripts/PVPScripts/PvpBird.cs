using UnityEngine;
using System.Collections;
using Google.ProtocolBuffers;

public class PvpBird : MonoBehaviour
{
    //food
    //public GameObject FoodPrefab;
    //speed
    public float horizontal_speed;
    //player
    private GameObject player;
    //target position
    private Vector3 target;
    //eps
    private float eps;
    //socket_generate
    private SocketGenerate socket_generate;
    //sprite
    //SpriteRenderer
    private SpriteRenderer spriteRenderer;
    public Sprite forward_sprites;
    public Sprite backward_sprites;

    void Start()
    {
        spriteRenderer = gameObject.GetComponent<Renderer>() as SpriteRenderer;
        Debug.Log("bird created position:" + transform.position);
        player = GameObject.FindWithTag("player");
        if (!player)
        {
            Debug.LogError("invalid player,please check!");
        }
        float left_border = player.GetComponent<PvpPlayer>().left_border;
        float right_border = player.GetComponent<PvpPlayer>().right_border;

        if (transform.position.x <= right_border)
        {
            //right move
            target = new Vector3(-1 * left_border, transform.position.y, transform.position.z);
            //forward_sprites
            spriteRenderer.sprite = forward_sprites;
        }
        else
        {
            //left move
            target = new Vector3(left_border, transform.position.y, transform.position.z);
            //backward_sprites
            spriteRenderer.sprite = backward_sprites;
        }
        eps = 0.001f;
        //socket_generate init
        socket_generate = GameObject.FindWithTag("MainCamera").GetComponent<SocketGenerate>();
        if (!socket_generate)
        {
            Debug.LogError("invalid socket_generate,please check!");
        }
        //repeated create bird
        InvokeRepeating("CreateFood", 1f, 3.0f);
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
            Debug.Log("Bird Position:" + transform.position);
            Destroy(gameObject);
        }
    }

    void CreateFood()
    {
        Debug.Log("CreateFood");
        //Instantiate(FoodPrefab, transform.position, gameObject.transform.rotation);
        //direction builder
        CodeBattle.Move_Direction.Builder movedirectionbuilder = new CodeBattle.Move_Direction.Builder();
        movedirectionbuilder.Left = false;
        movedirectionbuilder.Right = false;
        movedirectionbuilder.Up = false;
        //Position builder
        CodeBattle.Generated_Position.Builder positionbuilder = new CodeBattle.Generated_Position.Builder();
        positionbuilder.X = transform.position.x;
        positionbuilder.Y = transform.position.y;
        positionbuilder.Z = transform.position.z;
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
        clientframeBuilder.Objecttype = 4;
        clientframeBuilder.Pos = positionbuilder.BuildPartial();
        //
        CodeBattle.Client_Frame tmp = clientframeBuilder.BuildPartial();
        byte[] bytes = socket_generate.Client_FrameToBytes(tmp);
        socket_generate.Send_frame(bytes);
        print("create food sent:\n" + tmp);
    }



}
