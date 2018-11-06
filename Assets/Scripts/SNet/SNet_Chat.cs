/*! 
@author EasyMOBA <easymoba.com>
@lastupdate 13 February 2018
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Facepunch.Steamworks;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Text;

/// <summary>
/// Controls the chat in lobby.
/// I wish I can use the original chat system of Steam but latest version (13 February) Facepunch Steamworks has some problems on chat.
/// If you upgrade your Facepunch Steamworks and it's fixed,
/// Use Client.Instance.Lobby.SendChatMessage at line 71, and listen Client.Instance.Lobby.OnChatMessageRecieved at SNet_Network
/// </summary>
public class SNet_Chat : MonoBehaviour
{
    /// <summary>
    /// Chat message
    /// </summary>
    public class CM : SNet_Network.SNetMessage
    {
        public CM(string _text)
        {
            t = Encoding.UTF8.GetBytes(_text);
        }

        public byte[] t;
    }

    /// <summary>
    /// Incoming text message
    /// </summary>
    /// <param name="value">A double string array. value[0] is the message in json format, value[1] is the sender's steam id</param>
    public void OnCM(string[] value)
    {
        CM readMessage = SNet_Network.SNetMessage.ReadMessage<CM>(value[0]);
        readMessage.s = ulong.Parse(value[1]);

        if (SNet_Manager.instance.lobbyMemberHolder.Find(readMessage.s.ToString()).Find("voiceDisable").GetComponent<Toggle>().isOn)
            return;

        Transform newM = Instantiate(chatGrid.GetChild(0), chatGrid);
        newM.GetComponentInChildren<Facepunch_Avatar>().Fetch(readMessage.s);
        newM.GetComponentInChildren<Text>().text = Client.Instance.Friends.GetName(readMessage.s) + ": " + Encoding.UTF8.GetString(readMessage.t);
        newM.gameObject.GetComponent<UIVisibility>().Open();
    }

	public static SNet_Chat instance;
	
	public UIVisibility chat_Typer;
    public InputField chat_Input;
    public UIVisibility panel_Chat;
    public Transform chatGrid;
	// Use this for initialization
	void Start ()
    {
		if (instance == null)
		    instance = this;

        SNet_Network.RegisterHandler("CM", OnCM);
    }

    // Update is called once per frame
    void Update() {
        if (!panel_Chat.activeSelf)
            return;

        bool close = Input.GetKeyDown(KeyCode.Escape);

        if (Input.GetKeyDown(KeyCode.Return) || close)
        {
            chat_Typer.Open ((close) ? false : !chat_Typer.activeSelf);

            if (!chat_Typer.activeSelf)
            {
                EventSystem.current.SetSelectedGameObject(null);

                if (!close && !string.IsNullOrEmpty(chat_Input.text))
                {
                    SNet_Network.instance.Send_Message(new CM(chat_Input.text));
                }

                chat_Input.text = "";
            }
            else
            {
                EventSystem.current.SetSelectedGameObject(chat_Input.gameObject);
            }
        }
    }
}
