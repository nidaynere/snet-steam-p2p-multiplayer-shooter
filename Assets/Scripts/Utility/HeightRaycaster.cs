/*! 
@author EasyMOBA <easymoba.com>
@lastupdate 13 February 2018
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// HeightRaycaster used by Player Controller (SNet_Controller). It has some ready methods.
/// </summary>
public class HeightRaycaster : MonoBehaviour
{
    public static LayerMask ground = 1 << 0;
    /// <summary>
    /// Current distance to ground.
    /// </summary>
    /// <param name="pos">Position</param>
    /// <returns></returns>
    public static float DistToGround(Vector3 pos)
    {
        Vector3 tPos = pos;
        tPos.y = 1000;
        RaycastHit rh;
        if (Physics.Raycast(tPos, -Vector3.up, out rh, 1000, ground))
        {
            return pos.y - rh.point.y;
        }

        return pos.y;
    }

    /// <summary>
    /// Check if the position is near to ground
    /// </summary>
    /// <param name="pos">position</param>
    /// <param name="offset">groundOffset</param>
    /// <returns></returns>
    public static bool isGrounded (Vector3 pos, float offset = 0.25f)
    {
        Vector3 tPos = pos;
        tPos.y = 1000;
        RaycastHit rh;
        if (Physics.Raycast(tPos, -Vector3.up, out rh, 1000, ground))
        {
            return (pos.y - rh.point.y) < 0.25f;
        }

        return false;
    }

    /// <summary>
    /// Get ground height in world space.
    /// </summary>
    /// <param name="pos">Position</param>
    /// <returns></returns>
    public static float GroundY(Vector3 pos)
    {
        Vector3 tPos = pos;
        tPos.y = 1000;
        RaycastHit rh;
        if (Physics.Raycast(tPos, -Vector3.up, out rh, 1000))
        {
            return rh.point.y;
        }

        return pos.y;
    }
}
