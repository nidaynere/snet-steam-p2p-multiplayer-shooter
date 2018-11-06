/*! 
@author Facepunch
https://github.com/Facepunch/Facepunch.Steamworks
@lastupdate 13 February 2018
*/

using UnityEngine;
using Facepunch.Steamworks;

/// <summary>
/// Modified Facepunch script.
/// </summary>
public class Facepunch_Client : MonoBehaviour
{
    public uint AppId;

    void Start()
    {
        if (AppId == 0)
            throw new System.Exception("You need to set the AppId to your game");

        //
        // Configure us for this unity platform
        //
        Config.ForUnity(Application.platform.ToString());

        // Create the client
        Client client = new Client(AppId);

        if (Client.Instance == null)
        {
            Start(); // Retry
            return;
        }

        if (!client.IsValid)
        {
            client = null;
            Debug.LogWarning("Couldn't initialize Steam");
            return;
        }

        Debug.Log("Steam Initialized: " + client.Username + " / " + client.SteamId);

        new SNet_Network(SNet_Manager.instance.gameObject); // Initilize SNet_Network to SNET_Manager
    }

    private void FixedUpdate()
    {
        if (Client.Instance == null)
            return;

        Client.Instance.Update();
    }

    private void OnDestroy()
    {
        if (gameObject.name == "SteamManager")
        {
            if (Client.Instance != null)
                Client.Instance.Dispose();
        }
    }
}
