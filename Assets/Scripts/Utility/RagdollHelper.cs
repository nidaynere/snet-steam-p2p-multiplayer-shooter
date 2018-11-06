/*! 
@author nbzeman <easymoba.com>
@lastupdate 13 February 2018
*/

using UnityEngine;

/*
 * MODIFIED VERSION OF https://github.com/nbzeman/Ragdoll/blob/master/Assets/Scripts/RagdollHelper.cs
A helper component that enables animator or ragdoll.
*/

public class RagdollHelper : MonoBehaviour
{
    public class Impact : SNet_Network.SNetMessage
    {
        public Impact (ulong _identity, string _bone, Vector3 _direction, float _forceTime)
        {
            i = _identity;
            b = _bone;
            d = _direction;
            f = _forceTime;
        }

        public string b;
        public Vector3 d;
        public float f;
    }

	//public property that can be set to toggle between ragdolled and animated character
	public bool ragdolled
	{
		get
		{
			return state != RagdollState.animated;
		}
		set
		{
			if (value)
			{
				if (state == RagdollState.animated)
				{
                    anim.StopPlayback();
					setKinematic(false);
					GetComponent<Rigidbody>().isKinematic = true;
					anim.enabled = false; //disable animation
					state = RagdollState.ragdolled;
				}
			}
			else
			{
				if (state == RagdollState.ragdolled)
				{
					setKinematic(true); //disable gravity etc.
					GetComponent<Rigidbody>().isKinematic = false;
					anim.enabled = true; //enable animation
					state = RagdollState.animated;
				}
			}
		}
	}

	//Possible states of the ragdoll
	enum RagdollState
	{
		animated,    //Mecanim is fully in control
		ragdolled,   //Mecanim turned off, physics controls the ragdoll
	}

	//The current state
	RagdollState state = RagdollState.animated;

	//Declare an Animator member variable, initialized in Start to point to this gameobject's Animator component.
	Animator anim;

	//A helper function to set the isKinematc property of all RigidBodies in the children of the 
	//game object that this script is attached to

	public Rigidbody[] components;

	void setKinematic(bool newValue)
	{
        Rigidbody rb = GetComponent<Rigidbody>();

        //For each of the components in the array, treat the component as a Rigidbody and set its isKinematic property
        foreach (Rigidbody c in components)
        {
            if (c != rb)
            {
                c.isKinematic = newValue;
            }
			else
			{
				c.GetComponent<Collider>().enabled = newValue;
				c.isKinematic = !newValue;
			}
		}
    }

	// Initialization, first frame of game
	void Awake()
	{
		//Set all RigidBodies to kinematic so that they can be controlled with Mecanim
		//and there will be no glitches when transitioning to a ragdoll
		components = GetComponentsInChildren<Rigidbody>();

		setKinematic(true);

		//Store the Animator component
		anim = GetComponent<Animator>();
	}

	Rigidbody impactTarget = null;
	Vector3 impactDirection = Vector3.zero;
	float impactEnd = -1;

	public Rigidbody FindBone(string boneName)
	{
		foreach (Rigidbody r in components)
			if (r.name == boneName)
			{
				return r;
			}

		return null;
	}

	public void Impacted (Impact impactData)
	{
		if (impactData == null)
		{
			impactTarget = null;
			impactEnd = -1;
			impactDirection = Vector3.zero;
			return;
		}

		impactTarget = FindBone(impactData.b);
        impactDirection = impactData.d.normalized + Vector3.up;
        forceTime = impactData.f;
        impactEnd = Time.time + forceTime;
	}

    float forceTime;
	void Update()
	{
		//Check if we need to apply an impact
		if (Time.time < impactEnd && impactTarget)
		{
			impactTarget.AddForce(impactDirection, ForceMode.VelocityChange);
            impactDirection = Vector3.Lerp(impactDirection, Vector3.zero, Time.deltaTime / forceTime);
		}
	}
}
