/*! 
@author EasyMOBA <easymoba.com>
@lastupdate 15 February 2018
*/

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Facepunch.Steamworks;

/// <summary>
/// A complex Player Controller script of SNet.
/// Controls player movements, inventory, vehicle sync, user health, aiming with mouse, 
/// </summary>
/// 
public class SNet_Controller : MonoBehaviour
{
    public static List<SNet_Controller> list = new List<SNet_Controller>();

    /// <summary>
    /// Local player, reach the local player from everywhere.
    /// </summary>
    public static SNet_Controller user;

    /// <summary>
    /// Player aim
    /// </summary>
    [System.Serializable]
    public class A : SNet_Network.SNetMessage
    {
        public float y;
    }

    /// <summary>
    /// Users health
    /// </summary>
    [System.Serializable]
    public class Health : SNet_Network.SNetMessage
    {
        public Health(ulong _identity)
        {
            i = _identity;
        }

        public int v = 100; // Default health is 100
    }

    /// <summary>
    /// User hit info
    /// </summary>
    public class UserHit : SNet_Network.SNetMessage
    {
        public UserHit(ulong _identity)
        {
            i = _identity;
        }

        /// <summary>
        /// Hit amount
        /// </summary>
        public int v;

        /// <summary>
        /// Hit position will be used for incoming damage effect
        /// </summary>
        [HideInInspector]
        public Vector3 h;
    }

    [HideInInspector]
    public SNet_Identity identity;

    bool _isLocalPlayer;
    public bool isLocalPlayer
    {
        get
        {
            return _isLocalPlayer;
        }

        set
        {
            _isLocalPlayer = value;
            if (value)
            {
                // Set the local user
                user = this;
            }
        }
    }

    [HideInInspector]
    public Transform aimBone;
    [HideInInspector]
    public A aim;
    [HideInInspector]
    public A syncedAim; // Not used for local player
    [HideInInspector]
    public bool isDead;

    Health _health;
    public Health health
    {
        get
        {
            return _health;
        }

        set
        {
            if (isLocalPlayer)
            {
                if (_health == null || (isDead && value.v > 0))
                { // Respawn
                    MouseOrbitImproved.instance.CameraMode(false);
                }

                if (_health != null)
                {
                    if (value.v <= 0 && !isDead)
                    {
                        MouseOrbitImproved.instance.CameraMode(true);
                    }
                }

                SNet_Manager.instance.panel_Game.transform.Find("health").GetComponentInChildren<Text>().text = value.v.ToString ();
            }

            _health = value;

            if (!isDead && _health.v <= 0)
            {
                if (SNet_Network.instance.isHost())
                {
                    if (currentVehicle != null)
                    {
                        int idx = currentVehicle.vehicle.vehicle.p.FindIndex(x => x == identity.identity);
                        if (idx != -1)
                            currentVehicle.vehicle.vehicle.p[idx] = 0;

                        currentVehicle.vehicle.VehicleUpdate();
                    }

                    /*
                    * DROP ITEMS IN INVENTORY
                    * */
                    foreach (Item.PlayerItem pi in inventory.inv.l)
                    {
                        if (pi.prefabName == GameMap.instance.startingItem)
                            continue;

                        SNet_Network.Spawn_Item si = new SNet_Network.Spawn_Item
                            (pi.prefabName, transform.position + Vector3.up, Random.rotation, Vector3.zero, pi.ammo);
                        SNet_Network.instance.Send_Message(si);
                    }
                }

                inventory.inv.l.RemoveRange (string.IsNullOrEmpty (GameMap.instance.startingItem) ? 0 : 1, inventory.inv.l.Count-1); // Clear inventory

                if (isLocalPlayer)
                {
                    MonoBehaviourExtensions.Invoke(this, Resurrect, GameMap.instance.PlayerResurrectTime);

                    inventory.SyncInventory();
                    inventory.Equip("");
                    inventory.UpdateCurrentAmmo();
                    if (InventorySelector.current)
                        InventorySelector.current.UpdateInventory();
                }
            }

            isDead = value.v <= 0;

            /*
            * OPTIONABLE RAGDOLL
            * */
            if (identity.ragdoll != null)
            {
                identity.ragdoll.ragdolled = isDead;
            }
        }
    }

