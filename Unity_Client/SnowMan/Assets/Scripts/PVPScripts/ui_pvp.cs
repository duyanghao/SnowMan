using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Collections;
using System.Net.Mime;
using Google.ProtocolBuffers;
//using UnityEditor;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ui_pvp : MonoBehaviour {
    //socket generate
    private GameObject _socket;
    private SocketGenerate socket_generate;
    //external ui
    public GameObject player_info_panel;
    public GameObject direction_panel;
    public GameObject operation_panel;
    public GameObject player_win_panel;
    public GameObject player_lose_panel;
    
    //player
    private PvpPlayer pvpplayer;
    //attack time
    public float attack_time;
    private float attack_count;
    private bool is_attack;

    // Use this for initialization
    void Start () {
        _socket = GameObject.FindWithTag("MainCamera");
        if (!_socket)
        {
            Debug.LogError("no socketgenerate available");
        }
        socket_generate = _socket.GetComponent<SocketGenerate>();
        //init the panel
        Init_panel();
        //player
        pvpplayer = GameObject.FindWithTag("player").GetComponent<PvpPlayer>();
        if (!pvpplayer)
        {
            Debug.LogError("no player available");
        }
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

    public void Init_panel()
    {
        player_lose_panel.SetActive(false);
        player_win_panel.SetActive(false);
        player_info_panel.SetActive(true);
        direction_panel.SetActive(true);
        operation_panel.SetActive(true);
        //set player name
        player_info_panel.transform.FindChild("player_name").gameObject.GetComponent<Text>().text =
            PvpPanel.userinfo.Username;
        //set enemy name
        player_info_panel.transform.FindChild("enemy_name").gameObject.GetComponent<Text>().text =
           PvpPanel.enemyinfo.Username;
    }

    //button click
    public void OnMyButtonClickLeft()
    {
        //Debug.Log("left Button click sent");
        //direction builder
        CodeBattle.Move_Direction.Builder movedirectionbuilder = new CodeBattle.Move_Direction.Builder();
        movedirectionbuilder.Left = true;
        movedirectionbuilder.Right = false;
        movedirectionbuilder.Up = false;
        //Position builder
        CodeBattle.Generated_Position.Builder positionbuilder = new CodeBattle.Generated_Position.Builder();
        positionbuilder.X = 0;
        positionbuilder.Y = 0;
        positionbuilder.Z = 0;
        //Client_Frame builder
        CodeBattle.Client_Frame.Builder clientframeBuilder = new CodeBattle.Client_Frame.Builder();
        clientframeBuilder.Ip = SocketGenerate.ip;
        clientframeBuilder.Died = false;
        clientframeBuilder.Moved = true;
        clientframeBuilder.Direction = movedirectionbuilder.BuildPartial();
        clientframeBuilder.Hpchanged = false;
        clientframeBuilder.Playertype = false;
        clientframeBuilder.Changevalue = 0f;
        clientframeBuilder.Generated = false;
        clientframeBuilder.Objecttype = 0;
        clientframeBuilder.Pos = positionbuilder.BuildPartial();
        //
        CodeBattle.Client_Frame tmp = clientframeBuilder.BuildPartial();
        byte[] bytes = socket_generate.Client_FrameToBytes(tmp);
        socket_generate.Send_frame(bytes);
        print("left Button click sent:\n" + tmp);
    }

    public void OnMyButtonClickRight()
    {
        //Debug.Log("right Button click sent");
        //direction builder
        CodeBattle.Move_Direction.Builder movedirectionbuilder = new CodeBattle.Move_Direction.Builder();
        movedirectionbuilder.Left = false;
        movedirectionbuilder.Right = true;
        movedirectionbuilder.Up = false;
        //Position builder
        CodeBattle.Generated_Position.Builder positionbuilder = new CodeBattle.Generated_Position.Builder();
        positionbuilder.X = 0;
        positionbuilder.Y = 0;
        positionbuilder.Z = 0;
        //Client_Frame builder
        CodeBattle.Client_Frame.Builder clientframeBuilder = new CodeBattle.Client_Frame.Builder();
        clientframeBuilder.Ip = SocketGenerate.ip;
        clientframeBuilder.Died = false;
        clientframeBuilder.Moved = true;
        clientframeBuilder.Direction = movedirectionbuilder.BuildPartial();
        clientframeBuilder.Hpchanged = false;
        clientframeBuilder.Playertype = false;
        clientframeBuilder.Changevalue = 0f;
        clientframeBuilder.Generated = false;
        clientframeBuilder.Objecttype = 0;
        clientframeBuilder.Pos = positionbuilder.BuildPartial();
        //
        CodeBattle.Client_Frame tmp = clientframeBuilder.BuildPartial();
        byte[] bytes = socket_generate.Client_FrameToBytes(tmp);
        socket_generate.Send_frame(bytes);
        print("right Button click sent:\n" + tmp);
    }
    public void OnMyButtonClickUp()
    {
        //Debug.Log("up Button click sent");
        //direction builder
        CodeBattle.Move_Direction.Builder movedirectionbuilder = new CodeBattle.Move_Direction.Builder();
        movedirectionbuilder.Left = false;
        movedirectionbuilder.Right = false;
        movedirectionbuilder.Up = true;
        //Position builder
        CodeBattle.Generated_Position.Builder positionbuilder = new CodeBattle.Generated_Position.Builder();
        positionbuilder.X = 0;
        positionbuilder.Y = 0;
        positionbuilder.Z = 0;
        //Client_Frame builder
        CodeBattle.Client_Frame.Builder clientframeBuilder = new CodeBattle.Client_Frame.Builder();
        clientframeBuilder.Ip = SocketGenerate.ip;
        clientframeBuilder.Died = false;
        clientframeBuilder.Moved = true;
        clientframeBuilder.Direction = movedirectionbuilder.BuildPartial();
        clientframeBuilder.Hpchanged = false;
        clientframeBuilder.Playertype = false;
        clientframeBuilder.Changevalue = 0f;
        clientframeBuilder.Generated = false;
        clientframeBuilder.Objecttype = 0;
        clientframeBuilder.Pos = positionbuilder.BuildPartial();
        //
        CodeBattle.Client_Frame tmp = clientframeBuilder.BuildPartial();
        byte[] bytes = socket_generate.Client_FrameToBytes(tmp);
        socket_generate.Send_frame(bytes);
        print("up Button click sent:\n" + tmp);
    }
    public void OnMyButtonClickAttack()
    {
        if (!is_attack)
        {
            return;
        }
        //Debug.Log("attack Button click");
        //direction builder
        CodeBattle.Move_Direction.Builder movedirectionbuilder = new CodeBattle.Move_Direction.Builder();
        movedirectionbuilder.Left = false;
        movedirectionbuilder.Right = false;
        movedirectionbuilder.Up = false;
        //Position builder
        CodeBattle.Generated_Position.Builder positionbuilder = new CodeBattle.Generated_Position.Builder();
        positionbuilder.X = pvpplayer.transform.position.x;
        positionbuilder.Y = pvpplayer.transform.position.y + 1.5f;
        positionbuilder.Z = pvpplayer.transform.position.z;
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
        clientframeBuilder.Objecttype = 1;
        clientframeBuilder.Pos = positionbuilder.BuildPartial();
        //
        CodeBattle.Client_Frame tmp = clientframeBuilder.BuildPartial();
        byte[] bytes = socket_generate.Client_FrameToBytes(tmp);
        socket_generate.Send_frame(bytes);
        print("attack Button click sent:\n" + tmp);

        //set attack
        is_attack = false;
    }

    //show player succeed
    public void PlayerSucceed()
    {
        Time.timeScale = 0.0f;
        direction_panel.SetActive(false);
        operation_panel.SetActive(false);
        player_lose_panel.SetActive(false);
        player_win_panel.SetActive(true);
        player_info_panel.SetActive(true);

        //set player fight info
        int winnumber = PvpPanel.userinfo.Winnumbers + 1;
        int totalnumber = winnumber + PvpPanel.userinfo.Losenumbers;
        double winrate = (winnumber * 100.0)/totalnumber;

        string info_str = string.Format("用户id:{0} 用户名:{1} 胜场数:{2} 败场数:{3} 胜率:{4}",
            PvpPanel.userinfo.Id, PvpPanel.userinfo.Username, winnumber, PvpPanel.userinfo.Losenumbers, (int) winrate);
        player_win_panel.transform.FindChild("player_fight_info").gameObject.GetComponent<Text>().text = info_str;

        //set enemy fight info
        winnumber = PvpPanel.enemyinfo.Winnumbers;
        totalnumber = PvpPanel.enemyinfo.Losenumbers + 1 + winnumber;
        winrate = (winnumber*100.0)/totalnumber;

        info_str = string.Format("用户id:{0} 用户名:{1} 胜场数:{2} 败场数:{3} 胜率:{4}",
            PvpPanel.enemyinfo.Id, PvpPanel.enemyinfo.Username, winnumber, PvpPanel.enemyinfo.Losenumbers + 1, (int) winrate);
        player_win_panel.transform.FindChild("enemy_fight_info").gameObject.GetComponent<Text>().text = info_str;
    }

    //show player failure
    public void PlayerLose()
    {
        Time.timeScale = 0.0f;
        direction_panel.SetActive(false);
        operation_panel.SetActive(false);
        player_win_panel.SetActive(false);
        player_lose_panel.SetActive(true);
        player_info_panel.SetActive(true);

        //set player fight info
        int winnumber = PvpPanel.userinfo.Winnumbers;
        int totalnumber = winnumber + PvpPanel.userinfo.Losenumbers + 1;
        double winrate = (winnumber * 100.0) / totalnumber;

        string info_str = string.Format("用户id:{0} 用户名:{1} 胜场数:{2} 败场数:{3} 胜率:{4}",
            PvpPanel.userinfo.Id, PvpPanel.userinfo.Username, winnumber, PvpPanel.userinfo.Losenumbers + 1, (int)winrate);
        player_lose_panel.transform.FindChild("player_fight_info").gameObject.GetComponent<Text>().text = info_str;

        //set enemy fight info
        winnumber = PvpPanel.enemyinfo.Winnumbers + 1;
        totalnumber = PvpPanel.enemyinfo.Losenumbers + winnumber;
        winrate = (winnumber * 100.0) / totalnumber;

        info_str = string.Format("用户id:{0} 用户名:{1} 胜场数:{2} 败场数:{3} 胜率:{4}",
            PvpPanel.enemyinfo.Id, PvpPanel.enemyinfo.Username, winnumber, PvpPanel.enemyinfo.Losenumbers, (int)winrate);
        player_lose_panel.transform.FindChild("enemy_fight_info").gameObject.GetComponent<Text>().text = info_str;
    }

    public void OnMyButtonClickSure()
    {
        Time.timeScale = 1.0f;
        Debug.Log("restart the fight!");
        SceneManager.LoadScene("pvp-ui");
    }


}
