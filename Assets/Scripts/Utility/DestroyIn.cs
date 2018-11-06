/*! 
@author EasyMOBA <easymoba.com>
@lastupdate 13 February 2018
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyIn : MonoBehaviour
{
    public bool hostControlled = false;
    public float destroyTime = 5f;
    // Use this for initialization
    float startTime = 0;
    void Start ()
    {
        startTime = Time.time;
	}

    private void Update()
    {
        if (startTime + destroyTime < Time.time)
        {
            if (hostControlled && !SNet_Network.instance.isHost())
                Destroy(this);
            else
                Destroy(gameObject, destroyTime);
        }
    }
}
