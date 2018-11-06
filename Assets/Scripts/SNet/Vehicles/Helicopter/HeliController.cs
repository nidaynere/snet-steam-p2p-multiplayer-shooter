/*! 
@author EasyMOBA <easymoba.com>
@lastupdate 13 February 2018
*/

using UnityEngine;
using Facepunch.Steamworks;

/// <summary>
/// This is a simple helicopter controller (I suggest you replace this with a nice helicopter controller) 
/// This is made for understand to create a new vehicle controller.
/// New vehicle controllers must inherited SNet_Vehicle for easy-seat, easy hand and foot ik and networked vehicles.
/// Please see the comments on code below to understand how it works.
/// 
/// Important note1: Vehicle controllers cannot has Start() and OnDestroy() function because its used by SNet_Vehicle
/// 
/// Important note2: Vehicles syncs via VI class.
///         public float m; // Use this as gas
///         public float a; // Use this for steering.
///         public float b; // In this helicopter case, we used this as risingPower. Normally it's handbrake for cars
/// </summary>

public class HeliController : SNet_Vehicle
{
    /// <summary>
    /// Maximum distance from ground
    /// </summary>
    [Tooltip ("Maximum distance from ground")]
    public float maxHeight = 100f; // 100 meters

    /// <summary>
    /// Disable if the height lower than distanceOffset
    /// </summary>
    [Tooltip ("Disable if the height lower than distanceOffset")]
    public float distanceOffset = 0.25f;
    
    /// <summary>
    /// Motor power
    /// </summary>
    [Tooltip ("Motor power")]
    public float maxMotorTorque = 50f;

    /// <summary>
    /// Steering power
    /// </summary>
    [Tooltip ("Steering power")]
    public float steeringPower = 30f;

    /// <summary>
    /// Rising power
    /// </summary>
    [Tooltip("Rising power")]
    public float risingPower = 50f;

    /*
     * THESE ARE ONLY FOR ANIMATION
     * */
    public Transform fanTop;
    public Vector3 fanTopRotateAxis = new Vector3(0, 0, 1);
    /*
     * */
    
    /// <summary>
    /// Every vehicle controller must sync themself. This value is syncrate.
    /// </summary>
    float sync;

    // Update is called once per frame
    private void FixedUpdate ()
    {
        if (Client.Instance != null)
        {
            /*
             * If there is no players in vehicle, stop the vehicle
             * */
            if (vehicle.p.Count == 0 || vehicle.p[0] == 0)
            { // No driver
                info.m = Mathf.Clamp(info.m - Time.deltaTime * 100, 0, 1/maxMotorTorque); // Slow down gas
                info.a = 0; // Reset the steering
                info.b = 0; // Let's get land.
            }
        }

        /// SAMPLE HELICOPTER CODE IS STARTING ///

        /// Calculating ground distance for helicopter.
        float groundDist = HeightRaycaster.DistToGround(transform.position);

        if (isLocalVehicle)
        {
            /*
             * Only the driver can control the vehicle
             * *
             */

            if (InputBlocker.isBlocked())
            {
                // If we are on a UI Input.
                info.m = 0;
                info.a = 0;
                info.b = 0;
            }
            else
            {
                /*
                 * USE THE HELICOPTER
                 * */
                float vert = Input.GetAxis("Vertical");
                if (vert > 0 && groundDist > 1) // 1m is for waiting for rising.
                    info.m = maxMotorTorque * vert;
                else info.m = 0;

                info.a = steeringPower * Input.GetAxis("Horizontal");

                if (vert < 0) // descending
                    info.b = vert * risingPower / 2f;
                else
                    info.b = Mathf.Abs(Input.GetAxis("Jump")) * risingPower;
            }

            /*
             * THIS IS IMPORTANT, DRIVER MUST SYNC THE VEHICLE ON NETWORK.
             * */
            if (sync < Time.time)
            {
                sync = Time.time + 0.1f; // Sync rate is ~10 times in one second.
                SNet_Network.instance.Send_Message(info);
            }
            /*
             * */
        }

        identity.transform.Rotate(new Vector3(0, info.a * Time.deltaTime, 0));

        Quaternion tRot = new Quaternion();
        tRot.eulerAngles = new Vector3((info.m > 0) ? 5 : 0, transform.eulerAngles.y, -info.a);

        if (info.m > 0 || groundDist > distanceOffset) // Restore the axises
        {
            identity.transform.rotation = Quaternion.Slerp(transform.rotation, tRot, 0.05f);

            /*
             * THIS IS FOR ANIMATION
             * */
            fanTop.Rotate(fanTopRotateAxis, 720 * Time.deltaTime);
            /*
             * */
        }

        /// Move the helicopter.
        Vector3 force = transform.forward * info.m + Vector3.up * info.b;
        transform.position = identity.rbody.rBody.position + force * Time.deltaTime;

        /// SAMPLE HELICOPTER CODE IS ENDED ///

        /// Call this end of the update loop to update the passengers on vehicle.
        ControlPassengers();
    }
}
