/*! 
@author EasyMOBA <easymoba.com>
@lastupdate 13 February 2018
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// General button types for SNet.
/// </summary>
public class SNet_Button : MonoBehaviour
{
    public static SNet_Button SelectedPlayer;

    public ButtonType type;
    public enum ButtonType
    {
        CreateLobby,
        LeaveLobby,
        RefreshLobby,
        ExitGame,
        SelectPlayer,
        Connect,
        StartGame,
        Spawn,
        Quit,
        LevelSelector
    }

    private void Start()
    {
        if (type == ButtonType.CreateLobby)
        {
            ddvalues.Add(0, Facepunch.Steamworks.Lobby.Type.Public);
            ddvalues.Add(1, Facepunch.Steamworks.Lobby.Type.FriendsOnly);
            ddvalues.Add(2, Facepunch.Steamworks.Lobby.Type.Private);
            ddvalues.Add(3, Facepunch.Steamworks.Lobby.Type.Invisible);
        }
    }

    private void OnEnable()
    {
        if (type == ButtonType.SelectPlayer && transform.GetSiblingIndex() == 0)
            OnClick(); // Select the first player by default

        if (type == ButtonType.LevelSelector)
            SNet_Manager.instance.gameStatus.l = SNet_Manager.instance.Button_levelSelector.captionText.text;
    }

    public Dropdown[] values;
    public Dictionary<int, Facepunch.Steamworks.Lobby.Type> ddvalues = new Dictionary<int, Facepunch.Steamworks.Lobby.Type>();

    public void OnClick()
    {
        if (SNet_Network.instance == null)
            return; // SNet_Network is not initialized.

        switch (type)
        {
            case ButtonType.CreateLobby:
                SNet_Network.instance.CreateLobby(ddvalues [values[0].value], int.Parse (values[1].captionText.text));
                break;

            case ButtonType.LeaveLobby:
                SNet_Network.instance.LeaveLobby();
                break;

            case ButtonType.RefreshLobby:
                SNet_Manager.instance.panel_LobbyListLoading.Open();
                SNet_Network.instance.RefreshLobby();
                break;

            case ButtonType.SelectPlayer:
                SelectedPlayer = this;

                foreach (Transform t in transform.parent)
                {
                    t.Find("Selected").gameObject.SetActive(t == transform);
                }
                break;

            case ButtonType.StartGame:
                SNet_Network.Game_Status.Update_Game_Status(!SNet_Manager.instance.gameStatus.iS, SNet_Manager.instance.gameStatus.l);
                break;

            case ButtonType.LevelSelector:
                SNet_Manager.instance.panel_Lobby.transform.Find("back").Find("StartGame").gameObject.SetActive(false);
                SNet_Network.Game_Status.Update_Game_Status(SNet_Manager.instance.gameStatus.iS, SNet_Manager.instance.Button_levelSelector.captionText.text);
                break;

            case ButtonType.Spawn:
                if (SNet_Manager.instance.gameStatus != null && SNet_Manager.instance.gameStatus.iS && SNet_Manager.mapLoaded && SNet_Identity.list.Find(x => x.identity == Facepunch.Steamworks.Client.Instance.SteamId) == null)
                {
                    SNet_Network.instance.SpawnPlayer(SelectedPlayer.gameObject.name, GameMap.getSpawnPosition(), Quaternion.identity, Vector3.one);
                }
                break;
        }
    }
}
