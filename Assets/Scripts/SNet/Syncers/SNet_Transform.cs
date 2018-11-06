/*! 
@author EasyMOBA <easymoba.com>
@lastupdate 13 February 2018
*/

using System.Collections.Generic;
using UnityEngine;
using Facepunch.Steamworks;

/// <summary>
/// Syncs the transform (only positions and rotations. Scale is not synced) over the network.
/// </summary>
public class SNet_Transform : MonoBehaviour
{
    public static List<SNet_Transform> list = new List<SNet_Transform>();

    public class Pos : SNet_Network.SNetMessage
    {
        public Pos(ulong _identity, Vector3 value)
        {
            i = _identity;
            p = value;
        }

        /// <summary>
        /// The position will be sent over network
        /// </summary>
        public Vector3 p;
    }

    public class Rot : SNet_Network.SNetMessage
    {
        public Rot(ulong _identity, Vector3 value)
        {
            i = _identity;
            r = value;
        }

        /// <summary>
        /// The eulerangles will be sent over network
        /// </summary>
        public Vector3 r;
    }

    SNet_Identity identity;
    SNet_Controller controller;

    [Tooltip("Sync position for per x Second")]
    public float syncDelay_Position = 1f; // Default is 1 seconds
    [Tooltip("Sync rotation for per x Second")]
    public float syncDelay_Rotation = 0.2f; // Default is 1 seconds

    public bool syncPosition = true;
    public bool syncRotation = true;

    [HideInInspector]
    public Position_Tweener tweener;
    // Use this for initialization
    void Start()
    {
        identity = GetComponent<SNet_Identity>();
        tweener = gameObject.GetComponent<Position_Tweener>();
        if (tweener == null)
            tweener = gameObject.AddComponent<Position_Tweener>();

        lastPosition = transform.position;
        lastRotation = transform.eulerAngles;

        list.Add(this);
    }

    private void OnDestroy()
    {
        list.Remove(this);
    }

    [HideInInspector]
    public float nextUpdate_Position;
    [HideInInspector]
    public float nextUpdate_Rotation;

    [HideInInspector]
    public Quaternion tRot = Quaternion.identity;

    Vector3 lastPosition = Vector3.zero;
    Vector3 lastRotation = Vector3.zero;

    /// <summary>
    /// Use this method when a new player connected. This will update the syncer but don't make Steam Networks angry, so use Random values.
    /// </summary>
    public void ReUpdate()
    {
        nextUpdate_Position = Time.time + Random.Range (3f, 6f);
        nextUpdate_Rotation = Time.time + Random.Range (3f, 6f);
        lastPosition = Vector3.zero;
        lastRotation = Vector3.zero;
    }

    void Update()
    {
        if (SNet_Network.instance == null)
            return;

        if (identity.vehicle != null)
        {
            if (identity.vehicle.vehicle == null || identity.vehicle.vehicle.p.Count == 0)
                return;

            if (identity.vehicle.vehicle.p[0] == 0 && !SNet_Network.instance.isHost())
            {
                return; // No driver, only host can sync this.
            }
            if (identity.vehicle.vehicle.p[0] != 0 && identity.vehicle.vehicle.p[0] != Client.Instance.SteamId)
            {
                return; // There is a driver but I'm not the driver.
            }
        }
        else if ((identity.controller != null && !identity.controller.isLocalPlayer) || (identity.controller == null && !SNet_Network.instance.isHost()))
        {
            if (identity.controller != null && identity.controller.currentVehicle != null && !identity.controller.freeOnSeat)
                return;

            transform.rotation = Quaternion.Slerp(transform.rotation, tRot, 0.2f);
            return;
        }

        bool controllerOnVehicle = (identity.controller != null && identity.controller.currentVehicle != null);

        if (controllerOnVehicle && !identity.controller.freeOnSeat)
            return; //this is not for controllers are not free on their seat

        if (nextUpdate_Rotation < Time.time)
        {
            nextUpdate_Rotation = Time.time + syncDelay_Rotation;

            if (syncRotation && Vector3.Distance(transform.eulerAngles, lastRotation) > 0.1f)
            { // Don-t allow unnecessary syncs, if you sync all of the transforms, Steam Network won't allow that. It will cause a high latency.
                lastRotation = transform.eulerAngles;
                SNet_Network.instance.Send_Message(new Rot(identity.identity, transform.eulerAngles));
            }
        }

        if (controllerOnVehicle)
            return; // If the controller has vehicle, position won't be synced.

        if (nextUpdate_Position < Time.time)
        {
            nextUpdate_Position = Time.time + syncDelay_Position;

            if (syncPosition && Vector3.Distance(transform.position, lastPosition) > 0.1f)
            { // Don-t allow unnecessary syncs, if you sync all of the transforms, Steam Network won't allow that. It will cause a high latency.
                lastPosition = transform.position;
                SNet_Network.instance.Send_Message(new Pos(identity.identity, transform.position));
            }
        }
    }
}
