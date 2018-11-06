/*! 
@author EasyMOBA <easymoba.com>
@lastupdate 13 February 2018
*/

using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI object interaction script.
/// Simply raycasts forward of camera.
/// </summary>
public class Interactor : MonoBehaviour
{
    public class Interact : SNet_Network.SNetMessage
    {
        public Interact(ulong _id)
        {
            i = _id;
        }
    }

    public GameObject panel_Interact;
    public Text infoText;
    GameObject interactable = null;

    LayerMask lm = 1 << 12;
    // Update is called once per frame
    void Update ()
    {
        if (SNet_Controller.user != null && !SNet_Controller.user.isDead)
        {
            if (SNet_Controller.user.currentVehicle != null)
            {
                SetInteractable(null);

                if (Input.GetButtonDown("Interact") && !InputBlocker.isBlocked ())
                {
                    SNet_Network.instance.Send_Message(new Interact(SNet_Controller.user.currentVehicle.identity), SNet_Network.currentHost);
                }
            }
            else
            {
                float thickness = 0.5f;
                RaycastHit hit;

                if (Physics.SphereCast(SNet_Controller.user.aimBone.position, thickness, MouseOrbitImproved.instance.transform.forward, out hit, 3, lm))
                {
                    switch (hit.collider.tag)
                    {
                        case "Vehicle":
                            SetInteractable(hit.collider);
                            infoText.text = "Enter the vehicle " + hit.collider.name;
                            break;

                        case "Item":
                            SetInteractable(hit.collider);
                            infoText.text = "Take " + hit.collider.GetComponent<Item>().item.prefabName;
                            break;

                        case "NPC":
                            SetInteractable(hit.collider);
                            infoText.text = "Open Shop";
                            break;
                    }
                }
                else SetInteractable(null);

                if (interactable != null && Input.GetButtonDown("Interact"))
                {
                    SNet_Network.instance.Send_Message(new Interact(interactable.GetComponent<SNet_Identity>().identity), SNet_Network.currentHost);
                }
            }
        }

        panel_Interact.SetActive(interactable != null);
    }

    void SetInteractable(Collider c)
    {
        if (c != null)
            interactable = c.gameObject;
        else interactable = null;
    }
}