    void Resurrect()
    {
        // Local player controls the position
        transform.position = GameMap.getSpawnPosition();
        Health resurrect = new Health(identity.identity);
        SNet_Network.instance.Send_Message(resurrect);

        inventory.Equip(GameMap.instance.startingItem);
    }

    [HideInInspector]
    public float horizontal;
    [HideInInspector]
    public float vertical;
    [HideInInspector]
    public PlayerInventory inventory;
    [HideInInspector]
    public IKFixer ik;
    [HideInInspector]
    public Transform UI_PlayerAgent;
    [HideInInspector]
    public Transform cameraHolder;
    [HideInInspector]
    public Transform focusHolder;
    Vector3 defaultCamPosition;

    void Start()
    {
        list.Add(this);

        cameraHolder = transform.Find("CameraHolder"); // standart camera position
        if (cameraHolder != null)
        defaultCamPosition = cameraHolder.localPosition;
        focusHolder = transform.Find("FocusHolder"); // camera position when right mouse aimed

        aim = new A();
        identity = GetComponent<SNet_Identity>();
        health = new Health(identity.identity);
        inventory = GetComponent<PlayerInventory>();
        ik = GetComponent<IKFixer>();

        // Add default starting item of the map.
        if (!string.IsNullOrEmpty(GameMap.instance.startingItem))
        {
            inventory.AddItem(ResourcesLoader.prefabs.Find(x => x.name == GameMap.instance.startingItem).GetComponent<Item>().item);
        }
        //

        aimBone = identity.animator.animator.GetBoneTransform(HumanBodyBones.Spine);

        if (isLocalPlayer)
        {
            /*
             * Local players & host controlled NPCs must sync themselves
             * */
            MonoBehaviourExtensions.Invoke(this, GeneralSync, 5f);

            // Hide the player spawner panel, because we are already spawned.
            SNet_Manager.instance.panel_Game.transform.Find("playerSpawner").gameObject.SetActive(false);
        }
        else
        {
            // Other players UI
            UI_PlayerAgent = Instantiate(SNet_Manager.instance.UI_PlayerAgent, aimBone.parent);
            UI_PlayerAgent.localPosition = SNet_Manager.instance.UI_PlayerAgent.localPosition;
            UI_PlayerAgent.Find("steamAvatar").GetComponent<Facepunch_Avatar>().Fetch(identity.identity);
            UI_PlayerAgent.Find("steamName").GetComponent<Text>().text = Client.Instance.Friends.GetName(identity.identity);
        }

        /*
         * THIS IS A NEW PLAYER, UPDATE THE SPAWNED IDENTITIES FOR THE NEW PLAYER
         * */

        if (SNet_Network.instance.isHost())
        { // Host will update the spawneds
            Debug.Log ("New player spawned, SpawnAll()");
            int sCount = SNet_Identity.list.Count;
            for (int i = 0; i < sCount; i++)
            {
                if (!SNet_Identity.list[i].controller)
                { // Refresh only non-player objects
                    SNet_Network.Spawn sp = new SNet_Network.Spawn
                        (SNet_Identity.list[i].prefab,
                        SNet_Identity.list[i].transform.position,
                        SNet_Identity.list[i].transform.rotation,
                        SNet_Identity.list[i].transform.localScale,
                        0, true);

                    sp.i = SNet_Identity.list[i].identity;
                    SNet_Network.instance.Send_Message(sp, identity.identity);

                    /*
                     * UPDATE SYNCERS (Transforms, rigidbodies), But DON'T MAKE ANGRY STEAM SERVERS, SO USE RANDOM VALUES
                     * */
                    if (SNet_Identity.list[i].tform != null)
                    {
                        SNet_Identity.list[i].tform.ReUpdate();
                    }

                    if (SNet_Identity.list[i].rbody != null)
                    {
                        SNet_Identity.list[i].rbody.ReUpdate();
                    }
                    
                    if (SNet_Identity.list[i].vehicle != null)
                    {
                        MonoBehaviourExtensions.Invoke(SNet_Identity.list[i].vehicle, SNet_Identity.list[i].vehicle.VehicleUpdate, 3f);
                    }
                }
            }
        }
    }

