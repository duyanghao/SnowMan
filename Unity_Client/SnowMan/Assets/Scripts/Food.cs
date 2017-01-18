using UnityEngine;
using System.Collections;

public class Food : MonoBehaviour {
    //hurt value
    public float helpValue;
    //ladder
    private GameObject ladder;
    //snow sound
    private AudioManager audio;

    // Use this for initialization
    void Start () {
        //player sound
        audio = (GameObject.FindWithTag("audiomanager")).GetComponent<AudioManager>();
        //
        ladder = GameObject.FindWithTag("ladder");
        if (!ladder)
        {
            Debug.LogError("invalid ladder,please check!");
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
            other.gameObject.GetComponent<Player>().Set_dynamic_sprite(3);
            other.gameObject.GetComponent<Player>().TakeHelp(helpValue);
            Destroy(gameObject);
        }
        else if (other.gameObject.tag == "enemy")
        {
            //switch to help picture
            other.gameObject.GetComponent<Enemy>().Set_dynamic_sprite(3);
            other.gameObject.GetComponent<Enemy>().TakeHelp(helpValue);
            Destroy(gameObject);
        }
        Debug.Log("invalid OnTriggerEnter2D");
    }
}
