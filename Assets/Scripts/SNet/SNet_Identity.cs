/*! 
@author EasyMOBA <easymoba.com>
@lastupdate 13 February 2018
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This is one of the important classes of SNet.
/// It is the part of the networked objects.
/// Usually you may not need to edit this.
/// </summary>
public class SNet_Identity : MonoBehaviour 
{
    public static List<SNet_Identity> list = new List<SNet_Identity>();

    public static bool find(ulong identity, out SNet_Identity gameObject)
    {
        gameObject = null;

        SNet_Identity id = list.Find(x => x.set && x.identity == identity);
        if (id == null)
            return false;
        else
        {
            gameObject = id;
            return true;
        }
    }

    public ulong identity;

    [HideInInspector]
    public string prefab;

    [HideInInspector]
    public SNet_Animator animator;
    [HideInInspector]
    public SNet_Rigidbody rbody;
    [HideInInspector]
    public SNet_Transform tform;
    [HideInInspector]
    public SNet_Controller controller;
    [HideInInspector]
    public SNet_Vehicle vehicle;
    [HideInInspector]
    public RagdollHelper ragdoll;
    [HideInInspector]
    public Explosive explosive;
    [HideInInspector]
    public AudioSource asource;

    public bool set = false;
    public void Set(ulong _identity, string _prefab)
    {
        identity = _identity;
        prefab = _prefab;
        animator = GetComponent<SNet_Animator>();
        rbody = GetComponent<SNet_Rigidbody>();
        tform = GetComponent<SNet_Transform>();
        controller = GetComponent<SNet_Controller>();
        ragdoll = GetComponent<RagdollHelper>();
        explosive = GetComponent<Explosive>();
        vehicle = GetComponent<SNet_Vehicle>();
        asource = GetComponent<AudioSource>();

        if (_identity < 1000000 && identity > SNet_Network.SNetMessage.idStep)
            SNet_Network.SNetMessage.idStep = identity;

        /*
         * MAX REALTIME NETWORK OBJECTS LIMIT is 1.000.000
         * */

        set = true;
    }

    void OnEnable()
    {
        if (transform.parent == null)
        DontDestroyOnLoad(gameObject);

        if (!Application.isPlaying)
            return;

        if (!list.Find(x => x == this))
            list.Add(this);
    }

    void OnDestroy()
    {
        if (!Application.isPlaying)
            return;

        if (identity > 0 && SNet_Network.instance.isHost())
        {
            SNet_Network.instance.Send_Message(new SNet_Network.Destroyed(identity));
        }

        list.Remove(this);
    }
}
