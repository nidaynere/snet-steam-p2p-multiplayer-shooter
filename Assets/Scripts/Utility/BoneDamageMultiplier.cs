/*! 
@author EasyMOBA <easymoba.com>
@lastupdate 13 February 2018
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// When user hit to another user on this body parts, this is the damage multiplier. Headshot x3 by default.
/// </summary>
public class BoneDamageMultiplier : MonoBehaviour
{
    public float damageModifier = 1;

    private void Awake()
    {
        gameObject.layer = 9; // Player bone layer
    }
}
