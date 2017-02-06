using UnityEngine;
using System.Collections;
//using UnityEditor;
using UnityEngine.SceneManagement;

public class PvpEnemy : MonoBehaviour
{

    //player Move statement
    public enum MoveState
    {
        STATIC = 1,
        LEFT = 2,
        RIGHT = 3,
        UP = 4,
    }
    //player operation statement
    public enum OpeState
    {
        ATTACK = 1,
    }

    //state vars
    private MoveState playerstate;
    private MoveState state_move;
    //speed and distance
    public float horizontal_speed;
    public float horizontal_distance;
    public float vertical_speed;
    public float vertical_distance;
    //border
    public float left_border;
    public float right_border;
    //target position
    private Vector3 target;
    private bool flag;
    private float eps;
    private int up_count;
    private float up_y;
    //hp
    private float HP;
    //private bool trag;
    //snow
    public GameObject SnowPrefab;

    //add for machine-ai(enemy)
    public float time_attack;
    public float time_move;
    private float attack_delay;
    private float move_delay;

    //SpriteRenderer
    private SpriteRenderer spriteRenderer;
    //static_state_picture
    public Sprite static_sprites;
    //left_state_picture
    public Sprite[] left_sprites;
    public float left_framesPerSecond;
    //right_state_picture
    public Sprite[] right_sprites;
    public float right_framesPerSecond;
    //up_state_picture
    public Sprite up_sprites;
    //public Sprite[] up_sprites;
    //public float up_framesPerSecond;
    //down_state_picture
    //public Sprite[] down_sprites;
    //public float down_framesPerSecond;
    //attack picture
    public Sprite[] attack_sprites;
    public float attack_delay_time;
    private bool attack_in_delay;
    private float attack_delay_count;
    private int attack_index;
    //button_click
    private ui_pvp buttonclick;
    //die picture
    public Sprite die_sprites;
    //die show
    public float die_time;
    private bool Is_died;
    private float die_count;
    private GameObject ladder;

    //hp
    //public Slider healthSlider;
    private float barDisplay;
    //public Vector2 pos = new Vector2(20, 40);
    //public Vector2 size = new Vector2(100, 20);
    public Texture2D emptyTex;
    public Texture2D fullTex;

    public void Set_died(bool died)
    {
        Is_died = died;
    }

    // Use this for initialization
    void Start()
    {
        //button_click
        buttonclick = (GameObject.FindWithTag("uilogical")).GetComponent<ui_pvp>();
        //
        playerstate = MoveState.STATIC;
        state_move = MoveState.STATIC;
        target = this.transform.position;
        flag = false;
        //trag = false;
        eps = 0.001f;
        up_count = 0;
        HP = 100.0f;
        if (!SnowPrefab)
        {
            Debug.LogError("invalid SnowPrefab,please check!");
        }

        //add for machine-ai(enemy)
        attack_delay = 0f;
        move_delay = 0f;
        //spriteRenderer
        spriteRenderer = gameObject.GetComponent<Renderer>() as SpriteRenderer;
        //static picture init
        spriteRenderer.sprite = static_sprites;
        //attack_in_delay init
        attack_in_delay = false;
        attack_delay_count = 0f;
        //die
        die_count = 0f;
        Is_died = false;
        ladder = GameObject.FindWithTag("ladder");
    }

    // Update is called once per frame
    void Update()
    {
        barDisplay = HP * 0.01f;
        if (Is_died)
        {
            if (die_count >= die_time)
            {
                Is_died = false;
                Die();
            }
            die_count += Time.deltaTime;
            return;
        }
        //Simulation_enemy();
        if (attack_in_delay)
        {
            attack_delay_count += Time.deltaTime;
            if (attack_delay_count >= attack_delay_time)
            {
                //reset the static picture
                spriteRenderer.sprite = static_sprites;
                //reset the attack_in_delay
                attack_in_delay = false;
            }
            else
            {
                spriteRenderer.sprite = attack_sprites[attack_index];
            }
        }
        /*if (trag)
        {
            Throw_Snow();
        }*/
        if (playerstate != MoveState.STATIC || state_move != MoveState.STATIC)
        {
            Move();
        }
        //reset
        playerstate = MoveState.STATIC;
    }

