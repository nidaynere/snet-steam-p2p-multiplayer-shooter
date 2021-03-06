/*! 
@author DotTeam <dotteam.pro>
@lastupdate 13 February 2018
*/

/*
 * THIS IS THE MODIFIED VERSION OF https://assetstore.unity.com/packages/tools/physics/car-script-basic-61615
 * Implemented TO SNet Multiplayer. If you want to use a advanced car script, please see the HeliController.cs to create a new vehicle script.
 * */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Facepunch.Steamworks;

/// <summary>
/// This is for wheel colliders.
/// </summary>
[System.Serializable]
public class Dot_Truck : System.Object
{
	public WheelCollider leftWheel;
	public GameObject leftWheelMesh;
	public WheelCollider rightWheel;
	public GameObject rightWheelMesh;

    /// <summary>
    /// Used for 4x4 or 4x2. I usually enable this for front wheels
    /// </summary>
    [Tooltip("Used for 4x4 or 4x2. I usually enable this for front wheels")]
    public bool motor;

    /// <summary>
    /// Don't try to enable this on rear wheels :)
    /// </summary>
    [Tooltip("Don't try to enable this on rear wheels :)")]
    public bool steering;

    /// <summary>
    /// If you need reverse steering.
    /// </summary>
    [Tooltip("If you need reverse steering.")]
    public bool reverseTurn; 
}

public class Dot_Truck_Controller : SNet_Vehicle
{
    /// <summary>
    /// Maximum power
    /// </summary>
    [Tooltip ("Maximum power")]
	public float maxMotorTorque;

    /// <summary>
    /// Maximum steering angle
    /// </summary>
    [Tooltip("Maximum steering angle")]
    public float maxSteeringAngle;

    /// <summary>
    /// List of wheels.
    /// </summary>
    [Tooltip("Maximum power")]
    public List<Dot_Truck> truck_Infos;

    /// <summary>
    /// If there is a steering mesh.
    /// </summary>
    public SteeringMesh steeringMesh;

    /// <summary>
    /// Visualise wheel.
    /// </summary>
    /// <param name="wheelPair">Target wheel object.</param>
	public void VisualizeWheel(Dot_Truck wheelPair)
	{
		Quaternion rot;
		Vector3 pos;
        if (wheelPair.leftWheel != null)
        {
		    wheelPair.leftWheel.GetWorldPose ( out pos, out rot);
            wheelPair.leftWheelMesh.transform.position = pos;
            wheelPair.leftWheelMesh.transform.rotation = rot;
        }

        if (wheelPair.rightWheel != null)
        {
            wheelPair.rightWheel.GetWorldPose(out pos, out rot);
            wheelPair.rightWheelMesh.transform.position = pos;
            wheelPair.rightWheelMesh.transform.rotation = rot;
        }
	}

    private void Awake()
    {
        foreach (Dot_Truck dt in truck_Infos)
        {
            // Player collision fix for wheels.
            // If any player bone touch to wheel, something gone crazy.
            dt.rightWheel.gameObject.layer = 10;
            dt.leftWheel.gameObject.layer = 10;
        }
    }

    /// <summary>
    /// VI Sync rate
    /// </summary>
    float sync;

	public void Update ()
    {
        if (Client.Instance != null)
        {
            if (vehicle.p.Count == 0 || vehicle.p[0] == 0)
            { // If there is no driver, this should stop.
                info.m = Mathf.Clamp(info.m - Time.deltaTime * 100, 0, maxMotorTorque);
                info.s = 0;
                info.b = 100;
            }
        }

        if (isLocalVehicle)
        {
            if (InputBlocker.isBlocked())
            {
                // If we are on a UI Input.
                info.m = 0;
                info.s = 0;
                info.b = 100;
            }
            else
            {
                /*
                 * USE IT FREELY
                 * */
                info.m = maxMotorTorque * Input.GetAxis("Vertical");
                info.a = maxSteeringAngle * Input.GetAxis("Horizontal");
                info.b = Mathf.Abs(Input.GetAxis("Jump"));
                if (info.b > 0.001)
                {
                    info.b = maxMotorTorque;
                    info.m = 0;
                }
                else
                {
                    info.b = 0;
                }
            }
            
            if (sync < Time.time)
            {
                sync = Time.time + 0.1f;
                SNet_Network.instance.Send_Message(info);
            }
        }

		foreach (Dot_Truck truck_Info in truck_Infos)
		{
			if (truck_Info.steering)
            {
                float steer = ((truck_Info.reverseTurn) ? -1 : 1) * info.a;
                if (truck_Info.leftWheel != null)
                    truck_Info.leftWheel.steerAngle = steer;
                if (truck_Info.rightWheel != null)
                    truck_Info.rightWheel.steerAngle = steer;
			}

			if (truck_Info.motor)
			{
                if (truck_Info.leftWheel != null)
				truck_Info.leftWheel.motorTorque = info.m;
                if (truck_Info.rightWheel != null)
				truck_Info.rightWheel.motorTorque = info.m;
			}

            if (truck_Info.leftWheel != null)
			truck_Info.leftWheel.brakeTorque = info.b;
            if (truck_Info.rightWheel != null)
			truck_Info.rightWheel.brakeTorque = info.b;

			VisualizeWheel(truck_Info);
		}

        /// Steer the steering wheel if its not null
        if (steeringMesh != null)
            steeringMesh.Steer(info.a);

        ControlPassengers();
    }
}