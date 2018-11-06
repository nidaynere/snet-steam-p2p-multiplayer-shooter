/*! 
@author EasyMOBA <easymoba.com>
@lastupdate 13 February 2018
*/

using UnityEngine;

/// <summary>
/// Used for grenades currently. Rotates the grenade like it has thrown
/// </summary>
public class StartRotationForce : MonoBehaviour
{
    public Vector3 force = new Vector3(100, 0, 0); // Default force
    // Use this for initialization
    void Start () {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.AddTorque(force, ForceMode.VelocityChange);
        }
	}
}
