using UnityEngine;
using System.Collections;
using System.Reflection;
using System.Security.Policy;

public class Snow : MonoBehaviour {
    //hurt value
    public float hurtValue;
    //horizontal Speed and distance
    public float horizontal_speed;
    public float horizontal_distance;
    //ladder
    private GameObject ladder;
    //
    private const float G = 9.78046f/4;
    private float vertical_speed;
    private float time_count;
    private float distinguish_x;
    //dynamic trans
    public Sprite[] trans_sprites;
    //SpriteRenderer
    private SpriteRenderer spriteRenderer;
    //trans time
    public float trans_time;
    private float trans_count;
    private int trans_index;
    //snow sound
    private AudioManager audio;

    // Use this for initialization
    void Start () {
        //player sound
        audio = (GameObject.FindWithTag("audiomanager")).GetComponent<AudioManager>();
        //audio.PlayOneShotIndex(1);
        //
        float tmptime = horizontal_distance / horizontal_speed;
        vertical_speed = G*(tmptime/2);
        ladder = GameObject.FindWithTag("ladder");
        if (!ladder)
        {
            Debug.LogError("invalid ladder,please check!");
        }
        time_count = 0f;
        distinguish_x = transform.position.x;
        //
        spriteRenderer = gameObject.GetComponent<Renderer>() as SpriteRenderer;
        //static picture init
        spriteRenderer.sprite = trans_sprites[0];
        trans_count = 0f;
        trans_index = 0;
    }
	
	// Update is called once per frame
	void Update () {
	    if (trans_count >= trans_time)
	    {
	        trans_index = (trans_index + 1)%2;
	        spriteRenderer.sprite = trans_sprites[trans_index];
	        trans_count = 0f;
	    }
	    else
	    {
	        trans_count += Time.deltaTime;
	    }
        //transform.Rotate(new Vector3(0, 0, 90) * Time.deltaTime);
        if (transform.position.y < ladder.transform.position.y)
	    {
            //Debug.Log("snow position:"+transform.position);
	        Destroy(gameObject);
	    }
	    else
	    {
            time_count += Time.deltaTime;
	        float up_speed = vertical_speed - G*time_count;
	        if (distinguish_x < 0)
	        {
	            transform.Translate(transform.right*horizontal_speed*Time.deltaTime);
	        }
	        else
	        {
	            transform.Translate(transform.right*(-1*horizontal_speed)*Time.deltaTime);
	        }
            transform.Translate(transform.up * up_speed * Time.deltaTime, Space.World);
        }
	}

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.tag == "player")
        {
            //play hurt sound
            audio.PlayOneShotIndex(2);
            //switch to hurt picture
            other.gameObject.GetComponent<Player>().Set_dynamic_sprite(1);
            other.gameObject.GetComponent<Player>().TakeDamage(hurtValue);
            Destroy(gameObject);
        }
        else if (other.gameObject.tag == "enemy")
        {
            //switch to hurt picture
            other.gameObject.GetComponent<Enemy>().Set_dynamic_sprite(1);
            other.gameObject.GetComponent<Enemy>().TakeDamage(hurtValue);
            Destroy(gameObject);
        }
        Debug.Log("invalid OnTriggerEnter2D");
    }
}
