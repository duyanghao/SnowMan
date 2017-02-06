using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class GamePanel : MonoBehaviour {
    
    public GameObject top;
    public GameObject author_info;
    public GameObject select_mode;

    // Use this for initialization
    void Start ()
    {
        //start panel
        CloseBtnClick();
    }
	
	// Update is called once per frame
	void Update () {
	    //...
	}

    public void SelectBtnClick()
    {
        //select the game
        top.SetActive(false);
        author_info.SetActive(false);
        select_mode.SetActive(true);
    }

    public void InfoBtnClick()
    {
        //show author info
        top.SetActive(false);
        select_mode.SetActive(false);
        author_info.SetActive(true);
    }

    public void CloseBtnClick()
    {
        //back to start
        author_info.SetActive(false);
        select_mode.SetActive(false);
        top.SetActive(true);
    }

    public void PvmBtnClick()
    {
        SceneManager.LoadScene("pvm-demo");
    }

    public void PvpBtnClick()
    {
        SceneManager.LoadScene("pvp-ui");
    }

}
