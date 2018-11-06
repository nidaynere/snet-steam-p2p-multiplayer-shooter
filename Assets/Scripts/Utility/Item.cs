/*! 
@author EasyMOBA <easymoba.com>
@lastupdate 13 February 2018
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lootable and inventory item.
/// </summary>
[RequireComponent (typeof (SNet_Identity))]
public class Item : MonoBehaviour
{
    /// <summary>
    /// Equippable
    /// </summary>
    [System.Serializable]
    public class PlayerItem : SNet_Network.SNetMessage
    {
        public enum ItemType
        {
            Bullet, // Used for weapons
            Knife, // Used for Melee
            Throwable, // Throw prefab towards
        }

        /// <summary>
        /// Item type: Bullet, Knife (Melee), Throwable
        /// Bullets & Knifes makes a raycast towards of the firepoint to hit.
        /// Throwables instantiates a throwable prefab over the network. You can check the Throwable_ prefabs in Resources/Spawnables/Items/
        /// </summary>
        public ItemType itemType = ItemType.Bullet; // default is bullet
        public string prefabName;

        /// <summary>
        /// Default ammo
        /// </summary>
        public ushort ammo;

        /// <summary>
        /// Maximum ammo the user can hold
        /// </summary>
        public ushort maxammo;

        /// <summary>
        /// Shooting speed, 0.5f means 2 shots in a second.
        /// </summary>
        public float fireRate = 0.5f;

        /// <summary>
        /// Is fireable or its a flashlight or something?
        /// </summary>
        public bool fireable = true;

        /// <summary>
        /// Damage
        /// </summary>
        public ushort damage = 5;

        /// <summary>
        /// Range works for Bullet and Knife types only. Throwables takes its force from ThrowForce
        /// </summary>
        public float range = 40;

        /// <summary>
        /// If you want to enable cross hair for this item.
        /// </summary>
        public bool crossHairEnabled = true;
        [Tooltip ("Default crosshair panel name inside gameObject: SteamClient->Canvas->panel_Game->crossHair->normal->")]
        public string crossHair_Normal = "Default";
        [Tooltip("Default crosshair panel name inside gameObject: SteamClient->Canvas->panel_Game->crossHair->aim->")]
        public string crossHair_Aimed = "Default";

        [Tooltip ("Is this item aimable by right mouse click?")]
        /// <summary>
        /// Is this item aimable by right mouse click?
        /// </summary>
        public bool aimable = false;

        [Tooltip ("Throwable rotation addition. Throwables may want to go up.")]
        /// <summary>
        /// Throwable rotation addition. Throwables may want to go up.
        /// </summary>
        public Vector3 throwableRotation;

        [Tooltip ("Requires ammo?")]
        /// <summary>
        /// Requires ammo?
        /// </summary>
        public bool countable = true; // It means it uses ammo. Generally false on melee weapons

        [Tooltip("Used to make the shoots match the animation. Shooting shoots instantly, but for example you may want to wait for grenade animation to shoot.")]
        /// <summary>
        /// Used to make the shoots match the animation. Shooting shoots instantly, but for example you may want to wait for grenade animation to shoot.
        /// </summary>
        public float shootDelay = 0f;

        [Tooltip("Melee, Gun, Explosive, Unarmed, Spear")]
        public string animState = "Unarmed";
        /*
         * Melee
         * Gun
         * Explosive
         * Unarmed
         * Spear
         * */

        /// <summary>
        /// Which prefab will be instanced when its fired?
        /// </summary>
        public string ThrowPrefab; // Must be placed at Resources/Throwables/

        /// <summary>
        /// ThrowForce means the starting forward force of throwable
        /// </summary>
        public float ThrowForce = 10;

        /// <summary>
        /// This item will be instantiated on which bone?
        /// </summary>
        public HumanBodyBones targetBone;

        /// <summary>
        /// Dont touch this
        /// </summary>
        [HideInInspector]
        public ulong looter; // looter's id
    }

    public PlayerItem item;
    public AudioClip fireSound;
    public ParticleSystem fireParticle;

    [Tooltip ("Must be assigned. Bullet & knifes rays will start from here and throwable prefabs will be instanced here.")]
    public Transform firePoint;

    private void Start()
    {
        if (firePoint == null)
            firePoint = transform;
    }
}
