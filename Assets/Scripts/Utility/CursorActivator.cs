/*! 
@author EasyMOBA <easymoba.com>
@lastupdate 13 February 2018
*/

using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// This is a need. When I released one of my games, I didn't care about cursor lock because I don't work with multiple monitors.
/// My friend told me I cannot play the game because when I aim right with mouse and after I clicked to shoot, the game lost the focus and return to the desktop.
/// This is the CursorLocker. When cursor needs, it will show the cursor, otherwise it will lock it.
/// </summary>
public class CursorActivator : MonoBehaviour
{
    public static List<CursorActivator> activators = new List<CursorActivator>();

    public bool keepOnUpdate;

    public bool onEnabled;
    public bool onDisabled;

    public CursorLockMode enabledCursorMode;
    public CursorLockMode disabledCursorMode;

    public int order = 0; // higher than 0 means the priority
    void SortByOrder()
    {
        activators = activators.OrderByDescending(x => x.order).ToList();
    }

    private void OnEnable()
    {
        activators.Add(this);
        SortByOrder();
        Set();
    }

    void Set ()
    {
        Cursor.visible = onEnabled;
        Cursor.lockState = enabledCursorMode;
    }

    private void OnDisable()
    {
        activators.Remove(this);
        SortByOrder();

        Cursor.visible = onDisabled;
        Cursor.lockState = disabledCursorMode;
    }

    private void Update()
    {
        if (keepOnUpdate && activators[0] == this)
        {
            Set();
        }
    }
}
