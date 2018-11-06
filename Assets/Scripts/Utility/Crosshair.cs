/*! 
@author EasyMOBA <easymoba.com>
@lastupdate 13 February 2018
*/

using UnityEngine;

/// <summary>
/// Controls the crosshair panel for weapons.
/// </summary>
public class Crosshair : MonoBehaviour
{
    public static Crosshair instance;
    public UIVisibility aimed;
    public UIVisibility normal;

    private void Start()
    {
        if (instance != null)
            return;

        instance = this;
    }

    // Update is called once per frame
    void Update ()
    {
        bool cHair = (SNet_Controller.user != null && (SNet_Controller.user.currentVehicle == null || SNet_Controller.user.freeOnSeat) && !SNet_Controller.user.isDead && SNet_Controller.user.inventory.inv.ci.crossHairEnabled);
        aimed.Open (cHair && SNet_Controller.user.aimed);
        normal.Open(cHair && !SNet_Controller.user.aimed);
    }
}
