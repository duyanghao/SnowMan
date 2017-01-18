using UnityEngine;
using System.Collections;

public class BackGround_snow : MonoBehaviour
{
    //ladder
    private GameObject ladder;
    // Use this for initialization
    void Start () {
        //...
        ladder = GameObject.FindWithTag("ladder");
        if (!ladder)
        {
            Debug.LogError("invalid ladder,please check!");
        }
    }
	
	// Update is called once per frame
	void Update () {
        if (transform.position.y < ladder.transform.position.y)
        {
            //Debug.Log("snow position:"+transform.position);
            Destroy(gameObject);
        }
        transform.Rotate(new Vector3(0, 0, 90) * Time.deltaTime);
    }

    
}
