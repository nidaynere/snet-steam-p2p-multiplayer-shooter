/*! 
@author EasyMOBA <easymoba.com>
@lastupdate 13 February 2018
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// I could not find a way to block input while we are focused on a UI Input Field.
/// </summary>
public class InputBlocker : MonoBehaviour {

    /// <summary>
    /// Don't allow the input keys if we focused on a InputBlocker.
    /// Returns true if we are on a UI_InputBlocker.
    /// </summary>
    /// <returns></returns>
    public static bool isBlocked()
    {
        return (EventSystem.current.currentSelectedGameObject != null && EventSystem.current.currentSelectedGameObject.CompareTag("UI_InputBlocker"));
    }
}
