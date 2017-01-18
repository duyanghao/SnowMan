using UnityEngine;
using System.Collections;
using System.Security.Permissions;

public class Generate : MonoBehaviour {
    //ladder
    private GameObject ladder;
    //player
    private GameObject player;
    //border
    private float left_border;
    private float right_border;
    //bird
    public GameObject BirdPrefab;
    //animal
    public GameObject AnimalPrefab;
    //background snow
    public GameObject BgsnowPrefab;

    // Use this for initialization
    void Start () {
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
        left_border = -1 * player.GetComponent<Player>().right_border;
        right_border = -1 * player.GetComponent<Player>().left_border;
        //repeated create bird
        InvokeRepeating("CreateBird", 1f, 4.0f);
        InvokeRepeating("CreateAnimal", 1f, 5.0f);
        InvokeRepeating("CreateSnow", 1f, 0.05f);
    }
	
	// Update is called once per frame
	void Update () {
	    //...    
	}

    void CreateBird()
    {
        Debug.Log("CreateBird");
        //start x cur
        float start_x = Random.Range(left_border, right_border);
        //direction
        int tmp_dir = Random.Range(0, 2);
        if (tmp_dir==0)
        {
            tmp_dir = -1;
        }
        start_x *= tmp_dir;
        Vector3 start_position = new Vector3(start_x, ladder.transform.position.y + 7f, transform.position.z);
        Instantiate(BirdPrefab, start_position, gameObject.transform.rotation);
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
        Instantiate(AnimalPrefab, start_position, gameObject.transform.rotation);
    }

    void CreateSnow()
    {
        Debug.Log("CreateSnow");
        Camera cam = Camera.main;
        float height = 2f * cam.orthographicSize;
        float width = height * cam.aspect;
        //start x cur
        float start_x = Random.Range(-1*(width/2), width/2);
        Vector3 start_position = new Vector3(start_x, height/2, transform.position.z);
        Instantiate(BgsnowPrefab, start_position, gameObject.transform.rotation);
    }
}
