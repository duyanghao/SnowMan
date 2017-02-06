using UnityEngine;
using System.Collections;

public class Bird : MonoBehaviour {
    //food
    public GameObject FoodPrefab;
    //speed
    public float horizontal_speed;
    //player
    private GameObject player;
    //target position
    private Vector3 target;
    //eps
    private float eps;
    //sprite
    //SpriteRenderer
    private SpriteRenderer spriteRenderer;
    public Sprite forward_sprites;
    public Sprite backward_sprites;

    void Start ()
    {
        spriteRenderer = gameObject.GetComponent<Renderer>() as SpriteRenderer;
        Debug.Log("bird created position:"+transform.position);
        player = GameObject.FindWithTag("player");
        if (!player)
        {
            Debug.LogError("invalid player,please check!");
        }
        float left_border = player.GetComponent<Player>().left_border;
        float right_border = player.GetComponent<Player>().right_border;
        
        if (transform.position.x <= right_border)
        {
            //right move
            target = new Vector3(-1*left_border,transform.position.y,transform.position.z);
            //forward_sprites
            spriteRenderer.sprite = forward_sprites;
        }
        else
        {
            //left move
            target = new Vector3(left_border,transform.position.y,transform.position.z);
            //backward_sprites
            spriteRenderer.sprite = backward_sprites;
        }
        eps = 0.001f;
        //repeated create bird
        InvokeRepeating("CreateFood", 1f, 3.0f);
    }
	
	// Update is called once per frame
	void Update () {
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
        Instantiate(FoodPrefab, transform.position, gameObject.transform.rotation);
    }



}
