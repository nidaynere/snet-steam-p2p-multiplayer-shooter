/*! 
@author EasyMOBA <easymoba.com>
@lastupdate 13 February 2018
*/

using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;
using System.Text;
using Facepunch.Steamworks;
using System.Linq;
using System.IO.Compression;
using System.IO;

/// <summary>
/// SNet_Messages and its parameters with high value of bytes may not be named well.
/// For example InventoryStatus message named IS because the reduce bandwidth.
/// You may know about SNet, uses Json strings as network messages.
/// </summary>

public class SNet_Network
{
    ///<summary>The current SNet_Network instance.</summary>
    public static SNet_Network instance;

    ///<summary>This is the base class of SNet networking. All message classes must inherit this like 'public class NewMessage : SNetMessage'</summary>
    [System.Serializable]
    public class SNetMessage
    {
        public static ulong idStep;

        /// <summary>
        /// Identity, It's required to be filled by host controlled messages. Non-host controlled messages uses 'sender'.
        /// </summary>
        [HideInInspector] 
        public ulong i;
        /// <summary>
        /// This is the sender id.
        /// </summary>
        [HideInInspector]
        public ulong s;
        /// <summary>
        /// Serialized message name
        /// </summary>
        [HideInInspector]
        public string n;

        public string Name()
        {
            return GetType().Name;
        }

        public static T ReadMessage<T>(string json)
        {
            return JsonUtility.FromJson<T>(json);
        }

        public static string GetMessage(SNetMessage message)
        {
            message.n = message.Name();
            return JsonUtility.ToJson(message);
        }
    }

    ///<summary>Only host can call use this while spawning a networking prefab.</summary>
    [System.Serializable]
    public class Spawn : SNetMessage
    {
        public Spawn(string _prefab, Vector3 _position, Quaternion _rotation, Vector3 _scale, float _forceForward, bool customId = false)
        {
            if (!customId)
            {
                i = idStep++;
            }

            b = _prefab;
            p = _position;
            r = _rotation;
            c = _scale;
            f = _forceForward;
        }

        /// <summary>
        /// Prefab name will be instanced.
        /// </summary>
        public string b;
        /// <summary>
        /// Starting position
        /// </summary>
        public Vector3 p;
        /// <summary>
        /// Starting rotation
        /// </summary>
        public Quaternion r;
        /// <summary>
        /// Starting scale
        /// </summary>
        public Vector3 c;
        /// <summary>
        /// Rigibody force if prefab has rigidbody.
        /// </summary>
        public float f; // Rigidbody force
    }

    ///<summary>Used when players request for spawning player controllers</summary>
    [System.Serializable]
    public class Spawn_Player : Spawn
    {
        public Spawn_Player(string _prefab, Vector3 _position, Quaternion _rotation, Vector3 _scale, float _forceForward) : base(_prefab, _position, _rotation, _scale, _forceForward)
        {
            i = Client.Instance.SteamId;
            b = _prefab;
            p = _position;
            r = _rotation;
            c = _scale;
            f = _forceForward;
        }
    }

    ///<summary>Used when players request for spawning items</summary>
    [System.Serializable]
    public class Spawn_Request : Spawn
    {
        public Spawn_Request(string _prefab, Vector3 _position, Quaternion _rotation, Vector3 _scale, float _forceForward) : base(_prefab, _position, _rotation, _scale, _forceForward)
        {
            b = _prefab;
            p = _position;
            r = _rotation;
            c = _scale;
            f = _forceForward;
        }
    }

    ///<summary>Used when spawning items</summary>
    [System.Serializable]
    public class Spawn_Item : Spawn
    {
        public Spawn_Item(string _prefab, Vector3 _position, Quaternion _rotation, Vector3 _scale, ushort _ammo) : base(_prefab, _position, _rotation, _scale, 0)
        {
            i = idStep++;
            b = _prefab;
            p = _position;
            r = _rotation;
            c = _scale;
            a = _ammo;
        }

        /// <summary>
        /// Current ammo
        /// </summary>
        public ushort a;
    }

    [System.Serializable]
    public class Destroyed : SNetMessage
    {
        public Destroyed (ulong _identity)
        {
            i = _identity;
        }
    }

