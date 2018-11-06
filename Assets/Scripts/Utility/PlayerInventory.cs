using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerInventory : MonoBehaviour
{
    /// <summary>
    /// Inventory status
    /// </summary>
    [System.Serializable]
    public class IS : SNet_Network.SNetMessage
    {
        public List<Item.PlayerItem> l = new List<Item.PlayerItem>();
        public Item.PlayerItem ci;
    }

    /// <summary>
    /// Player shoot request
    /// </summary>
    [System.Serializable]
    public class St : SNet_Network.SNetMessage
    {
        
    }

    [System.Serializable]
    public class EquipItem : SNet_Network.SNetMessage
    {
        public string prefabName;

        public EquipItem(string _prefabName)
        {
            prefabName = _prefabName;
        }
    }

    public IS inv = new IS();

    SNet_Identity identity;
    private void Start ()
    {
        identity = GetComponent<SNet_Identity>();

        if (isLocalPlayer)
        {
            localPlayer = this;
            UpdateCurrentAmmo();

            if (InventorySelector.current != null)
            {
                InventorySelector.current.UpdateInventory();
            }
        }
    }

    public bool isLocalPlayer = false;
    public static PlayerInventory localPlayer;

    public void AddItem(Item.PlayerItem item)
    {
        Item.PlayerItem current = inv.l.Find(x => x.prefabName == item.prefabName);
        if (current != null)
        {
            current.ammo = (ushort) Mathf.Clamp(current.ammo + item.ammo, 0, item.maxammo);
        }
        else
        {
            inv.l.Add(item);
            /*
             * EQUIP IF IT's THE FIRST ITEM
             * */
            if (isLocalPlayer)
            {
                if (inv.l.Count == 1)
                {
                    Request_Equip(item.prefabName);
                }
            }
        }

        if (isLocalPlayer && InventorySelector.current != null)
        {
            if (!UpdateCurrentAmmo())
            {
                InventorySelector.current.UpdateInventory();
                InventorySelector.current.OnEnable(); // Update ammos
                SyncInventory();
            }
        }
    }

    public void Request_Equip(string prefabName)
    {
        if (!identity.controller.isDead && (identity.controller.currentVehicle == null || identity.controller.freeOnSeat))
        SNet_Network.instance.Send_Message(new EquipItem(prefabName));
    }

    public Item prefab;

    public void Equip(string prefabName)
    {
        if (prefab != null)
        {
            if (prefab.item.prefabName == prefabName)
                return;

            prefab.SendMessage("Detach");
            Destroy(prefab.gameObject);
        }

        if (string.IsNullOrEmpty(prefabName))
        {
            if (prefab != null)
            {
                Destroy(prefab.gameObject);
            }
        }

        Item.PlayerItem willEquip = inv.l.Find(x => x.prefabName == prefabName);
        if (willEquip == null)
            willEquip = new Item.PlayerItem();

        inv.ci = willEquip;
        identity.controller.isShooting = false;

        prefab = null;

        if (!string.IsNullOrEmpty(prefabName))
        {
            Transform Tprefab = ResourcesLoader.prefabs.Find (x=>x.name == prefabName);
            Transform tBone = identity.animator.animator.GetBoneTransform(Tprefab.GetComponent<Item>().item.targetBone);

            prefab = Instantiate(Tprefab, tBone).GetComponent<Item>();
            prefab.name = prefabName;

            Destroy(prefab.GetComponent<CustomScale>());

            Destroy(prefab.GetComponent<Snet_SceneObject_AutoSpawn>());

            Collider cldr = prefab.GetComponent<Collider>();
            if (cldr != null)
                cldr.enabled = false;

            Rigidbody rb = prefab.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;

            DestroyIn dIn = prefab.GetComponent<DestroyIn>();
            if (dIn != null)
                dIn.enabled = false;

            IKWeapon attacher = prefab.GetComponent<IKWeapon>();
            if (attacher != null)
                attacher.Attach();
        }

        SetState();

        if (isLocalPlayer)
        {
            if (Crosshair.instance == null || Crosshair.instance.normal == null)
                return;

            foreach (Transform t in Crosshair.instance.normal.transform)
            {
                t.GetComponent<UIVisibility>().Open(t.name == inv.ci.crossHair_Normal);
            }

            foreach (Transform t in Crosshair.instance.aimed.transform)
            {
                t.GetComponent<UIVisibility>().Open(t.name == inv.ci.crossHair_Aimed);
            }

            if (!UpdateCurrentAmmo())
            {
                SyncInventory();
            }
        }
    }

    /// <summary>
    /// Update local player's current ammo on UI
    /// </summary>
    /// <returns>If the current item is null, returns false</returns>
    public bool UpdateCurrentAmmo()
    {
        GameObject ammoGO = SNet_Manager.instance.panel_Game.transform.Find("currentAmmo").gameObject;
        if (inv.ci != null)
        {
            ammoGO.SetActive(inv.ci.countable);
            ammoGO.GetComponentInChildren<Text>().text = inv.ci.ammo.ToString();

            if (inv.ci.countable && inv.ci.ammo <= 0)
            {
                inv.l.Remove(inv.ci);
                inv.ci = new Item.PlayerItem();
                if (prefab != null)
                {
                    Destroy(prefab.gameObject);
                }

                if (inv.l.Count > 0)
                Request_Equip(inv.l[0].prefabName);

                if (InventorySelector.current != null)
                    InventorySelector.current.UpdateInventory();
                return true;
            }
        }
        else ammoGO.SetActive(false);

        return false;
    }

    public void SetState()
    {
        int c = identity.animator.animator.parameterCount;
        for (int i = 0; i < c; i++)
        {
            string paramName = identity.animator.animator.GetParameter(i).name;

            if (paramName.Contains("Item_"))
            {
                string tParam = "Item_" + inv.ci.animState;
                bool isEquip = tParam == paramName;

                if (isEquip != identity.animator.animator.GetBool (paramName))
                {
                    identity.animator.SetBool(paramName, isEquip);
                    identity.animator.SetTrigger("ChangeItem");
                }
            }
        }
    }

    public void SyncInventory()
    {
        SNet_Network.instance.Send_Message(inv);
    }
}
