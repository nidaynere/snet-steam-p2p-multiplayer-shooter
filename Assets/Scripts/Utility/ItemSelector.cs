/*! 
@author EasyMOBA <easymoba.com>
@lastupdate 13 February 2018
*/

using UnityEngine;

/// <summary>
/// Inventory item selector. this is a key listener.
/// </summary>
public class ItemSelector : MonoBehaviour
{
    public KeyCode keyCode;
	// Use this for initialization
	void Start ()
    {
        int sbl = transform.GetSiblingIndex();
        keyCode = (KeyCode) (sbl + 49);
        transform.Find("key").GetComponentInChildren<UnityEngine.UI.Text>().text = (sbl+1).ToString();
	}
	
	// Update is called once per frame
	void Update ()
    {
        if (Input.GetKeyDown(keyCode))
        {
            SNet_Controller.user.inventory.Request_Equip(gameObject.name);
        }
	}
}