    [System.Serializable]
    public class Game_Status : SNetMessage
    {
        public Game_Status(bool isStarted, string level)
        {
            iS = isStarted;
            l = level;
        }

        public Game_Status()
        {

        }

        /// <summary>
        /// Is the game started?
        /// </summary>
        public bool iS;

        /// <summary>
        /// Game level
        /// </summary>
        public string l;

        public static void Update_Game_Status(bool isStarted, string level)
        {
            instance.Send_Message(new Game_Status(isStarted, level));
        }
    }

    public static Dictionary<string, NetMessage> handlers = new Dictionary<string, NetMessage>();

    /// <summary>
    /// Current host means the controller of the game. Syncs non-player rigidbodies. Controls weapon & explosive hits.
    /// </summary>
    public static ulong currentHost;

    public bool isHost (ulong steamid = 0)
    {
        try
        {
            int i = Client.Instance.Lobby.GetMemberIDs().Length;
            if (i == 0)
                return false;

            return (currentHost == ((steamid != 0) ? steamid : Client.Instance.SteamId));
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// All players must send this to host to spawn something. Because only host can spawn.
    /// </summary>
    /// <param name="prefabName">Prefabname to spawn, must be placed in Resources/Spawnables</param>
    /// <param name="position">Spawn position</param>
    /// <param name="rotation">Spawn rotation</param>
    /// <param name="scale">Spawn scale</param>
    /// <param name="forceForward">Start force forward</param>
    public void SpawnRequest(string prefabName, Vector3 position, Quaternion rotation, Vector3 scale, float forceForward = 0) // In Resources/Spawnables/ *Only the host can send this
    {
        // Send the request to host.
        Send_Message(new Spawn_Request(prefabName, position, rotation, scale, forceForward), currentHost);
    }

    /// <summary>
    /// Host send this to all clients to spawn a networked prefab.
    /// </summary>
    /// <param name="prefabName">Prefabname to spawn, must be placed in Resources/Spawnables</param>
    /// <param name="position">Spawn position</param>
    /// <param name="rotation">Spawn rotation</param>
    /// <param name="scale">Spawn scale</param>
    /// <param name="forceForward">Start force forward</param>
    public void Send_Spawn (string prefabName, Vector3 position, Quaternion rotation, Vector3 scale, float forceForward = 0) // In Resources/Spawnables/ *Only the host can send this
    {
        Send_Message(new Spawn(prefabName, position, rotation, scale, forceForward));
    }

    /// <summary>
    /// Host send this to all clients, when a player spawned.
    /// </summary>
    /// <param name="prefabName"></param>
    /// <param name="position"></param>
    /// <param name="rotation"></param>
    /// <param name="scale"></param>
    public void SpawnPlayer (string prefabName, Vector3 position, Quaternion rotation, Vector3 scale) // In Resources/Spawnables/
    {
        Send_Message(new Spawn_Player(prefabName, position, rotation, scale, 0));
    }

    /// <summary>
    /// Sender is me?
    /// </summary>
    /// <param name="sender">Sender's steam id.</param>
    /// <returns></returns>
    public static bool Authorized (ulong sender)
    {
        if (sender == Client.Instance.SteamId) // Owner didnt need Sync
            return false;

        return true;
    }

    GameObject Receiver = null;
    public SNet_Network (GameObject receiver)
    {
        instance = this;

        Receiver = receiver;

        // Init voice
        SNet_Voice.instance.Init();

        Client.Instance.Networking.SetListenChannel(0, true); // Default game data
        Client.Instance.Networking.SetListenChannel(1, true); // Voice

        Client.Instance.Networking.OnP2PData = (steamid, bytes, length, channel) =>
        {
            if (channel == 1)
            {
                // Raw voice message
                SNet_Voice.instance.OnVM(bytes, length, steamid);
            }
            else
            Incoming_Message(bytes, length, steamid);
        };

        Client.Instance.Networking.OnIncomingConnection = (steamid) =>
        {
            Debug.Log("Incoming p2p connection from: " + steamid);
            /*
             * Only players in my lobby is allowed;
             * */
            return (Client.Instance.Lobby.GetMemberIDs().ToList().FindIndex(x => x == steamid) != -1);
        };

        Client.Instance.Networking.OnConnectionFailed = (steamid, error) =>
        {
            Debug.Log("Connection Error: " + steamid + " - " + error);
        };

        Client.Instance.Lobby.OnLobbyDataUpdated = () =>
        {
            Receiver.SendMessage("MemberUpdate");
        };

        Client.Instance.LobbyList.OnLobbiesUpdated = () =>
        {
            Debug.Log("Lobby count: " + Client.Instance.LobbyList.Lobbies.Count);

            Receiver.SendMessage("OnLobbyList");
        };

        Client.Instance.Lobby.OnLobbyJoined = (bool val) =>
        {
            Client.Instance.Lobby.SetMemberData("host", "false");

            Receiver.SendMessage("OnLobbyJoined", val);
        };

        Client.Instance.Lobby.OnLobbyCreated = (bool val) =>
        {
            Client.Instance.Lobby.SetMemberData("host", "false");

            Receiver.SendMessage("OnLobbyCreated", val);
        };

        Client.Instance.Lobby.OnChatStringRecieved = (steamid, text) =>
        {
            Debug.Log(steamid + " " + text);
        };

        SNet_Auth.Init();
        SNet_Controller.list.Clear();
    }

    public void CreateLobby (Lobby.Type lobbyType, int maxMembers)
    {
        Debug.Log("CreateLobby() " + lobbyType + " " + maxMembers);
        Client.Instance.Lobby.Create(lobbyType, maxMembers);
    }

    public void LeaveLobby()
    {
        Debug.Log("LeavyLobby()");
        Client.Instance.Lobby.Leave();
        Receiver.SendMessage("OnLobbyLeave");
    }

    public void RefreshLobby()
    {
        Debug.Log("RefreshLobby()");
        Client.Instance.LobbyList.Refresh();
    }

    public delegate void NetMessage(string[] value);
    /// <summary>
    /// Add a SNet_Message handler with a custom receiver. 
    /// value[0] is json deserialized json string, value[1] is the sender.
    /// </summary>
    /// <param name="name">Name of the SNet_Message inherited class</param>
    /// <param name="receiver">Receiver game object.</param>
    public static void RegisterHandler (string name, NetMessage callback)
    { // New handler with a custom receiver
        handlers.Add(name, callback);
    }

    public int incoming = 0;

    public void Incoming_Message(byte[] message, int length, ulong steamid)
    {
        incoming += length;

        message = Extensions.Get(message, 0, length);
        message = CLZF2.Decompress (message);
        string json = Encoding.UTF8.GetString(message, 0, message.Length);

        try
        {
            SNetMessage readMessage = JsonUtility.FromJson<SNetMessage>(json);

            string[] data = new string[2];
            data[0] = json;
            data[1] = steamid.ToString();

            if (handlers.ContainsKey(readMessage.n)) // If there is a handler
                handlers[readMessage.n] (data);
            else Debug.Log("Unknown message type: " + readMessage.n);
        }
        catch
        {

        }
    }

    /// <summary>
    /// Send message to all players in lobby.
    /// </summary>
    /// <param name="message">Message to send</param>
    /// <param name="specificuser">If you want to send this to a specific user. Enter its steamid.</param>
    public void Send_Message(SNetMessage message, ulong specificuser = 0, Networking.SendType type = Networking.SendType.Unreliable, int channel = 0)
    {
        if (Client.Instance == null || !Client.Instance.Lobby.IsValid)
            return;

        string json = SNetMessage.GetMessage(message);
        byte[] data = Encoding.UTF8.GetBytes( json );

        ulong[] members = Client.Instance.Lobby.GetMemberIDs();
        if (specificuser != 0)
            members = new ulong[1] { specificuser };

        int membersLength = members.Length;

        bool validationRequired = false;
        if (message.n != "Auth_H")
        {
            validationRequired = true;
        }

        data = CLZF2.Compress(data);

        for (int i = 0; i < membersLength; i++)
        {
            if (!SNet_Auth.current.validatedIds.Contains(members[i]) && validationRequired)
                continue; // Skip this user. Hes not validated.

            Client.Instance.Networking.SendP2PPacket(members[i], data, data.Length, type, channel);
        }
    }
}
