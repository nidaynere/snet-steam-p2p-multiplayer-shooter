/*! 
@author EasyMOBA <easymoba.com>
@lastupdate 13 February 2018
*/

using UnityEngine;

/// <summary>
/// If you instantiate something must be networked object, this script will spawn it over the network.
/// For example, drag a vehicle to the scene if you are the current master client of the lobby, it will be spawned over the network.
/// </summary>

[RequireComponent(typeof(SNet_Identity))]

public class Snet_SceneObject_AutoSpawn : MonoBehaviour
{
    [Tooltip("Used for default scene objects. PrefabName required to host by the other connections.")]
    public string prefab;

    private void Start ()
    {
        if (!SNet_Network.instance.isHost())
        {
            Destroy(gameObject);
        }

        if (GetComponent<SNet_Identity>().set)
            return;

        if (Application.isEditor && string.IsNullOrEmpty(prefab))
        {
            /*
            * SET PREFAB NAME
            * */
            prefab = gameObject.name;
        }

        if (Application.isPlaying && !string.IsNullOrEmpty (prefab))
        {
            WheelCollider[] wheels = GetComponentsInChildren<WheelCollider>();
            foreach (WheelCollider wc in wheels)
                wc.enabled = false;
            Destroy(GetComponent<SNet_Vehicle>());
            Destroy(GetComponent<Collider>());
            Destroy(GetComponent<Rigidbody>());

            MonoBehaviourExtensions.Invoke(this, Respawn, 0.5f);
        }
    }

    void Respawn()
    {
        if (SNet_Network.instance.isHost())
        {
            SNet_Network.instance.Send_Spawn(prefab, transform.position, transform.rotation, transform.localScale);
        }

        Destroy(gameObject);
    }
}
