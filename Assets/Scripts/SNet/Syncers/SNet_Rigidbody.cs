/*! 
@author EasyMOBA <easymoba.com>
@lastupdate 13 February 2018
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Facepunch.Steamworks;

/// <summary>
/// Sync the rigidbody over the network. 
/// If you want to add force on something, after applied the force you may want to set this nextUpdate = Time.time + 0.1f to sync the force over the network.
/// </summary>
[RequireComponent(typeof(SNet_Identity))]
public class SNet_Rigidbody : MonoBehaviour
{
    public static List<SNet_Rigidbody> list = new List<SNet_Rigidbody>();

    public class RB : SNet_Network.SNetMessage
    {
        public RB(ulong _identity)
        {
            i = _identity;
        }

        /// <summary>
        /// angular velocity
        /// </summary>
        public Vector3 a;

        /// <summary>
        /// velocity
        /// </summary>
        public Vector3 v;
    }

    [HideInInspector]
    public Rigidbody rBody;
    SNet_Identity identity;
    SNet_Controller controller;

    [Tooltip ("Sync for every x seconds. Default is 1 second")]
    public float delay = 1f;

    RB rMessage;
    // Use this for initialization
    void Start ()
    {
        rBody = GetComponent<Rigidbody>();
        identity = GetComponent<SNet_Identity>();
        controller = GetComponent<SNet_Controller>();
        lastAngularVelocity = rBody.angularVelocity;
        lastVelocity = rBody.velocity;

        rMessage = new RB(identity.identity);

        nextUpdate = Time.time + Random.Range(1f, 3f);

        list.Add(this);
    }

    private void OnDestroy()
    {
        list.Remove(this);
    }

    [HideInInspector]
    public float nextUpdate;

    Vector3 lastAngularVelocity;
    Vector3 lastVelocity;

    /// <summary>
    /// Use this method when a new player connected. This will update the syncer but don't make Steam Networks angry, so use Randoms.
    /// </summary>
    public void ReUpdate()
    {
        nextUpdate = Time.time + Random.Range(3f, 6f);
        lastAngularVelocity = Vector3.zero;
        lastVelocity = Vector3.zero;
    }

    void Update()
    {
        if (SNet_Network.instance == null)
            return;

        if (rBody == null)
            return;

        if (nextUpdate > Time.time)
            return;

        if (identity.vehicle != null)
        {
            if (identity.vehicle.vehicle.p[0] == 0 && !SNet_Network.instance.isHost())
                return; // No driver, only host can sync this.
            if (identity.vehicle.vehicle.p[0] != 0 && !identity.vehicle.isLocalVehicle)
                return; // There is a driver but I'm not the driver.
        }
        else if ((controller != null && !controller.isLocalPlayer) || (controller == null && !SNet_Network.instance.isHost()))
        {
            return;
        }

        nextUpdate = Time.time + delay;

        if (Vector3.Distance (lastAngularVelocity, rBody.angularVelocity) > 0.1f || Vector3.Distance (lastVelocity, rBody.velocity) > 0.1f)
        {
            lastAngularVelocity = rBody.angularVelocity;
            lastVelocity = rBody.velocity;

            rMessage.a = rBody.angularVelocity;
            rMessage.v = rBody.velocity;

            SNet_Network.instance.Send_Message(rMessage);
        }
    }
}
