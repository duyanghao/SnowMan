using UnityEngine;
using System.Collections;

public class Animal : MonoBehaviour {
    //hurt value
    public float hurtValue;
    //horizontal speed
    public float horizontal_speed;
    //target position
    private Vector3 target;
    //player
    private GameObject player;
    //eps
    private float eps;
    //snow sound
    private AudioManager audio;

    // Use this for initialization
    void Start () {
        //sound
        audio = (GameObject.FindWithTag("audiomanager")).GetComponent<AudioManager>();
        //
        Debug.Log("animal created position:" + transform.position);
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
            target = new Vector3(-1 * left_border, transform.position.y, transform.position.z);
        }
        else
        {
            //left move
            target = new Vector3(left_border, transform.position.y, transform.position.z);
        }
        eps = 0.001f;
    }
	
	// Update is called once per frame
	void Update () {
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
            other.gameObject.GetComponent<Player>().Set_dynamic_sprite(2);
            other.gameObject.GetComponent<Player>().TakeDamage(hurtValue);
            Destroy(gameObject);
        }
        else if (other.gameObject.tag == "enemy")
        {
            //switch to bite picture
            other.gameObject.GetComponent<Enemy>().Set_dynamic_sprite(2);
            other.gameObject.GetComponent<Enemy>().TakeDamage(hurtValue);
            Destroy(gameObject);
        }
        Debug.Log("invalid OnTriggerEnter2D");
    }
}
