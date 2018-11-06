/*! 
@author EasyMOBA <easymoba.com>
@lastupdate 13 February 2018
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This script is for falling and impacting damage but it's not working properly sometimes.
/// </summary>
public class UserFallDamage : MonoBehaviour
{
    SNet_Identity identity;
	// Use this for initialization
	void Start ()
    {
        identity = GetComponent<SNet_Identity>();
	}

    // Set when Rigidbody is not active
    float checkDelay = 0;
    private void Update()
    {
        if (identity.rbody.rBody.isKinematic)
            checkDelay = Time.time + 1;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (checkDelay > Time.time)
            return;

        if (identity == null || identity.controller == null || !identity.controller.isLocalPlayer)
            return;

        float impulse = collision.impulse.magnitude;
        if (impulse > 10)
        {
            SNet_Controller.UserHit userHit = new SNet_Controller.UserHit(identity.identity);
            userHit.v = Mathf.RoundToInt (((collision.rigidbody != null) ? Mathf.Sqrt (collision.rigidbody.mass) : 1) *impulse*impulse / 5f);

            if (collision.rigidbody != null)
                userHit.v = Mathf.RoundToInt (userHit.v * collision.rigidbody.mass);

            userHit.h = transform.position + transform.up;
            SNet_Network.instance.Send_Message(userHit);
        }
    }
}
