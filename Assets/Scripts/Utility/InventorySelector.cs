/*! 
@author EasyMOBA <easymoba.com>
@lastupdate 13 February 2018
*/

using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// It controls the item inventory on top of the screen.
/// </summary>
public class InventorySelector : MonoBehaviour
{
    public static InventorySelector current;

    public RectTransform holder;
    // Update is called once per frame

    public void UpdateInventory()
    {
        int i = 0;
        foreach (Transform t in holder)
        {
            bool fix = (PlayerInventory.localPlayer != null && i < PlayerInventory.localPlayer.inv.l.Count);
            t.gameObject.SetActive(fix);
            i++;

            if (!fix)
                continue;

            t.gameObject.name = PlayerInventory.localPlayer.inv.l[i-1].prefabName;
            t.Find("icon").GetComponent<Image>().sprite = ResourcesLoader.icons.Find(x => x.name == t.gameObject.name);
        }
    }

    private void Awake()
    {
        current = this;
    }

    public void OnEnable()
    {
        UpdateInventory();

        /*
         * UPDATE AMMO
         * */

        foreach (Transform t in holder)
        {
            if (t.gameObject.activeSelf)
            {
                Item.PlayerItem pi = PlayerInventory.localPlayer.inv.l.Find(x => x.prefabName == t.name);
                Text ammo = t.GetComponentInChildren<Text>();
                ammo.enabled = pi.ammo > 0;
                ammo.text = PlayerInventory.localPlayer.inv.l.Find(x => x.prefabName == t.name).ammo.ToString ();
            }
        }
    }

    public void UI_UpdateCurrentAmmo()
    {
        if (PlayerInventory.localPlayer.inv.ci != null && PlayerInventory.localPlayer.inv.ci.prefabName != null)
        {
            Transform t = holder.Find(PlayerInventory.localPlayer.inv.ci.prefabName);
            if (t != null)
                t.GetComponentInChildren<Text>().text = PlayerInventory.localPlayer.inv.ci.ammo.ToString();
        }
        
    }
}
