using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class button_click : MonoBehaviour {

    private GameObject _player;
    private Player player;

    //external panel
    public GameObject direction_panel;
    public GameObject operation_panel;
    public GameObject top_panel;
    public GameObject pause_panel;
    public GameObject quit_panel;
    public GameObject player_win_again;
    public GameObject player_lose_again;

    //attack time
    public float attack_time;
    private float attack_count;
    private bool is_attack;

    // Use this for initialization
    void Start () {
        _player = GameObject.FindWithTag("player");
        if (!_player)
        {
            Debug.LogError("no player available");
        }
        player= _player.GetComponent<Player>();
        //init the ui
        InitPanel();
        //init the attack
        is_attack = true;
        attack_count = 0f;
    }
	
	// Update is called once per frame
	void Update () {
        //...
        if (!is_attack)
        {
            attack_count += Time.deltaTime;
            if (attack_count >= attack_time)
            {
                attack_count = 0f;
                is_attack = true;
            }
        }
    }
    //button click
    public void OnMyButtonClickLeft()
    {
        Debug.Log("left Button click");
        player.Set_Player_MoveState(Player.MoveState.LEFT);
    }

    public void OnMyButtonClickRight()
    {
        Debug.Log("right Button click");
        player.Set_Player_MoveState(Player.MoveState.RIGHT);
    }
    public void OnMyButtonClickUp()
    {
        Debug.Log("up Button click");
        player.Set_Player_MoveState(Player.MoveState.UP);
    }
    public void OnMyButtonClickAttack()
    {
        if (!is_attack)
        {
            return;
        }
        Debug.Log("attack Button click");
        player.Set_Player_OpeState(Player.OpeState.ATTACK);
        //set attack
        is_attack = false;
    }

    //external panel function
    public void InitPanel()
    {
        //start panel
        Time.timeScale = 1.0f;
        quit_panel.SetActive(false);
        pause_panel.SetActive(false);
        direction_panel.SetActive(true);
        operation_panel.SetActive(true);
        //player win-lose panel
        player_win_again.SetActive(false);
        player_lose_again.SetActive(false);
        top_panel.SetActive(true);
    }

    public void PlayerWinAgain()
    {
        //call when player win
        Time.timeScale = 0.0f;
        quit_panel.SetActive(false);
        pause_panel.SetActive(false);
        direction_panel.SetActive(false);
        operation_panel.SetActive(false);
        top_panel.SetActive(false);
        //player win-lose panel
        player_lose_again.SetActive(false);
        player_win_again.SetActive(true);
    }

    public void PlayerLoseAgain()
    {
        //call when player lose
        Time.timeScale = 0.0f;
        quit_panel.SetActive(false);
        pause_panel.SetActive(false);
        direction_panel.SetActive(false);
        operation_panel.SetActive(false);
        top_panel.SetActive(false);
        //player win-lose panel
        player_win_again.SetActive(false);
        player_lose_again.SetActive(true);
    }

    public void FinashAgainBtnClick()
    {
        //restart the game
        Time.timeScale = 1.0f;
        Debug.Log("restart the game!");
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void FinashExitBtnClick()
    {
        QuitSureBtnClick();
    }

    public void TopQuitBtnClick()
    {
        //top-quit button
        Time.timeScale = 0.0f;
        pause_panel.SetActive(false);
        direction_panel.SetActive(false);
        operation_panel.SetActive(false);
        top_panel.SetActive(true);
        quit_panel.SetActive(true);
    }

    public void TopPauseBtnClick()
    {
        //top-pause button
        Time.timeScale = 0.0f;
        top_panel.SetActive(false);
        quit_panel.SetActive(false);
        direction_panel.SetActive(false);
        operation_panel.SetActive(false);
        pause_panel.SetActive(true);
    }

    public void PauseQuitBtnClick()
    {
        //pause-quit button
        Time.timeScale = 0.0f;
        top_panel.SetActive(false);
        direction_panel.SetActive(false);
        operation_panel.SetActive(false);
        pause_panel.SetActive(true);
        quit_panel.SetActive(true);
    }

    public void PauseContinueBtnClick()
    {
        //pause-continue button
        InitPanel();
    }

    public void QuitCancelBtnClick()
    {
        InitPanel();
    }
    public void QuitSureBtnClick()
    {
        Debug.Log("exit the game");
        //Application.Quit();
        //SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        Application.Quit();
    }

}
