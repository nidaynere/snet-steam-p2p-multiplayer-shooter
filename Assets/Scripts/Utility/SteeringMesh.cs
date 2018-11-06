/*! 
@author EasyMOBA <easymoba.com>
@lastupdate 13 February 2018
*/

using UnityEngine;

/// <summary>
/// This is part of the vehicles. Check the SampleVehicle prefab in Resources/Spawnables/Objects
/// </summary>
public class SteeringMesh : MonoBehaviour
{
    public Vector3 steeringDirection = new Vector3 (1,0,0);

    /// <summary>
    /// Target quaternion
    /// </summary>
    Quaternion tQ = new Quaternion();

    float lastVal = 0;
    /// <summary>
    /// Rotate the steering wheel.
    /// </summary>
    /// <param name="val"></param>
    public void Steer(float val)
    {
        if (lastVal == val)
            return;

        lastVal = val;
        tQ.eulerAngles = new Vector3 ((steeringDirection.x != 0) ? val : transform.localEulerAngles.x,
        (steeringDirection.y != 0) ? val : transform.localEulerAngles.y,
        (steeringDirection.z != 0) ? val : transform.localEulerAngles.z);

        steerFor = Time.time + 0.5f;
    }

    float steerFor = 0;
	
	// Update is called once per frame
	void Update ()
    {
        if (steerFor > Time.time)
        {
            transform.localRotation = Quaternion.Slerp(transform.localRotation, tQ, 0.2f);
        }
	}
}
