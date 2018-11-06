/*! 
@author EasyMOBA <easymoba.com>
@lastupdate 13 February 2018
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Base of the networked vehicle.
/// </summary>
[RequireComponent (typeof (SNet_Identity))]
public class SNet_Vehicle : MonoBehaviour
{
    public VI info = null;

    /// <summary>
    /// Vehicle Info, motor, gas, brake
    /// </summary>
    [System.Serializable]
    public class VI : SNet_Network.SNetMessage
    {
        public VI(ulong _id)
        {
            i = _id;
        }

        /// <summary>
        /// Motor (gas)
        /// </summary>
        public float m;
        /// <summary>
        /// Steering
        /// </summary>
        public float a;
        /// <summary>
        /// Brake
        /// </summary>
        public float b;
    }

    /// <summary>
    /// Vehicle passengers data
    /// </summary>
    [System.Serializable]
    public class V : SNet_Network.SNetMessage
    {
        public V (ulong _identity)
        {
            i = _identity;
        }

        /// <summary>
        /// Steam id of passengers
        /// </summary>
        public List<ulong> p = new List<ulong>();
    }

    /// <summary>
    /// Vehicle health
    /// </summary>
    [System.Serializable]
    public class VH : SNet_Network.SNetMessage
    {
        public VH(ulong _identity, int health)
        {
            i = _identity;
            v = health;
        }

        public int v;
    }

    public List<SNet_Identity> passengerControllers = new List<SNet_Identity>();

    /// <summary>
    /// If the client is on the driver seat.
    /// </summary>
    public bool isLocalVehicle;

    public V vehicle;

    /*
     * HEALTH IS CONTROLLED BY THE HOST CLIENT ONLY
     * */
    public int maxHealth = 1000;
    /// <summary>
    /// Armor means damage/armor. If the vehicle get 1000 damage, armor=5 makes it 200.
    /// </summary>
    [Tooltip ("Armor means damage/armor. If the vehicle get 1000 damage, armor=5 makes it 200.")]
    public float armor = 5;

    public Transform createOnExplode;

    VH _health;
    [HideInInspector]
    public VH health
    {
        get
        {
            return _health;
        }

        set
        {
            _health = value;

            if (_health.v <= 0)
            {
                if (createOnExplode != null)
                Instantiate(createOnExplode, transform.position, createOnExplode.rotation);

                /*
                 * VEHICLE EXPLODED. KILL THE PASSENGERS
                 * */
                foreach (SNet_Identity controller in passengerControllers)
                {
                    SNet_Controller.UserHit uh = new SNet_Controller.UserHit(controller.identity);
                    uh.v = controller.controller.health.v;
                    SNet_Network.instance.Send_Message(uh);
                }
                /*
                 * */

                Destroy(gameObject);
                return;
            }
        }
    }
    /*
     * */

    [Tooltip ("This is the seat points.")]
    public List<Transform> seatPoints = new List<Transform>();

    [Tooltip("Exit points used when leaving the car")]
    public List<Transform> exitPoints = new List<Transform>();

    [HideInInspector]
    public SNet_Identity identity;
    private void Start ()
    {
        identity = GetComponent<SNet_Identity>();
        info = new VI(identity.identity);
        vehicle = new V(identity.identity);
        health = new VH(identity.identity, maxHealth);

        vehicle.p = new List<ulong>();
        for (int i = 0; i < seatPoints.Count; i++)
            vehicle.p.Add(0);

        MonoBehaviourExtensions.Invoke(this, VehicleUpdate, 3f);
    }

    /// <summary>
    /// Syncs vehicle data and update the toucheds
    /// </summary>
    public void VehicleUpdate()
    {
        if (!SNet_Network.instance.isHost())
            return; // Only host can sync vehicle data.

        for (int i = 0; i < vehicle.p.Count; i++)
        {
            if (vehicle.p[i] == 0)
                continue;

            if (SNet_Identity.list.Find(x => x.identity == vehicle.p[i]) == null)
            {
                // Missing passenger
                vehicle.p[i] = 0;
            }
        }

        if (vehicle != null && vehicle.p.Count > 0)
            SNet_Network.instance.Send_Message(vehicle);

        if (vehicle != null)
            SNet_Network.instance.Send_Message(health);

        MonoBehaviourExtensions.Invoke(this, VehicleUpdate, 3f);
    }

    public void ControlPassengers()
    {
        foreach (SNet_Identity id in passengerControllers)
        {
            if (id != null && id.controller != null)
                id.controller.OnVehicleSync();
        }
    }

    private void OnDestroy()
    {
        if (identity == null || identity.identity == 0)
            return;

        foreach (SNet_Identity identity in passengerControllers)
        {
            if (identity.controller != null && identity.controller.currentVehicle != null)
            // Remove the vehicle from passengers
            identity.controller.currentVehicle = null;
        }
    }
}
