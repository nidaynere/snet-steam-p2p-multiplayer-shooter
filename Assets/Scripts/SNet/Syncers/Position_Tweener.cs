/*! 
@author EasyMOBA <easymoba.com>
@lastupdate 13 February 2018
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Used for lag simulation by SNet_Transform while syncing positions. Do not use this for vehicles etc. because this may break the vehicle controllers.
/// </summary>
public class Position_Tweener : MonoBehaviour
{
    Vector3 direction;
    Vector3 t;

    public bool tweening = false;

    /// <summary>
    /// Used to tween objects.
    /// </summary>
    /// <param name="v">Target position</param>
    /// <param name="speed">Tween speed, trust the default</param>
    public void TweenFor(Vector3 v, float speed = 8f)
    {
        tweenSpeed = speed;
        lastDist = 1000;
        t = v;
        direction = t - transform.position;
        dist = Vector3.Distance (transform.position, v);
        direction = direction.normalized;

        if (dist >= 4)
        {
            transform.position = v;
            tweening = false;
        }
        else
        {
            tweening = true;
        }
    }

    float tweenSpeed = 8f;
    float lastDist;
    float dist;

	// Update is called once per frame
	void Update ()
    {
        if (!tweening)
            return;

        if (dist > 0.1f)
        {
            transform.position += direction * tweenSpeed * Time.deltaTime;

            dist = Vector3.Distance (transform.position, t);

            if (lastDist < dist)
            {
                tweening = false;
            }
            else
            {
                lastDist = dist;
            }
        }
	}

    private void OnCollisionEnter(Collision collision)
    {
        if (tweening && collision.collider.gameObject.layer != 0)
        {
            transform.position = t;
            tweening = false;
        }
    }
}
