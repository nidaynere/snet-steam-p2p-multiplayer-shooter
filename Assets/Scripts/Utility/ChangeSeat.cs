/*! 
@author EasyMOBA <easymoba.com>
@lastupdate 13 February 2018
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// This class is used for changing seats on vehicle with F buttons.
/// </summary>
public class ChangeSeat : MonoBehaviour
{
    [System.Serializable]
    public class CSRequest : SNet_Network.SNetMessage
    {
        public CSRequest (int _index)
        {
            idx = _index;
        }

        /// <summary>
        /// Target index to seat on vehicle
        /// </summary>
        public int idx;
    }

    /// <summary>
    /// Current UI objects of seats
    /// </summary>
    public static List<ChangeSeat> UI_Seats = new List<ChangeSeat>();

    /// <summary>
    /// Target input key
    /// </summary>
    string key = "";

    /// Current seat index
    public int index = 0;

    /// <summary>
    /// This is for flashing on vehicles.
    /// </summary>
    public Transform point;

    private void Start()
    {
        key = "f" + (index + 1);

        // Create world point
        point = Instantiate(transform, transform);
        Destroy (point.GetComponent<ChangeSeat>());
        point.gameObject.SetActive(false);
        //

        UI_Seats.Add(this);
    }

    private void OnDestroy()
    {
        UI_Seats.Remove(this);
    }

    bool interactionStarted = false;
    float fillValue = 0;
    // Update is called once per frame
    void LateUpdate ()
    {
        if (Input.GetKeyDown(key))
        {
            interactionStarted = true;
        }

        if (interactionStarted)
        {
            fillValue += Time.deltaTime;

            int c = 0;
            foreach (ChangeSeat cs in UI_Seats)
            { // Deactivate UI seat points on world
                if (cs != null && cs.point != null)
                {
                    cs.point.transform.position = Camera.main.WorldToScreenPoint(SNet_Controller.user.currentVehicle.vehicle.seatPoints[cs.index].position);

                    if (cs == this)
                        cs.point.Find("filler").GetComponent<Image>().fillAmount = fillValue;

                    if (!cs.point.gameObject.activeSelf)
                    {
                        cs.point.gameObject.SetActive(true);
                    }
                }

                c++;
            }

            if (fillValue >= 1)
            {
                interactionStarted = false;

                // Send change seat request to master client.
                SNet_Network.instance.Send_Message(new CSRequest(index), SNet_Network.currentHost);
            }
        }

        if (Input.GetKeyUp(key) || (fillValue > 0 && !interactionStarted))
        {
            interactionStarted = false;
            fillValue = 0;

            foreach (ChangeSeat cs in UI_Seats)
            { // Deactivate UI seat points on world
                if (cs != null && cs.point != null)
                {
                    if (cs.point.gameObject.activeSelf)
                    {
                        cs.point.gameObject.SetActive(false);
                    }

                    cs.point.Find("filler").GetComponent<Image>().fillAmount = 0;
                }
            }
        }
	}
}
