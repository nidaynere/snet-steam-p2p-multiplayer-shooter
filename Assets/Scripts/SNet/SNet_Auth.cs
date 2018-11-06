/*! 
@author EasyMOBA <easymoba.com>
@lastupdate 15 February 2018
*/

using System.Collections.Generic;
using UnityEngine;
using Facepunch.Steamworks;

/// <summary>
/// SNet_Auth is for the Valve Anti Cheat.
/// There is no Auth.StartSession method on Client.Instance at Facepunch.Steamworks, so we had to create a local server in background to use Auth methods.
/// Don't worry, this is harmless.
/// </summary>
public class SNet_Auth : MonoBehaviour
{
    public List<ulong> validatedIds = new List<ulong>();
    public List<ulong> query = new List<ulong>();

    /// <summary>
    /// It will be true afte got Auth_All message.
    /// </summary>
    public static bool authed = false;

    public class Auth_All : SNet_Network.SNetMessage
    {
        public Auth_All()
        {
            list = current.validatedIds;
        }

        public List<ulong> list = new List<ulong>();
    }

    private void OnAuth_All(string[] value)
    {
        Auth_All readMessage = SNet_Network.SNetMessage.ReadMessage<Auth_All>(value[0]);
        readMessage.s = ulong.Parse(value[1]);

        if (!SNet_Network.instance.isHost(readMessage.s))
            return; // Only host can send this.

        authed = true;

        foreach (ulong i in readMessage.list)
            Validate(i);

        readMessage.list = readMessage.list.FindAll(x => !validatedIds.Contains(x));

        Debug.Log("OnAuth_All(): " + readMessage.list.Count);

        validatedIds.AddRange(readMessage.list);
    }

    /// <summary>
    /// Validate
    /// </summary>
    [System.Serializable]
    public class Auth_V : SNet_Network.SNetMessage
    {
        public Auth_V(ulong _id)
        {
            i = _id;
        }
    }

    private void OnAuth_V(string[] value)
    {
        Auth_V readMessage = SNet_Network.SNetMessage.ReadMessage<Auth_V>(value[0]);
        readMessage.s = ulong.Parse(value[1]);

        if (!SNet_Network.instance.isHost(readMessage.s))
            return; // Only host can send this.

        Debug.Log("OnAuth_V()");

        Validate(readMessage.i);

        if (!validatedIds.Contains (readMessage.i))
            validatedIds.Add(readMessage.i);
    }

    /// <summary>
    /// Incoming message from host, auth yourself.
    /// </summary>
    [System.Serializable]
    public class Auth_H : SNet_Network.SNetMessage
    {
        public Auth_H(byte[] _data)
        {
            d = _data;
        }

        /// <summary>
        /// Ticket data
        /// </summary>
        public byte[] d;
    }

    /// <summary>
    /// Incoming auth request from host. Identity yourself
    /// </summary>
    /// <param name="value">Incoming message</param>
    public void OnAuth_H(string[] value)
    {
        Auth_H readMessage = SNet_Network.SNetMessage.ReadMessage<Auth_H>(value[0]);
        readMessage.s = ulong.Parse(value[1]);

        Debug.Log("OnAuth_H()");

        Check(readMessage.s, readMessage.d);
    }

    /// <summary>
    /// Current auth instance
    /// </summary>
    public static SNet_Auth current;

    private void Awake()
    {
        current = this;

        SNet_Network.RegisterHandler("Auth_H", OnAuth_H);
        SNet_Network.RegisterHandler("Auth_V", OnAuth_V);
        SNet_Network.RegisterHandler("Auth_All", OnAuth_All);
    }

    public static void Init()
    {
        if (ticket != null)
            ticket.Cancel();

        if (Server.Instance != null)
            return;

        Debug.Log("SNet_Auth initiliazed");

        ServerInit initer = new ServerInit("", "");
        initer.GamePort = 27020;

        var server = new Server(Client.Instance.AppId, initer);

        if (server == null)
        {
            Debug.LogError("Steam Server init failed. Please restart the application/Unity");
            Application.Quit();
            return;
        }

        server.LogOnAnonymous();

        server.Auth.OnAuthChange = (steamid, ownerid, status) =>
        {
            Debug.Log(status + " " + ownerid);
            server.Auth.EndSession(steamid);

            Transform member = SNet_Manager.instance.lobbyMemberHolder.Find(steamid.ToString());

            if (status == ServerAuth.Status.OK)
            {
                if (SNet_Network.instance.isHost())
                { // client validated, send validate message to all of the clients
                    current.validatedIds.Add(steamid);
                    Validate(steamid);

                    // Send all validateds to the new validated user.
                    SNet_Network.instance.Send_Message(new Auth_All(), steamid);

                    // Send the others the new one.
                    ulong[] members = Client.Instance.Lobby.GetMemberIDs();
                    foreach (ulong u in members)
                        if (u != steamid)
                            SNet_Network.instance.Send_Message(new Auth_V(steamid), u);
                }

                else if (SNet_Network.instance.isHost(steamid))
                {
                    // host validated. send your auth ticket to the host.
                    if (ticket != null)
                        ticket.Cancel();

                    ticket = Client.Instance.Auth.GetAuthSessionTicket();
                    SNet_Network.instance.Send_Message(new Auth_H (ticket.Data), SNet_Network.currentHost);
                }
                /*
                 * USER VALIDATED
                 * */
                Debug.Log("User validated: " + steamid);

                Validate(steamid);
            }
            else
            {
                if (member != null)
                member.Find("VACError").gameObject.SetActive(true);
            } 
        };
    }

    /// <summary>
    /// Should be called at least once every frame
    /// </summary>
    /// 
    float nextUpdate = 0;
    private void FixedUpdate()
    {
        if (nextUpdate > Time.time)
            return;

        nextUpdate = Time.time + 1;

        if (Server.Instance != null)
        {
            if (Server.Instance.LoggedOn)
            {
                Server.Instance.Update();
            }
        }
    }

    public static Auth.Ticket ticket;

    public static void Check (ulong steamId, byte[] data)
    {
        Debug.Log("Auth request from: " + steamId + " Is me: " + (steamId == Client.Instance.SteamId));

        if (Server.Instance.Auth.StartSession(data, steamId))
        {
            Debug.Log("Secure Session started");
        }
        else Debug.Log("Secure session failed");
    }

    public static void Validate(ulong steamid)
    {
        Transform member = SNet_Manager.instance.lobbyMemberHolder.Find(steamid.ToString());

        if (member != null)
        {
            Validate(member);
        }
    }

    public static void Validate(Transform member)
    {
        member.Find("VACError").gameObject.SetActive(false);
        member.Find("VAC").GetComponent<UnityEngine.UI.Image>().color = new UnityEngine.Color(0, 1, 0, 0.5f);
    }

    private void OnDestroy()
    {
        if (gameObject.name == "SteamManager")
        {
            if (Server.Instance != null)
                Server.Instance.Dispose();
            if (ticket != null)
                ticket.Cancel();
        }
    }
}