    void GeneralSync()
    {
        SNet_Network.instance.SpawnPlayer(identity.prefab, transform.position, transform.rotation, transform.localScale);
        MonoBehaviourExtensions.Invoke(this, GeneralSync, 5f);
        MonoBehaviourExtensions.Invoke(this, SyncData, 3f);
    }

    void SyncData()
    {
        if (SNet_Network.instance.isHost())
        {
            /*
             * HOST CONTROLLEDS
             * */
            SNet_Network.instance.Send_Message(health);
        }

        inventory.SyncInventory();
    }

    void Spawner()
    {
        SNet_Network.instance.SpawnPlayer(SNet_Button.SelectedPlayer.gameObject.name, transform.position, transform.rotation, transform.localScale);
    }

    private void OnDestroy()
    {
        if (isLocalPlayer)
        {
            if (SNet_Manager.instance != null)
            // Show the player spawner panel, because our player is de-spawned.
            SNet_Manager.instance.panel_Game.transform.Find("playerSpawner").gameObject.SetActive(true);
        }

        int vd = SNet_Voice.instance.vdList.FindIndex(x => x.id == identity.identity);
        if (vd != -1) // Remove my voice data.
            SNet_Voice.instance.vdList.RemoveAt(vd);

        if (currentVehicle != null)
        {
            currentVehicle.vehicle.VehicleUpdate();
        }

        list.Remove(this);
    }

    /// <summary>
    /// Mouse X sensivity
    /// </summary>
    public float sensivityX = 130;
    /// <summary>
    /// Mouse Y sensivity
    /// </summary>
    public float sensivityY = 0.8f;

    float nextSync_Move;
    float nextSync_Aim;

    IKWeapon seatIK = null;
    Vector3 lastExit_Position;
    float lastExit_Rotation;

    /// <summary>
    /// If this controller on a seat but the seat is free to shoot and aim by IKWeapon.
    /// </summary>
    /// [HideInInspector]
    public bool freeOnSeat;

    SNet_Identity _currentVehicle;

