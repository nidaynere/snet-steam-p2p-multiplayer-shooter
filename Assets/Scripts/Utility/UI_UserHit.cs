/*! 
@author EasyMOBA <easymoba.com>
@lastupdate 13 February 2018
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// When SNet_Controller.user got hit, this UI effect will be shown.
/// </summary>
public class UI_UserHit : MonoBehaviour
{
    public static UI_UserHit instance;
	// Use this for initialization
	void Start ()
    {
        instance = this;
	}

    public CanvasGroup panel;
    public Transform incoming;
}
