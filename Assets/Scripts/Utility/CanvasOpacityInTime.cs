/*! 
@author EasyMOBA <easymoba.com>
@lastupdate 13 February 2018
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// We use this only for incoming damage effect for UI
/// </summary>
public class CanvasOpacityInTime : MonoBehaviour
{
    [Tooltip ("Target opacity")]
    public float tOpacity;
    [Tooltip ("Opacity reach speed")]
    public float tSpeed;

    public float hideAfter = 0;

    CanvasGroup cg;
    float startAt;
	// Use this for initialization
	void Start () {
        startAt = Time.time;
        cg = GetComponent<CanvasGroup>();
	}
	
	// Update is called once per frame
	void Update ()
    {
        if (startAt + hideAfter < Time.time)
        cg.alpha = Mathf.Lerp(cg.alpha, tOpacity, tSpeed * Time.deltaTime);
	}
}