    /// <summary>
    /// If there is a vehicle. Animator will stop (empty animation will be played) and third person controller won't work.
    /// </summary>
    [HideInInspector]
    public SNet_Identity currentVehicle
    {
        get
        {
            return _currentVehicle;
        }

        set
        {
            if (_currentVehicle != null && value == null)
            {
                if (!freeOnSeat)
                {
                    // Leaving the vehicle from a non-free seat, use the default item
                    inventory.Equip(GameMap.instance.startingItem);
                }

                freeOnSeat = false;
            }

            _currentVehicle = value;

            if (value != null)
            {
                horizontal = 0;
                vertical = 0;
                identity.animator.animator.SetFloat("H", 0);
                identity.animator.animator.SetFloat("V", 0);
                identity.animator.animator.SetBool("M", false);
                identity.animator.animator.SetBool("Sprint", false);
            }

            if (isLocalPlayer)
            {
                if (SNet_Manager.instance == null || SNet_Manager.instance.panel_Vehicle == null)
                    return;
                /*
                 * DRAW VEHICLE UI
                 * */
                Transform holder = SNet_Manager.instance.panel_Vehicle.transform.Find("seats");

                if (value == null)
                {
                    // Not in a vehicle anymore.
                    if (SNet_Manager.instance.panel_Vehicle.activeSelf)
                    {
                        SNet_Manager.instance.panel_Vehicle.Close();
                    }
                }
                else
                {
                    /// There is a vehicle. Check UI;

                    SNet_Manager.instance.panel_Vehicle.Open();

                    int pCount = currentVehicle.vehicle.vehicle.p.Count;

                    /*
                     * RE-INSTANTIATE PANEL
                     * */

                    foreach (Transform t in holder)
                        Destroy(t.gameObject);

                    for (int i = 0; i < pCount; i++)
                    {
                        Transform seat = Instantiate(SNet_Manager.instance.UI_VehicleSeat, holder);

                        seat.GetComponentInChildren<ChangeSeat>().index = i;

                        bool isMySeat = currentVehicle.vehicle.vehicle.p[i] == Client.Instance.SteamId;
                        bool seatIsHolded = currentVehicle.vehicle.vehicle.p[i] != 0;

                        seat.Find("mySeat").gameObject.SetActive(isMySeat);
                        seat.Find("notAvailable").gameObject.SetActive(!isMySeat && seatIsHolded);

                        GameObject kk = seat.Find("keyboardKey").gameObject;
                        kk.SetActive(!seatIsHolded);
                        kk.GetComponentInChildren<Text>().text = "F" + (i + 1);

                        IKWeapon _seatIk = currentVehicle.vehicle.seatPoints[i].GetComponent<IKWeapon>();
                        seat.Find("weaponblocked").gameObject.SetActive(_seatIk == null || !_seatIk.FreeOnSeat);

                        Facepunch_Avatar avatar = seat.Find("steamAvatar").GetComponent<Facepunch_Avatar>();
                        avatar.gameObject.SetActive(seatIsHolded);
                        if (currentVehicle.vehicle.vehicle.p[i] != 0)
                            avatar.Fetch(currentVehicle.vehicle.vehicle.p[i]);
                    }
                }
            }

            /*
             * */

            if (this == null)
                return; // Unity editor fix. This object is going to be destroyed. Pass this.

            if (value == null && lastExit_Position != -Vector3.up)
            {
                float ground = HeightRaycaster.DistToGround(lastExit_Position) + 1;
                if (lastExit_Position.y < ground)
                { // Under ground fix
                    lastExit_Position.y = ground;
                }

                transform.position = lastExit_Position;
                transform.eulerAngles = new Vector3(0, lastExit_Rotation, 0);

                lastExit_Position = -Vector3.up;
                lastExit_Rotation = 0;
            }

            if (value != null)
            {
                // Possible IK on my seat
                int mySeat = currentVehicle.vehicle.vehicle.p.FindIndex(x => x == identity.identity);
                if (mySeat != -1)
                {
                    /*
                     * RESET IKs
                     * */
                    ik.attachLeft = null;
                    ik.attachRight = null;
                    ik.attachLeft_Foot = null;
                    ik.attachRight_Foot = null;
                    /*
                     * */

                    IKWeapon ikw = currentVehicle.vehicle.seatPoints[mySeat].GetComponent<IKWeapon>();
                    freeOnSeat = (ikw != null && ikw.FreeOnSeat);

                    if ((ikw == null || !ikw.FreeOnSeat) && inventory.prefab != null && !string.IsNullOrEmpty(inventory.prefab.item.prefabName))
                    {
                        // You cannot use an item while on vehicle, Equip ("") means unequip the current item
                        inventory.Equip("");
                    }

                    if (ikw != null)
                    {
                        if (!ikw.FreeOnSeat)
                        {
                            /*
                             * ASSIGN SEAT IK, but if only we are bound on our seat.
                             * */
                            seatIK = ikw;

                            if (seatIK.IKLeft != null)
                                ik.attachLeft = seatIK.IKLeft;

                            if (seatIK.IKLeft_Foot != null)
                                ik.attachLeft_Foot = seatIK.IKLeft_Foot;

                            if (seatIK.IKRight != null)
                                ik.attachRight = seatIK.IKRight;

                            if (seatIK.IKRight_Foot != null)
                                ik.attachRight_Foot = seatIK.IKRight_Foot;
                        }
                        else
                        {
                            if (inventory.prefab == null || string.IsNullOrEmpty(inventory.prefab.item.prefabName))
                            {
                                inventory.Equip(GameMap.instance.startingItem);
                            }

                            inventory.prefab.GetComponent<IKWeapon>().Attach(false);
                        }
                    }
                }
            }
            else
            {
                /* DISMOUNTING FROM VEHICLE
                 * RESET VEHICLE SEAT IK
                 * */
                if (seatIK != null)
                {
                    if (seatIK.IKLeft != null)
                    ik.attachLeft = null;

                    if (seatIK.IKLeft_Foot != null)
                    ik.attachLeft_Foot = null;

                    if (seatIK.IKRight != null)
                    ik.attachRight = null;

                    if (seatIK.IKRight_Foot != null)
                    ik.attachRight_Foot = null;

                    seatIK = null;

                    /*
                     * If there is a weapon, re-attach.
                     * */
                    if (inventory.prefab != null)
                        inventory.prefab.GetComponent<IKWeapon>().Attach(false); // False, because it's already disparented.
                }
            }

            /*
             * VEHICLE GOINGIN & GOINGOUT
             * */
            bool isInVehicle = (value != null);
            identity.rbody.enabled = !isInVehicle; // Passengers don't sync rigidbody.
            identity.rbody.rBody.isKinematic = isInVehicle;

            bool vAnim = isInVehicle && !freeOnSeat;

            identity.animator.animator.SetBool("InVehicle", vAnim);
            identity.animator.animator.SetTrigger("ChangeItem");

            if (vAnim)
            {
                identity.animator.animator.Play("Vehicle");
            }

            identity.GetComponent<Collider>().isTrigger = isInVehicle;
            foreach (Rigidbody rb in identity.ragdoll.components)
            {
                rb.GetComponent<Collider>().isTrigger = isInVehicle;
            }

            if (isLocalPlayer && !isDead)
            {
                if (vAnim && !MouseOrbitImproved.instance.currentMode)
                {
                    MouseOrbitImproved.instance.CameraMode(true);
                }

                if (!vAnim && MouseOrbitImproved.instance.currentMode)
                {
                    MouseOrbitImproved.instance.CameraMode(false);
                }
            }
        }
    }

