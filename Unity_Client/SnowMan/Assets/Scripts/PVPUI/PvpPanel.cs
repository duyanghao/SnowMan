using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Collections;
using System.Net.Mime;
using Google.ProtocolBuffers;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PvpPanel : MonoBehaviour
{

    public GameObject login_panel;
    public GameObject register_panel;
    public GameObject register_succeed_panel;
    public GameObject match_panel;
    public GameObject match_timeout_panel;
    public GameObject fight_panel;
    public GameObject exit_panel;
    public GameObject cancel_panel;
    public GameObject fight_exit_panel;
    public GameObject pk_panel;
    //wait icon
    public GameObject wait_prefab;
    //socket instance
    private SocketHelper socket_instance = SocketHelper.GetInstance();
    public static CodeBattle.Userinfo_Frame userinfo;
    public static CodeBattle.Userinfo_Frame enemyinfo;
    private static string ip;
    private static CodeBattle.Login_Frame logininfo;
    private static bool first = true;
    private GameObject wait_icon;
    //match time
    public float match_time;
    private float match_count;
    //private bool is_matched;
    private bool start_matched;
    //pk panel time
    public float pk_time;
    private float pk_count;
    private bool is_pk;
    //snow sound
    private pvp_audio audio;
    //whether logininfo has sent
    private bool is_sent;

    // Use this for initialization
    void Start () {
        Time.timeScale = 1.0f;
        //...
        if (!first)
        {
            AgainloginRequest();
        }
        else
        {
            InitPanel();
            ip = GetInternalIP();
            first = false;
        }
        //init the match
        //is_matched = false;
        start_matched = false;
        match_count = 0f;
        //pk
        pk_count = 0f;
        is_pk = false;
        //sound
        audio = (GameObject.FindWithTag("pvp_audio")).GetComponent<pvp_audio>();
        //reset is_sent
        is_sent = false;
    }

    private string GetInternalIP()
    {
        IPHostEntry host;
        string localIP = "";
        host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (IPAddress ip in host.AddressList)
        {
            if (ip.AddressFamily.ToString() == "InterNetwork")
            {
                localIP = ip.ToString();
                if (localIP != "127.0.0.1")
                {
                    break;
                }
            }
        }
        return localIP;
    }

    // Update is called once per frame
    void Update () {
        //loop pk
        if (is_pk)
        {
            Debug.Log("enter is_pk:" + pk_count);
            pk_count += Time.deltaTime;
            if (pk_count>=pk_time)
            {
                SwitchToPvpScenes();
            }
        }
        //receive frame
	    if (start_matched)
	    {
            if (socket_instance.is_connected)
            {
                //Receive_frame();
                // Wating for Param#1 Microseconds to check is there any data send from server.
                // 1 second == 1000 Millisecond == 1000 * 1000 Microseconds
                if (socket_instance.socket.Poll(10000, SelectMode.SelectRead))
                {
                    try
                    {
                        Receive_match_frame();
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("Receive_match_frame failure:" + e);
                        socket_instance.is_connected = false;
                        return;
                    }
                }
                else
                {
                    match_count += Time.deltaTime;
                }
            }
	        if (match_count>=match_time)
	        {
	            SwitchMatchTimeoutPanel();
	        }
        }
    }

    private void Receive_match_frame()
    {
        //switch to pvp scenes
        start_matched = false;
        match_count = 0f;
        //receive userinfo and enemyinfo
        byte[] info_bytes = socket_instance.ReceiveMessage();
        CodeBattle.Totalinfo_Frame total_info = BytesToTotalinfoFrame(info_bytes);
        if (total_info.Preinfo.Ip != ip)
        {
            enemyinfo = total_info.Preinfo;
        }
        else if (total_info.Laterinfo.Ip != ip)
        {
            enemyinfo = total_info.Laterinfo;
        }
        else
        {
            Debug.LogError("invalid totalinfo received:" + total_info);
        }
        Debug.Log("totalinfo received:" + total_info);
        //set text
        pk_panel.transform.FindChild("player_info").gameObject.GetComponent<Text>().text = userinfo.Username;
        pk_panel.transform.FindChild("enemy_info").gameObject.GetComponent<Text>().text = enemyinfo.Username;
        //set true
        is_pk = true;
        pk_count = 0f;
        Debug.Log("Switch to pk panel"+ is_pk);
        SwitchToPkPanel();
    }

    public void SwitchToPkPanel()
    {
        //play pk sound
        audio.PlayOneShotIndex(0);
        //destory the wait icon
        Destroy(wait_icon);
        //switch to pk panel
        fight_panel.SetActive(false);
        fight_exit_panel.SetActive(false);
        exit_panel.SetActive(false);
        cancel_panel.SetActive(false);
        login_panel.SetActive(false);
        register_panel.SetActive(false);
        register_succeed_panel.SetActive(false);
        match_panel.SetActive(false);
        match_timeout_panel.SetActive(false);
        pk_panel.SetActive(true);
    }

    public void SwitchMatchTimeoutPanel()
    {
        //reset match setting
        start_matched = false;
        match_count = 0f;
        //switch to match-timeout panel
        pk_panel.SetActive(false);
        fight_panel.SetActive(false);
        fight_exit_panel.SetActive(false);
        exit_panel.SetActive(false);
        cancel_panel.SetActive(false);
        login_panel.SetActive(false);
        register_panel.SetActive(false);
        register_succeed_panel.SetActive(false);
        match_panel.SetActive(true);
        match_timeout_panel.SetActive(true);
    }

    public void SureTimeoutBtnClick()
    {
        //sure to timeout and back to fight panel
        CancelSureBtnClick();
    }

    public void InitPanel()
    {
        //init the panel
        pk_panel.SetActive(false);
        match_panel.SetActive(false);
        match_timeout_panel.SetActive(false);
        fight_panel.SetActive(false);
        fight_exit_panel.SetActive(false);
        exit_panel.SetActive(false);
        cancel_panel.SetActive(false);
        register_panel.SetActive(false);
        register_succeed_panel.SetActive(false);
        login_panel.SetActive(true);
    }

    public byte[] LoginFrameToBytes(CodeBattle.Login_Frame loginframe)
    {
        //...
        byte[] buffer = new byte[loginframe.SerializedSize];
        CodedOutputStream stream = CodedOutputStream.CreateInstance(buffer);
        loginframe.WriteTo(stream);
        byte[] binary = new byte[buffer.Length + 4];
        int len = IPAddress.HostToNetworkOrder(buffer.Length);
        byte[] lenBytes = BitConverter.GetBytes(len);
        lenBytes.CopyTo(binary, 0);
        buffer.CopyTo(binary, 4);
        return binary;
    }

    public CodeBattle.Login_Response BytesToLoginResponse(byte[] bytes)
    {
        //...
        CodedInputStream stream = CodedInputStream.CreateInstance(bytes);
        CodeBattle.Login_Response loginresponse = CodeBattle.Login_Response.ParseFrom(stream);
        return loginresponse;
    }

    public CodeBattle.Userinfo_Frame BytesToUserinfoFrame(byte[] bytes)
    {
        //...
        CodedInputStream stream = CodedInputStream.CreateInstance(bytes);
        CodeBattle.Userinfo_Frame userinfoframe = CodeBattle.Userinfo_Frame.ParseFrom(stream);
        return userinfoframe;
    }

    public CodeBattle.Totalinfo_Frame BytesToTotalinfoFrame(byte[] bytes)
    {
        //...
        CodedInputStream stream = CodedInputStream.CreateInstance(bytes);
        CodeBattle.Totalinfo_Frame totalinfoframe = CodeBattle.Totalinfo_Frame.ParseFrom(stream);
        return totalinfoframe;
    }
    public void Send_frame(byte[] bytes)
    {
        socket_instance.SendMessage(bytes);
    }

    //again login request
    public void AgainloginRequest()
    {
        //close session socket
        socket_instance.Closed();
        //create login socket
        socket_instance.CreateLoginSocket();
        //send the user and pwd
        byte[] bytes = LoginFrameToBytes(logininfo);
        Send_frame(bytes);
        Debug.Log("again login sent:" + logininfo);
        //receive the response
        byte[] response_bytes = socket_instance.ReceiveMessage();
        CodeBattle.Login_Response loginresponse = BytesToLoginResponse(response_bytes);
        if (!loginresponse.Succeed)
        {
            Debug.LogError("again login failure");
        }
        //call SwitchFightBtnClick while login succeed(set text info before call)
        byte[] info_bytes = socket_instance.ReceiveMessage();
        userinfo = BytesToUserinfoFrame(info_bytes);
        //set userinfo
        string info_str = string.Format("用户id:{0} 用户名:{1} 胜场数:{2} 败场数:{3} 胜率:{4}",
            userinfo.Id, userinfo.Username, userinfo.Winnumbers, userinfo.Losenumbers,
            userinfo.Winrate);
        fight_panel.transform.FindChild("user_info").gameObject.GetComponent<Text>().text = info_str;
        //close the socket
        socket_instance.Closed();
        //create the session socket
        socket_instance.CreateNormalSocket();
        //switch to fight panel
        SwitchFightBtnClick();
    }

    //first login request
    public void LoginSureBtnClick()
    {
        //login logical
        //get input field user and pwd
        GameObject tmp = login_panel.transform.FindChild("username").gameObject;
        CodeBattle.Login_Frame.Builder loginframeBuilder = new CodeBattle.Login_Frame.Builder();
        loginframeBuilder.Username = "";
        loginframeBuilder.Username = tmp.GetComponent<InputField>().text;
        tmp = login_panel.transform.FindChild("password").gameObject;
        loginframeBuilder.Password = "";
        loginframeBuilder.Password = tmp.GetComponent<InputField>().text;
        loginframeBuilder.Login = true;
        loginframeBuilder.Ip = ip;
        logininfo = loginframeBuilder.BuildPartial();
        //Debug.Log("login:"+logininfo);
        //send the user and pwd
        byte[] bytes = LoginFrameToBytes(logininfo);
        Send_frame(bytes);
        Debug.Log("login sent:" + logininfo);
        //receive the response
        byte[] response_bytes = socket_instance.ReceiveMessage();
        CodeBattle.Login_Response loginresponse = BytesToLoginResponse(response_bytes);
        Debug.Log("loginresponse:" + loginresponse);
        if (!loginresponse.Succeed)
        {
            switch (loginresponse.Errcode)
            {
                case 1:
                    //user has not existed yet
                    login_panel.transform.FindChild("error_profmt").gameObject.GetComponent<Text>().text =
                        string.Format("username: {0} has not existed yet,please check!", logininfo.Username);
                    break;
                case 2:
                    //password is not correct
                    login_panel.transform.FindChild("error_profmt").gameObject.GetComponent<Text>().text =
                        "password is not correct,please check!";
                    break;
                default:
                    Debug.LogError("invalid errcode" + loginresponse.Errcode);
                    break;

            }
            //close the socket
            socket_instance.Closed();
            //create the login socket
            socket_instance.CreateLoginSocket();
        }
        else
        {
            //call SwitchFightBtnClick while login succeed(set text info before call)
            byte[] info_bytes = socket_instance.ReceiveMessage();
            userinfo = BytesToUserinfoFrame(info_bytes);
            Debug.Log("userinfo:" + userinfo);
            //set userinfo
            string info_str = string.Format("用户id:{0} 用户名:{1} 胜场数:{2} 败场数:{3} 胜率:{4}",
                userinfo.Id, userinfo.Username, userinfo.Winnumbers, userinfo.Losenumbers,
                userinfo.Winrate);
            fight_panel.transform.FindChild("user_info").gameObject.GetComponent<Text>().text = info_str;
            //close the socket
            socket_instance.Closed();
            //create the session socket
            socket_instance.CreateNormalSocket();
            //switch to fight panel
            SwitchFightBtnClick();
        }
    }

    public void RegisterSureBtnClick()
    {
        //login logical
        //get input field user and pwd
        GameObject tmp = register_panel.transform.FindChild("username").gameObject;
        CodeBattle.Login_Frame.Builder loginframeBuilder = new CodeBattle.Login_Frame.Builder();
        loginframeBuilder.Username = "";
        loginframeBuilder.Username = tmp.GetComponent<InputField>().text;
        tmp = register_panel.transform.FindChild("password").gameObject;
        loginframeBuilder.Password = "";
        loginframeBuilder.Password = tmp.GetComponent<InputField>().text;
        loginframeBuilder.Login = false;
        loginframeBuilder.Ip = ip;
        logininfo = loginframeBuilder.BuildPartial();

        //check validation
        if (logininfo.Username == "" || logininfo.Password == "")
        {
            register_panel.transform.FindChild("error_profmt").gameObject.GetComponent<Text>().text =
                "username or password is empty,please check!";
            return;
        }
        if (logininfo.Password.Length < 8)
        {
            register_panel.transform.FindChild("error_profmt").gameObject.GetComponent<Text>().text =
                "password length is less than 8,please check!";
            return;
        }
        Debug.Log("login:"+logininfo);
        //send the user and pwd
        byte[] bytes = LoginFrameToBytes(logininfo);
        Send_frame(bytes);
        Debug.Log("register sent:" + logininfo);
        //receive the response
        byte[] response_bytes = socket_instance.ReceiveMessage();
        CodeBattle.Login_Response loginresponse = BytesToLoginResponse(response_bytes);
        Debug.Log("loginresponse:" + loginresponse);
        if (!loginresponse.Succeed)
        {
            switch (loginresponse.Errcode)
            {
                case 1:
                    //user has existed already
                    register_panel.transform.FindChild("error_profmt").gameObject.GetComponent<Text>().text =
                        string.Format("username: {0} has existed already,please check!", logininfo.Username);
                    break;
                default:
                    Debug.LogError("invalid errcode" + loginresponse.Errcode);
                    break;

            }
            //close the socket
            socket_instance.Closed();
            //create the login socket
            socket_instance.CreateLoginSocket();
        }
        else
        {
            //switch to register succeed panel
            SwitchRegisterSucceedPanel();
        }
    }

    public void LoginRegisterBtnClick()
    {
        //reset the register error_profmt
        register_panel.transform.FindChild("error_profmt").gameObject.GetComponent<Text>().text = "";
        //switch to register panel
        pk_panel.SetActive(false);
        match_panel.SetActive(false);
        match_timeout_panel.SetActive(false);
        fight_panel.SetActive(false);
        fight_exit_panel.SetActive(false);
        exit_panel.SetActive(false);
        cancel_panel.SetActive(false);
        login_panel.SetActive(false);
        register_succeed_panel.SetActive(false);
        register_panel.SetActive(true);
    }

    public void SwitchRegisterSucceedPanel()
    {
        //switch to register succeed panel
        pk_panel.SetActive(false);
        match_panel.SetActive(false);
        match_timeout_panel.SetActive(false);
        fight_panel.SetActive(false);
        fight_exit_panel.SetActive(false);
        exit_panel.SetActive(false);
        cancel_panel.SetActive(false);
        login_panel.SetActive(false);
        register_panel.SetActive(true);
        register_succeed_panel.SetActive(true);
    }

    public void RegisterCancelBtnClick()
    {
        //reset the login error_profmt
        login_panel.transform.FindChild("error_profmt").gameObject.GetComponent<Text>().text = "";
        //back to login panel
        InitPanel();
    }
   
    public void ExitBtnClick()
    {
        //exit the game?
        pk_panel.SetActive(false);
        match_panel.SetActive(false);
        match_timeout_panel.SetActive(false);
        fight_panel.SetActive(false);
        fight_exit_panel.SetActive(false);
        cancel_panel.SetActive(false);
        register_panel.SetActive(false);
        register_succeed_panel.SetActive(false);
        login_panel.SetActive(true);
        exit_panel.SetActive(true);
    }

    public void ExitSureBtnClick()
    {
        //exit the game
        ExitGame();
    }

    public void ExitCancelBtnClick()
    {
        //back to user and pwd
        InitPanel();
    }

    public void SwitchFightBtnClick()
    {
        //switch to fight panel(add text before called)
        pk_panel.SetActive(false);
        match_panel.SetActive(false);
        match_timeout_panel.SetActive(false);
        cancel_panel.SetActive(false);
        exit_panel.SetActive(false);
        register_panel.SetActive(false);
        register_succeed_panel.SetActive(false);
        login_panel.SetActive(false);
        fight_exit_panel.SetActive(false);
        fight_panel.SetActive(true);
    }

    public void FightExitBtnClick()
    {
        //switch to fight_exit panel(add text before called)
        pk_panel.SetActive(false);
        match_panel.SetActive(false);
        match_timeout_panel.SetActive(false);
        cancel_panel.SetActive(false);
        exit_panel.SetActive(false);
        register_panel.SetActive(false);
        register_succeed_panel.SetActive(false);
        login_panel.SetActive(false);
        fight_exit_panel.SetActive(true);
        fight_panel.SetActive(true);
    }

    public void FightExitCancelBtnClick()
    {
        //back to fight panel
        SwitchFightBtnClick();
    }

    public void FightSureBtnClick()
    {
        //switch to match panel
        pk_panel.SetActive(false);
        cancel_panel.SetActive(false);
        exit_panel.SetActive(false);
        register_panel.SetActive(false);
        register_succeed_panel.SetActive(false);
        login_panel.SetActive(false);
        fight_exit_panel.SetActive(false);
        fight_panel.SetActive(false);
        match_timeout_panel.SetActive(false);
        match_panel.SetActive(true);
        //generate wait_icon
        wait_icon = Instantiate(wait_prefab);
        //match
        //is_matched = false;
        start_matched = true;
        match_count = 0f;
        //socket
        //...
        //send loginframe to session socket
        if (!is_sent)
        {
            byte[] session_bytes = LoginFrameToBytes(logininfo);
            Send_frame(session_bytes);
            Debug.Log("login sent:" + logininfo);
        }       
        is_sent = true;
    }

    public void MatchCancelBtnClick()
    {
        //stop the time_count
        start_matched = false;

        //switch to cancel panel
        pk_panel.SetActive(false);
        exit_panel.SetActive(false);
        register_panel.SetActive(false);
        register_succeed_panel.SetActive(false);
        login_panel.SetActive(false);
        fight_exit_panel.SetActive(false);
        fight_panel.SetActive(false);
        match_timeout_panel.SetActive(false);
        match_panel.SetActive(true);
        cancel_panel.SetActive(true);
    }

    public void CancelSureBtnClick()
    {
        //destory the wait icon
        Destroy(wait_icon);
        //switch to fight panel
        SwitchFightBtnClick();
    }

    public void CancelCancelBtnClick()
    {
        //set the time_count(corresponds to MatchCancelBtnClick)
        start_matched = true;

        //switch to match panel
        pk_panel.SetActive(false);
        cancel_panel.SetActive(false);
        exit_panel.SetActive(false);
        register_panel.SetActive(false);
        register_succeed_panel.SetActive(false);
        login_panel.SetActive(false);
        fight_exit_panel.SetActive(false);
        fight_panel.SetActive(false);
        match_timeout_panel.SetActive(false);
        match_panel.SetActive(true);
    }

    public void SwitchToPvpScenes()
    {
        //switch to pvp scene
        Debug.Log("load pvp-demo");
        SceneManager.LoadScene("pvp-demo");
    }

    public void ExitGame()
    {
        Debug.Log("exit the game");
        //Application.Quit();
        //SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        Application.Quit();
    }
}
