/*! 
@author EasyMOBA <easymoba.com>
@lastupdate 13 February 2018
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Similar to NetworkAnimator.
/// Sets the Animator parameters over the network.
/// </summary>
[RequireComponent(typeof(SNet_Identity))]
public class SNet_Animator : MonoBehaviour
{
    /// <summary>
    /// Base class of animator message
    /// </summary>
    public class SNet_Animator_Message : SNet_Network.SNetMessage
    {
        public string id;
    }

    /// <summary>
    /// SetBool over the network
    /// </summary>
    public class A_Bool : SNet_Animator_Message
    {
        public A_Bool(ulong _identity, string _id, bool _value)
        {
            value = _value;
            id = _id;
            i = _identity;
        }

        public bool value;
    }

    /// <summary>
    /// SetFloat over the network
    /// </summary>
    public class A_Float : SNet_Animator_Message
    {
        public A_Float(ulong _identity, string _id, float _value)
        {
            value = _value;
            id = _id;
            i = _identity;
        }

        public float value;
    }

    /// <summary>
    /// SetInt over the network
    /// </summary>
    public class A_Int : SNet_Animator_Message
    {
        public A_Int(ulong _identity, string _id, int _value)
        {
            value = _value;
            id = _id;
            i = _identity;
        }

        public int value;
    }

    /// <summary>
    /// SetTrigger over the network
    /// </summary>
    public class A_Trigger : SNet_Animator_Message
    {
        public A_Trigger(ulong _identity, string _id)
        {
            id = _id;
            i = _identity;
        }
    }

    [HideInInspector]
    public Animator animator;
    [HideInInspector]
    public SNet_Controller controller;

    SNet_Identity identity;
    private void Awake ()
    {
        animator = GetComponent<Animator>();
        identity = GetComponent<SNet_Identity>();
        controller = GetComponent<SNet_Controller>();
    }

    /// <summary>
    /// Change a boolean on animator over the network.
    /// </summary>
    /// <param name="name">Parameter name</param>
    /// <param name="value">New value</param>
    public void SetBool (string name, bool value)
    {
        if (!Check())
            return;

        SNet_Network.instance.Send_Message(new A_Bool(identity.identity, name, value));
    }

    /// <summary>
    /// Change an integer on animator over the network.
    /// </summary>
    /// <param name="name">Parameter name</param>
    /// <param name="value">New value</param>
    public void SetInt(string name, int value)
    {
        if (!Check())
            return;

        SNet_Network.instance.Send_Message(new A_Int(identity.identity, name, value)); 
    }

    /// <summary>
    /// Change a float on animator over the network.
    /// </summary>
    /// <param name="name">Parameter name</param>
    /// <param name="value">New value</param>
    public void SetFloat(string name, float value)
    {
        if (!Check())
            return;

        SNet_Network.instance.Send_Message(new A_Float(identity.identity, name, value));
    }

    /// <summary>
    /// Set a trigger on animator over the network.
    /// </summary>
    /// <param name="name">Parameter name</param>
    public void SetTrigger(string name)
    {
        if (!Check())
            return;

        SNet_Network.instance.Send_Message(new A_Trigger(identity.identity, name));
    }

    public bool Check()
    {
        if (controller == null && !SNet_Network.instance.isHost())
            return false; // Only the host can control non-player objects.

        return true;
    }
}