    /// <summary>
    /// Used for some methods like canJump etc.
    /// </summary>
    bool isGrounded = false;

    /// <summary>
    /// If we are on a high slope, we cannot move.
    /// </summary>
    bool canMove = false;

    /// <summary>
    /// Jump time offset
    /// </summary>
    float nextJump = 0;

    /// <summary>
    /// When the mouse button clicks. Is shooting will be active until its up. Also changing weapons will make this false.
    /// </summary>
    [HideInInspector]
    public bool isShooting = false;

    void Update()
    {
        isGrounded = HeightRaycaster.isGrounded(transform.position);

        /// We send a ray towards from our position but y += 1.25f, if it hits something walkable, we cannot move. 
        /// This is the slope fix.
        canMove = !Physics.Raycast(transform.position + Vector3.up*1.25f, transform.forward, 2f, HeightRaycaster.ground);

        if (isDead)
            return; // Dead players cannot use inputs. Free camera must be activated

        if (currentVehicle != null && !freeOnSeat)
        {
            //We are on a vehicle, also we are not free on the current seat.
            return;
        }

        if (!isLocalPlayer)
        {
            /* For other players, aiming must be smooth.
             * Smooth Aim Sync
             * */
            if (syncedAim != null)
                aim.y = Mathf.Lerp(aim.y, syncedAim.y, 0.1f);
            return;
        }

        if (InputBlocker.isBlocked())
        {
            /// Input blocked because of UI InputField
            horizontal = 0;
            vertical = 0;
            Sync(); // Sync anyway.
            return;
        }

        if (currentVehicle == null)
        { // Even we are on a free set, we cannot move
            horizontal = Mathf.Lerp(horizontal, Input.GetAxis("Horizontal"), Time.deltaTime * 12);
            vertical = Mathf.Lerp(vertical, Input.GetAxis("Vertical"), Time.deltaTime * 12);

            if (Input.GetButtonDown("Jump") && nextJump < Time.time && (isGrounded || identity.rbody.rBody.velocity.magnitude < 0.1f) && canMove)
            {/// Simple jumping
                nextJump = Time.time + 1;
                identity.rbody.rBody.AddForce(transform.up * 5 + transform.forward * vertical * 2 * (identity.animator.animator.GetBool("Sprint") ? 2 : 1) + transform.right * horizontal * 1, ForceMode.VelocityChange);
                identity.rbody.nextUpdate = Time.time + 0.02f;
            }

            /*
             * Local move input. It will be synced, but not in every frame so we are doing this for local client.
             * */
            identity.animator.animator.SetFloat("H", horizontal);
            identity.animator.animator.SetFloat("V", vertical);
            bool isMoving = (Mathf.Abs(horizontal) > 0.1f || Mathf.Abs(vertical) > 0.1f);
            identity.animator.animator.SetBool("M", isMoving);
            /*
             * */

            // Spriting via Shift key.
            if (Input.GetKeyDown(KeyCode.LeftShift) && vertical > 0.1f)
            {
                identity.animator.SetBool("Sprint", true);
            }
            else if (Input.GetKeyUp(KeyCode.LeftShift) || vertical <= 0.1f)
            { // If we done with shift key, disable the sprint.
                if (identity.animator.animator.GetBool("Sprint"))
                {
                    identity.animator.SetBool("Sprint", false);
                    identity.animator.SetTrigger("ChangeItem"); // We must call this to leave the sprinting state on animator.
                }
            }
        }

        // Rotating our player will rotate the camera because camera is child of our transform.
        transform.Rotate(new Vector3(0, Input.GetAxis("Mouse X"), 0) * Time.deltaTime * sensivityX);
        // Set player aim by Mouse Y
        aim.y = Mathf.Clamp(aim.y - Input.GetAxis("Mouse Y") * sensivityY, -50, 45);

        if (MouseOrbitImproved.instance.target == null)
        {
            /*
             * Set the camera position by aim.
             * */
            Camera.main.transform.localEulerAngles = new Vector3(aim.y, Camera.main.transform.localEulerAngles.y, Camera.main.transform.localEulerAngles.z);
            Vector3 cameraPos = Camera.main.transform.position;
            cameraPos.y = Camera.main.transform.parent.position.y + aim.y / 80;
            Camera.main.transform.position = cameraPos;
        }

        /*
         * Shooting request.
         * */

        if (Input.GetButtonDown("Fire1"))
            isShooting = true;
        else if (Input.GetButtonUp ("Fire1")) isShooting = false;

        if (isShooting && !identity.animator.animator.GetBool("Sprint") && !SNet_Manager.instance.panel_Lobby.activeSelf)
        {
            if (inventory.inv.ci == null)
                return;

            if (inventory.inv.ci.fireable && (inventory.inv.ci.ammo > 0 || !inventory.inv.ci.countable) && nextFire < Time.time)
            {
                nextFire = Time.time + inventory.inv.ci.fireRate;
                SNet_Network.instance.Send_Message(new PlayerInventory.St());
            }
        }

        Sync(); // Always sync.
    }