    /*void Simulation_enemy()
    {
        if (attack_delay >= time_attack)
        {
            Set_Player_OpeState(OpeState.ATTACK);
            attack_delay = 0f;
        }
        if (move_delay >= time_move)
        {
            //Debug.Log("move action!");
            int index = Random.Range(2, 5);
            switch (index)
            {
                case 2:
                    Set_Player_MoveState(MoveState.LEFT);
                    break;
                case 3:
                    Set_Player_MoveState(MoveState.RIGHT);
                    break;
                case 4:
                    Set_Player_MoveState(MoveState.UP);
                    break;
                default:
                    Debug.LogError("invalid random move index:" + index);
                    break;
            }
            move_delay = 0f;
        }
        //Debug.Log("time:"+move_delay);
        attack_delay += Time.deltaTime;
        move_delay += Time.deltaTime;
    }*/

    void Move()
    {
        if (state_move == MoveState.STATIC)
        {
            state_move = playerstate;
            flag = true;
        }
        switch (state_move)
        {
            case MoveState.LEFT:
                if (flag)
                {
                    target.x = transform.position.x - horizontal_distance;
                    if (target.x < left_border)
                    {
                        target.x = left_border;
                    }
                    flag = false;
                }
                if ((transform.position - target).sqrMagnitude > eps)
                {
                    if (!attack_in_delay)
                    {
                        int index = (int)(Time.timeSinceLevelLoad * left_framesPerSecond);
                        //Debug.Log("index:"+index);
                        index = index % left_sprites.Length;
                        spriteRenderer.sprite = left_sprites[index];
                    }
                    transform.position -= (transform.position - target).normalized * horizontal_speed * Time.deltaTime;
                }
                else
                {
                    //reset the static picture
                    spriteRenderer.sprite = static_sprites;
                    transform.position = target;
                    state_move = MoveState.STATIC;
                    Debug.Log("Now Position:" + transform.position);
                }
                break;

            case MoveState.RIGHT:
                if (flag)
                {

                    target.x = transform.position.x + horizontal_distance;
                    if (target.x > right_border)
                    {
                        target.x = right_border;
                    }
                    flag = false;
                }
                if ((transform.position - target).sqrMagnitude > eps)
                {
                    if (!attack_in_delay)
                    {
                        int index = (int)(Time.timeSinceLevelLoad * right_framesPerSecond);
                        //Debug.Log("index:"+index);
                        index = index % right_sprites.Length;
                        spriteRenderer.sprite = right_sprites[index];
                    }
                    transform.position -= (transform.position - target).normalized * horizontal_speed * Time.deltaTime;
                }
                else
                {
                    //reset the static picture
                    spriteRenderer.sprite = static_sprites;
                    transform.position = target;
                    state_move = MoveState.STATIC;
                    Debug.Log("Now Position:" + transform.position);
                }
                break;

            case MoveState.UP:
                if (flag)
                {
                    up_count = 1;
                    up_y = transform.position.y;
                    target.y = transform.position.y + vertical_distance;
                    flag = false;
                }
                if (up_count == 1)
                {
                    if ((transform.position - target).sqrMagnitude > eps)
                    {
                        if (!attack_in_delay)
                        {
                            spriteRenderer.sprite = up_sprites;
                        }
                        transform.position -= (transform.position - target).normalized * horizontal_speed * Time.deltaTime;
                    }
                    else
                    {
                        up_count = 2;
                        transform.position = target;
                        target.y = up_y;
                        Debug.Log("Now Position:" + transform.position);
                    }
                }
                if (up_count == 2)
                {
                    if ((transform.position - target).sqrMagnitude > eps)
                    {
                        if (!attack_in_delay)
                        {
                            spriteRenderer.sprite = up_sprites;
                        }
                        transform.position -= (transform.position - target).normalized * horizontal_speed * Time.deltaTime;
                    }
                    else
                    {
                        //reset the static picture
                        spriteRenderer.sprite = static_sprites;
                        up_count = 0;
                        transform.position = target;
                        state_move = MoveState.STATIC;
                        Debug.Log("Now Position:" + transform.position);
                    }
                }
                break;

            default:
                Debug.LogError("invalid state_move vars:" + state_move);
                break;
        }
    }
    //set player state
    public void Set_Player_MoveState(MoveState index)
    {
        if (Is_died)
        {
            return;
        }
        switch (index)
        {
            case MoveState.LEFT:
                playerstate = MoveState.LEFT;
                break;
            case MoveState.RIGHT:
                playerstate = MoveState.RIGHT;
                break;
            case MoveState.STATIC:
                playerstate = MoveState.STATIC;
                break;
            case MoveState.UP:
                playerstate = MoveState.UP;
                break;
            default:
                Debug.LogError("invalid move state:" + index);
                break;
        }
    }

