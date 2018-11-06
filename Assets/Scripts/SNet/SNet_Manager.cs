/*! 
@author EasyMOBA <easymoba.com>
@lastupdate 15 February 2018
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Facepunch.Steamworks;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Linq;

/// <summary>
/// This is the main manager of SNet.
/// This class controls most of the network messages.
/// Please see the method summaries.
/// </summary>
public class SNet_Manager : MonoBehaviour
{
    public static SNet_Manager instance;

    private void OnGUI()
    {
        GUILayout.Label ("Total transfer: " + (SNet_Network.instance.incoming/1000f) + " kb.");
    }

    // Use this for initialization
    private void Awake()
    {
        gameStatus = new SNet_Network.Game_Status(); // Reset the game status

        if (instance != null)
        {
            SNet_Auth.Init();
            Destroy(gameObject);
            return;
        }

        gameObject.name = "SteamManager";

        // This will always remain.
        DontDestroyOnLoad(gameObject);

        panel_Loading.Open();
        instance = this;
        nextMemberUpdate = Time.time + 1;

        ///Register the main handlers

        SNet_Network.RegisterHandler("Spawn", OnSpawn);
        SNet_Network.RegisterHandler("Spawn_Request", OnSpawn_Request);
        SNet_Network.RegisterHandler("Spawn_Player", OnSpawn_Player);
        SNet_Network.RegisterHandler("Spawn_Item", OnSpawn_Item);
        SNet_Network.RegisterHandler("Game_Status", OnGame_Status);
        SNet_Network.RegisterHandler("Destroyed", OnDestroyed);
        SNet_Network.RegisterHandler("Impact", OnImpact);
        SNet_Network.RegisterHandler("A_Bool", OnA_Bool);
        SNet_Network.RegisterHandler("A_Float", OnA_Float);
        SNet_Network.RegisterHandler("A_Trigger", OnA_Trigger);
        SNet_Network.RegisterHandler("A_Int", OnA_Int);
        SNet_Network.RegisterHandler("RB", OnRB);
        SNet_Network.RegisterHandler("Pos", OnPos);
        SNet_Network.RegisterHandler("Rot", OnRot);
        SNet_Network.RegisterHandler("IS", OnIS);
        SNet_Network.RegisterHandler("PlayerItem", OnPlayerItem);
        SNet_Network.RegisterHandler("EquipItem", OnEquipItem);
        SNet_Network.RegisterHandler("A", OnA);
        SNet_Network.RegisterHandler("St", OnSt);
        SNet_Network.RegisterHandler("Health", OnHealth);
        SNet_Network.RegisterHandler("UserHit", OnUserHit);
        SNet_Network.RegisterHandler("Exploded", OnExploded);
        SNet_Network.RegisterHandler("V", OnV);
        SNet_Network.RegisterHandler("Interact", OnInteract);
        SNet_Network.RegisterHandler("VI", OnVI);
        SNet_Network.RegisterHandler("VH", OnVH);
        SNet_Network.RegisterHandler("CSRequest", OnCSRequest);
    }

    float nextMemberUpdate;
    private void Update()
    {
        if (nextMemberUpdate < Time.time)
        {
            /*
             * Checking the members, maybe new connections / disconnections.
             * */
            nextMemberUpdate = Time.time + 1;
            MemberUpdate();
        }
    }

    [HideInInspector]
    public int memberCount;

    /// <summary>
    /// If the disconnected lobby owner connects to lobby again, it will be the master client again. But we don't want this.
    /// So, there is a different method. Lobby's member data has "host" variable.
    /// If the current host disconnects, the first member of remaining player in lobby will be the new host.
    /// </summary>
    /// <param name="members">Members list.</param>
    /// <returns>Returns true if the current host missing.</returns>
    private bool HostRequired(List<ulong> members)
    {
        if (members.Contains(SNet_Network.currentHost))
            return false; // Current host is still in game.

        int hostCount = 0;
        foreach (ulong m in members)
        {
            if (SNet_Auth.current.validatedIds.Contains(Client.Instance.SteamId) && !SNet_Auth.current.validatedIds.Contains(m))
                continue;

            if (Client.Instance.Lobby.GetMemberData(m, "host") == "true")
            {
                Debug.Log("Current host received = " + m + ", my steamid: " + Client.Instance.SteamId);
                SNet_Network.currentHost = m;
                UI_UpdateHost();
                hostCount++;
            }
        }

        if (hostCount == 1)
        {
            return false;
        }

        Debug.Log("Required new host");

        /*
         * NEW HOST EASY FIX* START
         * EASY FIX INFO:
         * -NEW HOST REQUIRED-. Let's DELAY ALL of SNet_Transforms & SNet_Rigidbodies because all is going to update with new host.
         * We won't make the Valve's servers angry with a high amount data transfer.
         * */
        foreach (SNet_Transform s in SNet_Transform.list)
        {
            s.nextUpdate_Position = Random.Range(1f, 4f);
            s.nextUpdate_Rotation = Random.Range(1f, 4f);
        }

        foreach (SNet_Rigidbody r in SNet_Rigidbody.list)
        {
            r.nextUpdate = Time.time + Random.Range(1f, 4f);
        }
        /*
         * EASY FIX* END
         * */

        return true;
    }

    /// <summary>
    /// You can see the master client with the icon on lobby UI.
    /// </summary>
    void UI_UpdateHost()
    {
        foreach (Transform t in lobbyMemberHolder)
        {
            t.Find ("host").gameObject.SetActive (SNet_Network.instance.isHost(ulong.Parse(t.gameObject.name)));
        }
    }

    /// <summary>
    /// Wait for ticket response.
    /// </summary>
    [HideInInspector]
    public float nextAuthCheck = 0;
    /// <summary>
    /// Clean blacklist after a while.
    /// </summary>
    [HideInInspector]
    public float nextQueryClean = 0;

    /// <summary>
    /// There may be a new connection or disconnection.
    /// </summary>
    public void MemberUpdate()
    {
        if (Client.Instance == null || !Client.Instance.Lobby.IsValid)
        {// We are not in a lobby.
            memberCount = 0;
            return;
        }

        List<ulong> members = Client.Instance.Lobby.GetMemberIDs().ToList();

        //Clear old validated auths
        List<ulong> _temp = SNet_Auth.current.validatedIds.FindAll(x => !members.Contains(x));
        foreach (ulong i in _temp)
        {
            SNet_Auth.current.validatedIds.Remove(i);
            SNet_Auth.current.query.Remove(i);
        }

        int lobbyC = members.Count;
        if (memberCount != lobbyC)
        { /// A new connection or disconnection.
            Debug.Log("Member Update");

            OnLobbyUpdate(); // update the lobby UI.

            memberCount = lobbyC;

            if (memberCount > 0)
                OnLobbyJoined(true); // Join lobby automatically, if we are not initialized.

            if (HostRequired(members))
            { // Assign new host.
                if (SNet_Auth.current.validatedIds.Count > 0 && SNet_Auth.authed)
                    SNet_Network.currentHost = SNet_Auth.current.validatedIds[0];
                else
                    SNet_Network.currentHost = members[0];
                UI_UpdateHost();
            }

            Client.Instance.Lobby.SetMemberData ("host", (SNet_Network.instance.isHost ()) ? "true" : "false");

            bool isHost = SNet_Network.instance.isHost();
            Button_startGame.interactable = isHost;
            Button_levelSelector.interactable = isHost;

            /*
             * REMOVE DISCONNECTED PLAYERS
             * */
            List<SNet_Controller> missing = SNet_Controller.list.FindAll(x => members.FindIndex(e => e == x.identity.identity) == -1);
            foreach (SNet_Controller sc in missing)
            {
                Destroy(sc.gameObject);
            }
            /*
             * */
        }

        if (SNet_Network.instance.isHost()) // Update the game status for possible new members
        {
            SNet_Network.Game_Status.Update_Game_Status(gameStatus.iS, gameStatus.l);

            if (nextAuthCheck < Time.time)
            {
                foreach (ulong i in members)
                {
                    if (!SNet_Auth.current.validatedIds.Contains(i) && !SNet_Auth.current.query.Contains(i))
                    {
                        Debug.Log("Auth needed: " + i);
                        SNet_Auth.current.query.Add(i);
                        nextAuthCheck = Time.time + 5;
                        nextQueryClean = Time.time + 30;
                        // we are the host and the player has not authed yet.
                        if (SNet_Auth.ticket != null) // Release the current ticket
                            SNet_Auth.ticket.Cancel();

                        SNet_Auth.ticket = Client.Instance.Auth.GetAuthSessionTicket();
                        /// Send the ticket to that connection.
                        SNet_Network.instance.Send_Message (new SNet_Auth.Auth_H(SNet_Auth.ticket.Data), i);
                    }
                }
            }
        }

        ///Clean the auth query
        if (nextQueryClean != 0 && nextQueryClean < Time.time)
        {
            nextQueryClean = 0;
            SNet_Auth.current.query.Clear();
        }
    }

    // UI Panels.
    public UIVisibility panel_Loading, panel_Lobby, panel_Menu, panel_Game, panel_Connections, panel_Lobbies, panel_LobbyListLoading, panel_Vehicle;

    /// <summary>
    /// Called by network
    /// </summary>
    /// <param name="val">True: succeded, False: failed</param>
    private void OnLobbyCreated(bool val)
    {
        Debug.Log("OnLobbyCreated()" + ": " + val);
        panel_Loading.Open(false);
        panel_Lobby.Open();
    }

    public Transform lobbyListGrid;
    public Transform lobbyListPrefab;
    public Transform lobbyPageGrid;
    public Transform lobbyPagePrefab;
    int maxLobbyInPage = 10;

    private void DrawLobbyPages()
    {
        Debug.Log("DrawLobbyPages()");
        foreach (Transform t in lobbyPageGrid)
        {
            Destroy(t.gameObject);
        }

        int pagecount = Client.Instance.LobbyList.Lobbies.Count / maxLobbyInPage;
        for (int i = 0; i < pagecount; i++)
        {
            Transform prefab = Instantiate(lobbyPagePrefab, lobbyListGrid);
            prefab.GetComponent<Button>().onClick.AddListener(() => { currentPage = i; OnLobbyList(); });
            prefab.GetComponentInChildren<Text>().text = (i + 1).ToString();
        }
    }

    int currentPage = 0;
    private void OnLobbyList() // Event called from SNET_Manager
    {
        panel_LobbyListLoading.Open(false);

        Debug.Log("OnLobbyList()");
        foreach (Transform t in lobbyListGrid)
        {
            Destroy(t.gameObject);
        }

        for (int i = currentPage * maxLobbyInPage; i < currentPage * maxLobbyInPage + maxLobbyInPage; i++)
        {
            if (i >= Client.Instance.LobbyList.Lobbies.Count)
            {
                break;
            }

            Transform prefab = Instantiate(lobbyListPrefab, lobbyListGrid);
            prefab.Find("name").GetComponent<Text>().text = Client.Instance.LobbyList.Lobbies[i].Name;
            prefab.Find("owner").GetComponent<Text>().text = Client.Instance.Friends.GetName(Client.Instance.LobbyList.Lobbies[i].Owner);
            prefab.Find("players").GetComponent<Text>().text = Client.Instance.LobbyList.Lobbies[i].NumMembers + "/" + Client.Instance.LobbyList.Lobbies[i].MemberLimit;

            ulong u = Client.Instance.LobbyList.Lobbies[i].LobbyID;
            prefab.GetComponent<Button>().onClick.AddListener(() => { JoinLobby(u); });
        }

        panel_Lobbies.transform.Find("nogames").gameObject.SetActive(lobbyListGrid.childCount == 0);
    }

    /// <summary>
    /// Join lobby request
    /// </summary>
    /// <param name="id">lobby id</param>
    public void JoinLobby(ulong id)
    {
        Debug.Log("Joining lobby " + id);
        Client.Instance.Lobby.Join(id);
    }

    /// <summary>
    /// Lobby join response
    /// </summary>
    /// <param name="value">We joined or we not</param>
    private void OnLobbyJoined(bool value)
    {
        Debug.Log("OnLobbyJoined() " + value);
        if (value)
        {
            panel_Lobbies.Open(false);
            panel_Menu.Open(false);
            SNet_Chat.instance.panel_Chat.Open ();
            if (!gameStatus.iS)
            panel_Lobby.Open();
        }
    }

    /// <summary>
    /// Lobby leave
    /// </summary>
    private void OnLobbyLeave()
    {
        Debug.Log("OnLobbyLeave()");

        SNet_Auth.current.validatedIds.Clear();
        SNet_Auth.current.query.Clear();
        nextAuthCheck = 0;
        nextQueryClean = 0;
        SNet_Auth.authed = false;
        SNet_Network.currentHost = 0;
        memberCount = 0;
        panel_Game.Open(false);
        panel_Lobby.Open(false);
        SNet_Chat.instance.panel_Chat.Open(false);
        panel_Menu.Open();
        UnloadLevel();
    }

    public Transform lobbyMemberHolder;
    public Transform UIConnectedPlayer;
    public Button Button_startGame;
    public Dropdown Button_levelSelector;

    /// <summary>
    /// Draw lobby screen.
    /// </summary>
    public void OnLobbyUpdate()
    {
        List<ulong> members = Client.Instance.Lobby.GetMemberIDs().ToList ();
        Debug.Log("OnLobbyUpdate(): Member count " + Client.Instance.Lobby.GetMemberIDs().Length);

        foreach (Transform t in lobbyMemberHolder)
        {
            if (!members.Contains(ulong.Parse(t.name)))
            {
                if (memberCount > 0)
                {
                    /*
                    * DISCONNECTED USER
                    * */
                    Transform conn = Instantiate(panel_Connections.transform.GetChild(0), panel_Connections.transform);
                    conn.GetComponentInChildren<Text>().text = t.GetComponentInChildren<Text>().text + " disconnected.";
                    conn.GetComponentInChildren<UnityEngine.UI.Image>().color = new UnityEngine.Color(1, 0.5f, 0.5f);
                    conn.gameObject.GetComponent<UIVisibility>().Open ();
                    /*
                    */
                }

                Destroy(t.gameObject);
            }
        }

        int i = 0;
        foreach (ulong id in members)
        {
            if (lobbyMemberHolder.Find(id.ToString()))
                continue; // already has

            string alias = Client.Instance.Friends.GetName(id);
            Transform t = Instantiate(UIConnectedPlayer, lobbyMemberHolder);
            t.name = id.ToString();
            t.Find("avatar").GetComponent<Facepunch_Avatar>().Fetch(id);
            t.Find("name").GetComponent<Text>().text = alias;
            i++;

            if (SNet_Auth.current.validatedIds.Contains(id))
            {
                SNet_Auth.Validate(id);
            }

            if (memberCount > 0)
            {
                /*
                 * CONNECTED USER
                 * */
                Transform conn = Instantiate(panel_Connections.transform.GetChild(0), panel_Connections.transform);
                conn.GetComponentInChildren<Text>().text = t.GetComponentInChildren<Text>().text + " connected.";
                conn.GetComponentInChildren<UnityEngine.UI.Image>().color = new UnityEngine.Color(0.5f, 1f, 0.5f);
                conn.gameObject.AddComponent<DestroyIn>();
                conn.gameObject.SetActive(true);
                /*
                */
            }
        }
    }

    /// <summary>
    /// Something spawned on world.
    /// </summary>
    /// <param name="value">Incoming message</param>
    private void OnSpawn (string[] value)
    {
        SNet_Network.Spawn readMessage = SNet_Network.SNetMessage.ReadMessage<SNet_Network.Spawn>(value[0]);
        readMessage.s = ulong.Parse(value[1]);

        if (GameMap.instance == null || !SNet_Network.instance.isHost (readMessage.s))
            return;

        SNet_Identity go = null;
        if (SNet_Identity.find(readMessage.i, out go))
        {
            // Already have that identity.
            return;
        }

        Transform prefab = ResourcesLoader.prefabs.Find(x => x.name == readMessage.b);
        if (prefab == null)
        {
            Debug.Log("Null spawn prefab: " + readMessage.b);
            return;
        }

        string pName = prefab.name;
        prefab = Instantiate(prefab, readMessage.p, (readMessage.r != Quaternion.identity) ? readMessage.r : prefab.rotation);
        prefab.name = pName + " (Networked)"; 
        if (readMessage.c != Vector3.zero)
            prefab.localScale = readMessage.c;

        Destroy (prefab.GetComponent<Snet_SceneObject_AutoSpawn>());

        SNet_Identity identity = prefab.GetComponent<SNet_Identity>();
        if (identity != null)
        {
            identity.Set(readMessage.i, readMessage.b);

            Rigidbody rBody = identity.GetComponent<Rigidbody>();
            if (rBody != null)
            {
                // Default force
                rBody.AddForce(identity.transform.forward * readMessage.f);
            }
        }
    }

    /// <summary>
    /// Users cannot spawn npcs more than 100 in 10 minutes.
    /// </summary>
    public class User_Spawn_Limit
    {
        public User_Spawn_Limit(ulong i)
        {
            id = i;
            reset = Time.time + 60;
        }

        public ulong id;
        public int spawns = 100;
        public float reset = 0;
    }

    public List<User_Spawn_Limit> limits = new List<User_Spawn_Limit>();
    /// <summary>
    /// Spawn request to host, from a connection.
    /// </summary>
    /// <param name="value">Incoming message</param>
    private void OnSpawn_Request(string[] value)
    {
        SNet_Network.Spawn_Request readMessage = SNet_Network.SNetMessage.ReadMessage<SNet_Network.Spawn_Request>(value[0]);
        readMessage.s = ulong.Parse(value[1]);

        if (!gameStatus.iS || !SNet_Network.instance.isHost ())
            return; // Game must be started and only host can receive this.

        User_Spawn_Limit usl = limits.Find(x => x.id == readMessage.s);
        if (usl != null && usl.reset < Time.time)
        {
            limits.Remove(usl);
            usl = null;
        }

        if (usl == null)
        {
            usl = new User_Spawn_Limit(readMessage.s);
            limits.Add(usl);
        }

        if (usl.spawns <= 0)
            return;

        usl.spawns--;

        SNet_Network.instance.Send_Spawn (readMessage.b, readMessage.p, readMessage.r, readMessage.c, readMessage.f);
    }

    public Transform UI_PlayerAgent;
    /// <summary>
    /// Player spawn.
    /// </summary>
    /// <param name="value">Incoming message</param>
    private void OnSpawn_Player(string[] value)
    {
        SNet_Network.Spawn_Player readMessage = SNet_Network.SNetMessage.ReadMessage<SNet_Network.Spawn_Player>(value[0]);
        readMessage.s = ulong.Parse(value[1]);

        if (GameMap.instance == null)
            return;

        SNet_Identity go = null;
        if (SNet_Identity.find(readMessage.i, out go))
        {
            // Already have that identity.
            return;
        }

        Debug.Log("Spawn_Player(): " + readMessage.s);

        Transform prefab = ResourcesLoader.prefabs.Find(x => x.name == readMessage.b);
        prefab = Instantiate(prefab, readMessage.p, readMessage.r);
        if (readMessage.c != Vector3.zero)
            prefab.localScale = readMessage.c;

        SNet_Identity identity = prefab.GetComponent<SNet_Identity>();
        bool isLocalPlayer = readMessage.s == Client.Instance.SteamId;
        prefab.gameObject.AddComponent<PlayerInventory>().isLocalPlayer = isLocalPlayer;
        prefab.gameObject.AddComponent<SNet_Controller>().isLocalPlayer = isLocalPlayer;
        prefab.gameObject.AddComponent<SNet_Animator>();

        identity.Set(readMessage.i, readMessage.b);
    }

    /// <summary>
    /// Spawn lootable item on world.
    /// </summary>
    /// <param name="value">Incoming message</param>
    private void OnSpawn_Item(string[] value)
    {
        SNet_Network.Spawn_Item readMessage = SNet_Network.SNetMessage.ReadMessage<SNet_Network.Spawn_Item>(value[0]);
        readMessage.s = ulong.Parse(value[1]);

        if (!SNet_Network.instance.isHost(readMessage.s))
            return; // Only the host can send this

        SNet_Identity go = null;
        if (SNet_Identity.find(readMessage.i, out go))
        {
            if (readMessage.a > 0)
            go.GetComponent<Item>().item.ammo = readMessage.a;
            // Already have that identity.
            return;
        }

        Transform prefab = ResourcesLoader.prefabs.Find(x => x.name == readMessage.b);
        prefab = Instantiate(prefab, readMessage.p, readMessage.r);
        if (readMessage.c != Vector3.zero)
        prefab.localScale = readMessage.c;

        SNet_Identity identity = prefab.GetComponent<SNet_Identity>();
        identity.Set(readMessage.i, readMessage.b);

        if (readMessage.a > 0) // or it has default ammo
        identity.GetComponent<Item>().item.ammo = readMessage.a;

        Collider cldr = prefab.GetComponent<Collider>();
        if (cldr != null)
            cldr.enabled = true;

        Rigidbody rb = prefab.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = false;

        DestroyIn dIn = prefab.GetComponent<DestroyIn>();
        if (dIn != null)
            dIn.enabled = true;
    }

    /// <summary>
    /// Networked object destroyed.
    /// </summary>
    /// <param name="value">Incoming message</param>
    private void OnDestroyed(string[] value)
    {
        SNet_Network.Destroyed readMessage = SNet_Network.SNetMessage.ReadMessage<SNet_Network.Destroyed>(value[0]);
        readMessage.s = ulong.Parse(value[1]);

        if (!SNet_Network.instance.isHost(readMessage.s))
            return; // Only the host can send this

        if (SNet_Network.instance.isHost())
            return; // Host is already destroyed this object.

        SNet_Identity go = null;
        if (SNet_Identity.find(readMessage.i, out go))
        {
            Destroy(go.gameObject);
        }
    }

    /// <summary>
    /// SNet_Animator Message
    /// </summary>
    /// <param name="value">Incoming message</param>
    private void OnA_Bool(string[] value)
    {
        SNet_Animator.A_Bool readMessage = SNet_Network.SNetMessage.ReadMessage<SNet_Animator.A_Bool>(value[0]);
        readMessage.s = ulong.Parse(value[1]);

        SNet_Identity id = null;
        if (
        SNet_Identity.find(readMessage.i, out id))
        {
            id.animator.animator.SetBool(readMessage.id, readMessage.value);
        }
    }

    /// <summary>
    /// SNet_Animator Message
    /// </summary>
    /// <param name="value">Incoming message</param>
    private void OnA_Float(string[] value)
    {
        SNet_Animator.A_Float readMessage = SNet_Network.SNetMessage.ReadMessage<SNet_Animator.A_Float>(value[0]);
        readMessage.s = ulong.Parse(value[1]);

        SNet_Identity id = null;
        if (
        SNet_Identity.find(readMessage.i, out id))
        {
            id.animator.animator.SetFloat(readMessage.id, readMessage.value);
        }
    }

    /// <summary>
    /// SNet_Animator Message
    /// </summary>
    /// <param name="value">Incoming message</param>
    private void OnA_Int(string[] value)
    {
        SNet_Animator.A_Int readMessage = SNet_Network.SNetMessage.ReadMessage<SNet_Animator.A_Int>(value[0]);
        readMessage.s = ulong.Parse(value[1]);

        SNet_Identity id = null;
        if (
        SNet_Identity.find(readMessage.i, out id))
        {
            id.animator.animator.SetInteger(readMessage.id, readMessage.value);
        }
    }

    /// <summary>
    /// SNet_Animator Message
    /// </summary>
    /// <param name="value">Incoming message</param>
    private void OnA_Trigger(string[] value)
    {
        SNet_Animator.A_Trigger readMessage = SNet_Network.SNetMessage.ReadMessage<SNet_Animator.A_Trigger>(value[0]);
        readMessage.s = ulong.Parse(value[1]);

        SNet_Identity id = null;
        if (
        SNet_Identity.find(readMessage.i, out id))
        {
            id.animator.animator.SetTrigger(readMessage.id);
        }
    }

    /// <summary>
    /// Rigidbody Message
    /// </summary>
    /// <param name="value">Incoming message</param>
    private void OnRB(string[] value)
    {
        SNet_Rigidbody.RB readMessage = SNet_Network.SNetMessage.ReadMessage<SNet_Rigidbody.RB>(value[0]);
        readMessage.s = ulong.Parse(value[1]); 

        if (!SNet_Network.Authorized (readMessage.s))
            return; // Sender didn't need rbody sync.

        SNet_Identity id = null;
        if (
        SNet_Identity.find(readMessage.i, out id))
        {
            if (id.controller != null && readMessage.i != id.identity)
                return; // Player rigibody sync is client authoritative. This is not allowed.

            if (id.rbody != null && id.rbody.rBody != null)
            {
                id.rbody.rBody.velocity = readMessage.v;
                id.rbody.rBody.angularVelocity = readMessage.a;
                return;
            }
        }
    }

    /// <summary>
    /// SNet_Transform, position message
    /// </summary>
    /// <param name="value">Incoming message</param>
    private void OnPos(string[] value)
    {
        SNet_Transform.Pos readMessage = SNet_Network.SNetMessage.ReadMessage<SNet_Transform.Pos>(value[0]);
        readMessage.s = ulong.Parse(value[1]);

        if (!SNet_Network.Authorized(readMessage.s))
            return; // Sender didn't need tform sync.

        SNet_Identity id = null;
        if (
        SNet_Identity.find(readMessage.i, out id))
        {
            if (id.controller != null && readMessage.i != id.identity)
                return; // Player sync is client authoritative. This is not allowed.

            if (id.controller != null && Vector3.Distance(id.transform.position, readMessage.p) > 0.5f)
                id.tform.tweener.TweenFor(readMessage.p);
            else id.transform.position = readMessage.p;
        }
    }

    /// <summary>
    /// SNet_Transform, rotation message
    /// </summary>
    /// <param name="value">Incoming message</param>
    private void OnRot(string[] value)
    {
        SNet_Transform.Rot readMessage = SNet_Network.SNetMessage.ReadMessage<SNet_Transform.Rot>(value[0]);
        readMessage.s = ulong.Parse(value[1]);

        if (!SNet_Network.Authorized(readMessage.s))
            return; // Sender didn't need tform sync.

        SNet_Identity id = null;
        if (
        SNet_Identity.find(readMessage.i, out id))
        {
            if (id.controller != null && readMessage.i != id.identity)
                return; // Player sync is client authoritative. This is not allowed.

            if (id.controller != null)
                id.tform.tRot.eulerAngles = readMessage.r;
            else id.transform.eulerAngles = readMessage.r;
        }
    }

    /// <summary>
    /// Player aim
    /// </summary>
    /// <param name="value">Incoming message</param>
    private void OnA(string[] value)
    {
        SNet_Controller.A readMessage = SNet_Network.SNetMessage.ReadMessage<SNet_Controller.A>(value[0]);
        readMessage.s = ulong.Parse(value[1]);

        if (!SNet_Network.Authorized(readMessage.s))
            return;

        SNet_Identity id = null;
        if (
        SNet_Identity.find(readMessage.s, out id))
        {
            id.controller.syncedAim = readMessage;
        }
    }

    /// <summary>
    /// Player health, comes from master client (host)
    /// </summary>
    /// <param name="value">Incoming message</param>
    private void OnHealth (string[] value)
    {
        SNet_Controller.Health readMessage = SNet_Network.SNetMessage.ReadMessage<SNet_Controller.Health>(value[0]);
        readMessage.s = ulong.Parse(value[1]);

        SNet_Identity id = null;
        if (
        SNet_Identity.find(readMessage.i, out id))
        {
            id.controller.health = readMessage;
        }
    }

    /// <summary>
    /// Some player has been hit
    /// </summary>
    /// <param name="value">Incoming message</param>
    private void OnUserHit(string[] value)
    {
        SNet_Controller.UserHit readMessage = SNet_Network.SNetMessage.ReadMessage<SNet_Controller.UserHit>(value[0]);
        readMessage.s = ulong.Parse(value[1]);

        SNet_Identity id = null;
        if (
        SNet_Identity.find(readMessage.i, out id))
        {
            /*
            * UI DAMAGE EFFECT
            * */

            if (id.controller == SNet_Controller.user && !SNet_Controller.user.isDead)
            {
                UI_UserHit.instance.panel.alpha = 1;

                Vector3 vec1 = readMessage.h - id.transform.position;
                Vector3 vec2 = id.transform.forward;

                float angle = Vector3.Angle(vec1, vec2);
                Vector3 cross = Vector3.Cross(vec1, vec2);
                if (cross.y < 0) angle = -angle;

                UI_UserHit.instance.incoming.eulerAngles = new Vector3(0, 0, angle);
            }

            SNet_Controller.Health health = new SNet_Controller.Health(id.identity);
            health.v = id.controller.health.v - readMessage.v;
            id.controller.health = health;
        }
    }

    /// <summary>
    /// Ragdoll impact message
    /// </summary>
    /// <param name="value">Incoming message</param>
    private void OnImpact(string[] value)
    {
        RagdollHelper.Impact readMessage = SNet_Network.SNetMessage.ReadMessage<RagdollHelper.Impact>(value[0]);
        readMessage.s = ulong.Parse(value[1]);

        SNet_Identity id = null;
        if (
        SNet_Identity.find(readMessage.i, out id))
        {
            if (id.ragdoll != null)
            { // Ragdoll is an option
                id.ragdoll.Impacted(readMessage);
            }
        }
    }

    /// <summary>
    /// Some player shooted.
    /// </summary>
    /// <param name="value">Incoming message</param>
    private void OnSt(string[] value)
    {
        PlayerInventory.St readMessage = SNet_Network.SNetMessage.ReadMessage<PlayerInventory.St>(value[0]);
        readMessage.s = ulong.Parse(value[1]);

        SNet_Identity id = null;
        if (
        SNet_Identity.find(readMessage.s, out id))
        {
            id.controller.Shoot();
        }
    }

    /// <summary>
    /// Item looted by player.
    /// </summary>
    /// <param name="value">Incoming message</param>
    private void OnPlayerItem(string[] value)
    {
        Item.PlayerItem readMessage = SNet_Network.SNetMessage.ReadMessage<Item.PlayerItem>(value[0]);
        readMessage.s = ulong.Parse(value[1]);

        if (!SNet_Network.instance.isHost(readMessage.s))
            return; // only host can send this

        SNet_Identity id = null;
        if (
        SNet_Identity.find(readMessage.looter, out id))
        {
            if (id.controller != null && id.controller.inventory != null)
                id.controller.inventory.AddItem(readMessage);
        }
    }

    /// <summary>
    /// Player equips a new item
    /// </summary>
    /// <param name="value">Incoming message</param>
    private void OnEquipItem(string[] value)
    {
        PlayerInventory.EquipItem readMessage = SNet_Network.SNetMessage.ReadMessage<PlayerInventory.EquipItem>(value[0]);
        readMessage.s = ulong.Parse(value[1]);

        SNet_Identity id = null;
        if (
        SNet_Identity.find(readMessage.s, out id))
        {
            if (id.controller != null && id.controller.inventory != null)
            id.controller.inventory.Equip(readMessage.prefabName);
        }
    }

    /// <summary>
    /// Inventory status
    /// Inventory list, and current item of a player.
    /// Called when a new player connects.
    /// </summary>
    /// <param name="value">Incoming message</param>
    private void OnIS(string[] value)
    {
        PlayerInventory.IS readMessage = SNet_Network.SNetMessage.ReadMessage<PlayerInventory.IS>(value[0]);
        readMessage.s = ulong.Parse(value[1]);

        if (readMessage.s == Client.Instance.SteamId)
            return; // Sender already has the updated inventory.

        SNet_Identity id = null;
        if (
        SNet_Identity.find(readMessage.s, out id))
        {
            if (id.controller != null && id.controller.inventory != null)
            {
                id.controller.inventory.inv = readMessage;
                id.controller.inventory.Equip(readMessage.ci.prefabName);
            }
        }
    }

    /// <summary>
    /// Some explosive has been exploded.
    /// </summary>
    /// <param name="value">Incoming message</param>
    private void OnExploded (string[] value)
    {
        Explosive.Exploded readMessage = SNet_Network.SNetMessage.ReadMessage<Explosive.Exploded>(value[0]);
        readMessage.s = ulong.Parse(value[1]);

        if (!SNet_Network.instance.isHost(readMessage.s))
            return;

        SNet_Identity id = null;
        if (
        SNet_Identity.find(readMessage.i, out id))
        {
            if (id.explosive != null)
                id.explosive.Explode();
        }
    }

    /// <summary>
    /// Data of a vehicle.
    /// Contains gas, brake, steering
    /// </summary>
    /// <param name="value">Incoming message</param>
    private void OnVI (string[] value)
    {
        SNet_Vehicle.VI readMessage = SNet_Network.SNetMessage.ReadMessage<SNet_Vehicle.VI>(value[0]);
        readMessage.s = ulong.Parse(value[1]);

        if (!SNet_Network.Authorized (readMessage.s))
            return; // sender doesnt need sync.
            
        SNet_Identity id = null;
        if (
        SNet_Identity.find(readMessage.i, out id))
        {
            id.vehicle.info = readMessage;
        }
    }

    /// <summary>
    /// Data of a vehicle.
    /// Contains health
    /// </summary>
    /// <param name="value">Incoming message</param>
    private void OnVH (string[] value)
    {
        SNet_Vehicle.VH readMessage = SNet_Network.SNetMessage.ReadMessage<SNet_Vehicle.VH>(value[0]);
        readMessage.s = ulong.Parse(value[1]);

        if (!SNet_Network.instance.isHost (readMessage.s))
            return; // only host can send this

        SNet_Identity id = null;
        if (
        SNet_Identity.find(readMessage.i, out id))
        {
            id.vehicle.health = readMessage;
        }
    }

    /// <summary>
    /// A player interacts to a networked object.
    /// </summary>
    /// <param name="value">Incoming message</param>
    private void OnInteract(string[] value)
    {
        Interactor.Interact readMessage = SNet_Network.SNetMessage.ReadMessage<Interactor.Interact>(value[0]);
        readMessage.s = ulong.Parse(value[1]);

        SNet_Identity id = null;
        if (
        SNet_Identity.find(readMessage.i, out id))
        {
            // sender interacts the item.
            SNet_Identity sender = null;

            if (SNet_Identity.find(readMessage.s, out sender))
            {
                switch (id.gameObject.tag)
                {
                    case "Item": // Loot weapon or something
                        Item item = id.GetComponent<Item>();

                        if (item != null && sender.controller != null)
                        {
                            Item.PlayerItem pi = sender.controller.GetComponent<PlayerInventory>().inv.l.Find(x => x.prefabName == item.item.prefabName);

                            if (pi != null && pi.maxammo <= pi.ammo)
                                return; // Maximum ammo reached.

                            item.item.looter = sender.controller.GetComponent<SNet_Identity>().identity;

                            SNet_Network.instance.Send_Message(item.item);

                            Destroy(item.gameObject);
                        }

                        break;

                    case "Vehicle": // Get into a vehicle
                        SNet_Vehicle vehicle = id.GetComponent<SNet_Vehicle>();

                        int vIndex = vehicle.vehicle.p.FindIndex(x=>x == sender.identity);

                        if (vIndex != -1)
                        {
                            vehicle.vehicle.p [vIndex] = 0;
                            vehicle.VehicleUpdate();
                            return;
                        }

                        vIndex = vehicle.vehicle.p.FindIndex(x => x == 0); // Empty seat
                        if (!sender.controller.isDead && vIndex != -1 && !vehicle.vehicle.p.Contains(sender.identity))
                        {
                            vehicle.vehicle.p[vIndex] = sender.identity;
                            vehicle.VehicleUpdate();
                        }
                        break;
                }
            }
        }
    }

    public Transform UI_VehicleSeat;
    /// <summary>
    /// Vehicle passengers data.
    /// </summary>
    /// <param name="value">Incoming message</param>
    private void OnV(string[] value)
    {
        SNet_Vehicle.V readMessage = SNet_Network.SNetMessage.ReadMessage<SNet_Vehicle.V>(value[0]);
        readMessage.s = ulong.Parse(value[1]);

        if (!SNet_Network.instance.isHost(readMessage.s))
            return; // Only host can send vehicle data

        SNet_Identity id = null;
        if (
        SNet_Identity.find(readMessage.i, out id))
        {
            if (id.vehicle != null)
            {
                id.vehicle.vehicle = readMessage;

                int myClient_Driver = readMessage.p.FindIndex(x => x == Client.Instance.SteamId);
                id.vehicle.isLocalVehicle = myClient_Driver == 0;

                SNet_Identity[] currentPassangers = id.vehicle.passengerControllers.ToArray();

                id.vehicle.passengerControllers.Clear();
                foreach (ulong pid in readMessage.p)
                {
                    if (pid == 0)
                        continue;

                    SNet_Identity psid = null;
                    if (SNet_Identity.find(pid, out psid))
                    {
                        id.vehicle.passengerControllers.Add(psid);
                        psid.controller.currentVehicle = id;
                    }
                }

                foreach (SNet_Identity cp in currentPassangers)
                {
                    if (cp.controller.currentVehicle != null && !cp.controller.currentVehicle.vehicle.vehicle.p.Contains(cp.identity))
                    {
                        cp.controller.currentVehicle = null;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Change seat request
    /// </summary>
    /// <param name="value"></param>
    private void OnCSRequest(string[] value)
    {
        ChangeSeat.CSRequest readMessage = SNet_Network.SNetMessage.ReadMessage<ChangeSeat.CSRequest>(value[0]);
        readMessage.s = ulong.Parse(value[1]);

        if (!SNet_Network.instance.isHost())
            return; // Only host can receive this data

        SNet_Identity id = null;
        if (
        SNet_Identity.find(readMessage.s, out id))
        {
            if (id.controller.currentVehicle == null)
                return;

            if (id.controller.currentVehicle.vehicle.vehicle.p[readMessage.idx] != 0)
                return;

            int currentSeat = id.controller.currentVehicle.vehicle.vehicle.p.FindIndex(x => x == readMessage.s);
            if (currentSeat == -1)
                return;

            id.controller.currentVehicle.vehicle.vehicle.p[currentSeat] = 0;
            id.controller.currentVehicle.vehicle.vehicle.p[readMessage.idx] = readMessage.s;

            id.controller.currentVehicle.vehicle.VehicleUpdate();
        }
    }

    public SNet_Network.Game_Status gameStatus = new SNet_Network.Game_Status();

    /// <summary>
    /// Current game status
    /// Game map
    /// isStarted?
    /// </summary>
    /// <param name="value">Incoming message</param>
    private void OnGame_Status(string[] value)
    {
        SNet_Network.Game_Status readMessage = SNet_Network.SNetMessage.ReadMessage<SNet_Network.Game_Status>(value[0]);
        readMessage.s = ulong.Parse(value[1]);

        if (!SNet_Network.instance.isHost(readMessage.s))
            return; // only host can send this

        if (mapIsLoading)
            return; // Map is currently loading. Wait for finish.

        Button_levelSelector.captionText.text = readMessage.l;

        GameObject sGame = panel_Lobby.transform.Find("back").Find("StartGame").gameObject;

        if (!sGame.activeSelf)
            sGame.SetActive(true);

        if (!gameStatus.iS && readMessage.iS)
        {
            sGame.GetComponentInChildren<Text>().text = "Stop Game";
            panel_Lobby.Open (false);
            panel_Game.Open(true);
        }

        if (gameStatus.iS && !readMessage.iS)
        {
            sGame.GetComponentInChildren<Text>().text = "Start Game";
            panel_Game.Open(false);
            panel_Lobby.Open(true);
        }

        if (readMessage.iS)
        {
            /*
            * LOAD LEVEL ASYNC
            * */
            if (gameStatus == null || gameStatus.iS != readMessage.iS || gameStatus.l != readMessage.l)
            {
                Debug.Log("Loading: " + readMessage.l);
                StartCoroutine(LoadYourAsyncScene(readMessage.l));
            }
            /*
             * */
        }
        else if (mapLoaded)
        {
            UnloadLevel();
        }

        gameStatus = readMessage;
    }

    void UnloadLevel()
    {
        Debug.Log("Unloading level.");

        /// Free the camera
        MouseOrbitImproved.instance.target = null;
        MouseOrbitImproved.instance.transform.SetParent(null);
        ///

        mapLoaded = false;
        gameStatus = new SNet_Network.Game_Status();
        panel_Lobby.transform.Find ("back").Find("StartGame").GetComponentInChildren<Text>().text = "Start Game";
        StartCoroutine(LoadYourAsyncScene("Empty", true));
        /*
         * Empty scene is used for finishing the game and return to lobby. But lobby must remain. We cannot load Game scene again so here is 'Empty'
         * */
    }

    public static bool mapLoaded = false;
    public static bool mapIsLoading = false;
    IEnumerator LoadYourAsyncScene(string sceneName, bool unload = false)
    {
        if (!mapIsLoading)
        {
            Debug.Log("Map Loading: " + sceneName + " " + Time.time + " (map is loading: ) " + mapIsLoading);

            mapIsLoading = true;

            // Free the camera
            MouseOrbitImproved.instance.transform.parent = null;
            MouseOrbitImproved.instance.target = null;

            /*
            * REMOVE SNET IDENTITIES
            * */
            foreach (SNet_Identity id in SNet_Identity.list)
                if (id != null) Destroy(id.gameObject);

            SNet_Identity.list.Clear();
            /*
             * */
            panel_Loading.Open();
            // The Application loads the Scene in the background at the same time as the current Scene.
            //This is particularly good for creating loading screens. You could also load the Scene by build //number.

            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);

            mapLoaded = false;

            //Wait until the last operation fully loads to return anything
            while (!asyncLoad.isDone)
            {
                yield return null;
            }

            mapLoaded = !unload;
            mapIsLoading = false;

            /*
            CLEAR VOICE DATA, THIS IS A MUST.
            * */
            SNet_Voice.instance.vdList.Clear();

            panel_Loading.Open(false);
        }
    }
}