    /// <summary>
    /// Right mouse aim
    /// </summary>
    
    [HideInInspector]
    public bool aimed = false;
    private void LateUpdate()
    {
        if (aimBone != null) // Set the aim bone.
            aimBone.transform.localEulerAngles = new Vector3(
                (ik.aimAxis == 0) ? aim.y * ik.aimMod : aimBone.transform.localEulerAngles.x,
                (ik.aimAxis == 1) ? aim.y * ik.aimMod : aimBone.transform.localEulerAngles.y,
                (ik.aimAxis == 2) ? aim.y * ik.aimMod : aimBone.transform.localEulerAngles.z);

        if (shootCurrentItem != 0 && shootCurrentItem <= Time.time)
        {
            shootCurrentItem = 0;
            Shoot_CurrentItem();
        }

        if (isLocalPlayer)
        {
            if (inventory.inv.ci.aimable && Input.GetMouseButtonDown(1) && !identity.animator.animator.GetBool ("Sprint") && inventory.inv.ci.crossHairEnabled)
            {
                identity.animator.SetFloat("Speed", 0.5f);
                aimed = true;
            }
            else if (!inventory.inv.ci.aimable || identity.animator.animator.GetBool("Sprint") || Input.GetMouseButtonUp(1))
            {
                if (aimed)
                {
                    identity.animator.SetFloat("Speed", 1f);
                    aimed = false;
                }
            }

            Vector3 tPos = (aimed) ? focusHolder.localPosition : defaultCamPosition;
            if (Vector3.Distance(tPos, cameraHolder.localPosition) > 0.1f)
            {
                cameraHolder.localPosition = Vector3.Lerp(cameraHolder.localPosition, tPos, 0.1f);
            }
        }
    }