    /*public void Set_Player_OpeState(OpeState index)
    {
        if (Is_died)
        {
            return;
        }
        switch (index)
        {
            case OpeState.ATTACK:
                trag = true;
                break;
            default:
                Debug.LogError("invalid operation state" + index);
                break;
        }
    }*/

    public void Set_dynamic_sprite(int index)
    {
        if (Is_died)
        {
            return;
        }
        attack_index = index;
        spriteRenderer.sprite = attack_sprites[attack_index];
        attack_in_delay = true;
        attack_delay_count = 0f;
    }

    public void Throw_Snow(Vector3 start_position)
    {
        if (Is_died)
        {
            return;
        }
        //...
        if (SnowPrefab)
        {
            //set attack picture
            /*spriteRenderer.sprite = attack_sprites;
            attack_in_delay = true;
            attack_delay_count = 0f;*/
            Set_dynamic_sprite(0);

            //GameObject snow;
            //Vector3 start_position = new Vector3(transform.position.x, transform.position.y + 1.5f, transform.position.z);
            //snow = Instantiate(SnowPrefab, start_position, gameObject.transform.rotation) as GameObject;
            Instantiate(SnowPrefab, start_position, gameObject.transform.rotation);
            Debug.Log("throw snow attack!");
        }
        //trag = false;
    }

    // Update is called once per frame
    void OnGUI()
    {
        //GUI.color = Color.red;

        //GUI.Label(new Rect(870, 0, 200, 30), "Enemy HP: " + HP.ToString());

        Vector2 pos = new Vector2(930, 30);
        Vector2 size = new Vector2(100, 30);

        //draw the background:
        GUI.BeginGroup(new Rect(pos.x, pos.y, size.x, size.y));
        GUI.Box(new Rect(0, 0, size.x, size.y), emptyTex);

        //draw the filled-in part:
        GUI.BeginGroup(new Rect(0, 0, size.x * barDisplay, size.y));
        GUI.Box(new Rect(0, 0, size.x, size.y), fullTex);
        GUI.EndGroup();
        GUI.EndGroup();

    }

    //change hp(help and hurt)
    public void TakeDamage(float hurtValue)
    {
        if (Is_died)
        {
            return;
        }
        HP += hurtValue;
        if (HP > 100.0f)
        {
            HP = 100.0f;
        }
        else if (HP <= 0)
        {
            HP = 0;
            Is_died = true;
            //Die();
            //Send_Die_Msg();
        }
    }

    /*
    public void TakeDamage(float hurtValue)
    {
        if (Is_died)
        {
            return;
        }
        HP -= hurtValue;
        if (HP <= 0)
        {
            HP = 0;
            Is_died = true;
            Die();
        }
    }
    public void TakeHelp(float helpValue)
    {
        if (Is_died)
        {
            return;
        }
        HP += helpValue;
        if (HP > 100.0f)
        {
            HP = 100.0f;
        }
    }
    */

    public void Die()
    {
        //Application.LoadLevel(Application.loadedLevel);
        //Debug.Log("restart the game!");
        //SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        if (Is_died)
        {
            transform.position = new Vector3(transform.position.x, ladder.transform.position.y, transform.position.z);
            spriteRenderer.sprite = die_sprites;
        }
        else
        {
            buttonclick.PlayerSucceed();
            Is_died = true;
        }
        //Debug.Log("enemy lose");
    }

}