    /// <summary>
    /// Sync the controller input.
    /// </summary>
    void Sync()
    {
        /*
        * SYNC
        * */
        if (nextSync_Move < Time.time)
        {
            nextSync_Move = Time.time + 0.2f; // 5 times for one second.

            SNet_Network.instance.Send_Message(aim);

            if (currentVehicle != null && !freeOnSeat)
                return;

            identity.animator.SetFloat("H", horizontal);
            identity.animator.SetFloat("V", vertical);
            bool isMoving = (Mathf.Abs(horizontal) > 0.1f || Mathf.Abs(vertical) > 0.1f);
            identity.animator.SetBool("M", isMoving);
        }
        /*
         * */
    }

    /// <summary>
    /// Used for shoot delay. For example we requested to throw a grenade, but It must be throwed but synced with animation. So we can make a delay on Item script of items.
    /// </summary>
    [HideInInspector]
    public float shootCurrentItem = 0;

    /// <summary>
    /// Fire recovery time. Like automatic, single shot etc.
    /// </summary>
    float nextFire; // Fire rate;

    public static LayerMask hitMask = 1 << 8 | 1 << 2;
    public void Shoot()
    {
        if (inventory.inv.ci == null || !inventory.inv.ci.fireable || (inventory.inv.ci.ammo == 0 && inventory.inv.ci.countable))
            return;

        if (inventory.prefab.fireSound != null) // 
        AudioSource.PlayClipAtPoint(inventory.prefab.fireSound, transform.position);

        if (inventory.prefab.fireParticle != null)
        inventory.prefab.fireParticle.Play(); // Play fire particle

        identity.animator.animator.SetTrigger("Shoot");

        shootCurrentItem = Time.time + inventory.inv.ci.shootDelay;
    }

    void Shoot_CurrentItem ()
    {
        if (inventory.prefab == null)
            return;

        switch (inventory.inv.ci.itemType)
        {
            case Item.PlayerItem.ItemType.Bullet:
            case Item.PlayerItem.ItemType.Knife:
                {
                    RaycastHit hit;
                    if (Physics.Raycast(inventory.prefab.firePoint.position, MouseOrbitImproved.instance.transform.forward, out hit, inventory.inv.ci.range, ~hitMask))
                    {
                        if (isLocalPlayer)
                        {
                            if (inventory.inv.ci.itemType == Item.PlayerItem.ItemType.Bullet)
                                SNet_Network.instance.Send_Spawn("bulletImpact", hit.point, Quaternion.LookRotation(-inventory.prefab.firePoint.forward), Vector3.zero, 0);

                            if (hit.collider.CompareTag("Explosive"))
                            {
                                hit.collider.GetComponent<Explosive>().Explode_Request();
                                return;
                            }
                            /*
                            * ONLY LOCAL PLAYER CAN MAKE HIT.
                            * Bone damage multipliers are the parts of the user. 
                            * If you are making a third person shooter game you can use the ragdoll bones for this. 
                            * Add bone damage multipliers to the bones have colliders.
                            * 
                            * For example if you are making a car fight game (non-humanoid) drop the bone damage multiplier to the car has collider.
                            * */
                            SNet_Identity id = hit.collider.transform.root.GetComponent<SNet_Identity>();

                            if (id != null)
                            {
                                BoneDamageMultiplier bone = hit.collider.GetComponent<BoneDamageMultiplier>();
                                if (bone != null)
                                { // Hit an agent
                                    UserHit userHit = new UserHit(id.identity);
                                    userHit.v = Mathf.RoundToInt(inventory.inv.ci.damage * bone.damageModifier);
                                    userHit.h = transform.position;
                                    SNet_Network.instance.Send_Message(userHit);
                                    /*
                                     * IMPACT TO BONE
                                     * */
                                    SNet_Network.instance.Send_Message(new RagdollHelper.Impact(identity.identity, bone.name, inventory.prefab.firePoint.forward, inventory.inv.ci.damage / 100f));
                                }
                                else
                                {
                                    if (id.vehicle != null)
                                    {
                                        /*
                                         * VEHICLE HEALTH IS HOST CONTROLLED
                                         * */
                                        if (SNet_Network.instance.isHost())
                                        {
                                            id.vehicle.health = new SNet_Vehicle.VH(id.identity, Mathf.RoundToInt (id.vehicle.health.v - inventory.inv.ci.damage/id.vehicle.armor));
                                            SNet_Network.instance.Send_Message(id.vehicle.health);
                                        }
                                        /*
                                         * */
                                    }

                                    if (id.rbody != null)
                                    {
                                        id.rbody.rBody.AddForce(inventory.prefab.firePoint.forward * inventory.inv.ci.damage, ForceMode.Force);
                                        id.rbody.nextUpdate = 0; // Update now
                                    }
                                }
                            }
                        }
                    }
                }
                break;
            case Item.PlayerItem.ItemType.Throwable:
                if (isLocalPlayer)
                {
                    Quaternion high = new Quaternion();
                    high.eulerAngles = inventory.inv.ci.throwableRotation;
                    SNet_Network.instance.SpawnRequest (inventory.inv.ci.ThrowPrefab, inventory.prefab.firePoint.position, MouseOrbitImproved.instance.transform.rotation * high, Vector3.zero, inventory.inv.ci.ThrowForce);
                }
                break;
        }

        if (inventory.inv.ci.countable)
        {
            inventory.inv.ci.ammo--;

            if (isLocalPlayer)
            { // Update ammo
                inventory.UpdateCurrentAmmo();
                InventorySelector.current.UI_UpdateCurrentAmmo();
            }
        }
    }

    /*
     * Animator moves the character with root motion but I don't like the movement style that free animations, its not so smooth, so a little movement method here.
     * */
    float moveSpeed = 3;
    void OnAnimatorMove()
    {
        if (currentVehicle != null)
            return;

        if (identity.animator.animator.GetBool("M") && (isGrounded || identity.rbody.rBody.velocity.magnitude < 0.1f) && canMove)
        {
            bool sprinting = identity.animator.animator.GetBool("Sprint");
            Vector3 targetDirection = new Vector3((sprinting) ? 0 : identity.animator.animator.GetFloat("H"), 0, identity.animator.animator.GetFloat("V"));
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection, Vector3.up);
            targetRotation = transform.rotation * targetRotation;

            transform.position += targetRotation * Vector3.forward * moveSpeed * Time.deltaTime * ( (sprinting) ? 2f : 1f ) * identity.animator.animator.GetFloat ("Speed");
        }
    }

    /// <summary>
    /// Called by SNet_Vehicle. Passengers are not a child of vehicles transforms. So it must follow the vehicle.
    /// </summary>
    public void OnVehicleSync()
    {
        if (currentVehicle == null)
            return;

        int seatNo = currentVehicle.vehicle.vehicle.p.FindIndex(x => x == identity.identity);
        if (seatNo == -1)
        {
            return;
        }

        lastExit_Position = currentVehicle.vehicle.exitPoints[seatNo].position;
        lastExit_Rotation = currentVehicle.vehicle.exitPoints[seatNo].eulerAngles.y;

        Transform mySeat = currentVehicle.vehicle.seatPoints[seatNo];
        transform.position = mySeat.position;

        if (!freeOnSeat)
        {
            aim.y = 0; // Default aim while on the car.
            transform.rotation = mySeat.rotation;
        }
    }
}
