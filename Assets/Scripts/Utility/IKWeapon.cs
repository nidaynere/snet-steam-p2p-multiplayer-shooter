/*! 
@author EasyMOBA <easymoba.com>
@lastupdate 13 February 2018
*/

using UnityEngine;

/// <summary>
/// At first, I created this script for just hands to use weapons but now its used also for vehicles with also foots. So don't care for IK + 'Weapon' :)
/// </summary>
public class IKWeapon : MonoBehaviour
{
    public Transform IKLeft;
    public Transform IKRight;
    public Transform IKLeft_Foot;
    public Transform IKRight_Foot;

    /// <summary>
    /// Used for vehicles, the passenger on this seat will be free to aim & shot
    /// It will be set to False if one of IK transforms has assigned.
    /// </summary>
    [Tooltip ("Used for vehicles, the passenger on this seat will be free to aim & shot. It will be set to False if one of IK transforms has assigned.")]
    public bool FreeOnSeat = false;

    IKFixer lastIK;
    bool handsDisparented;

    public bool attachOnStart;
    private void Start()
    {
        if (IKLeft != null || IKRight != null || IKLeft_Foot != null || IKRight_Foot != null)
            FreeOnSeat = false;

        if (attachOnStart)
            Attach();
    }

    [Tooltip ("Click this to save the current item position.")]
    public bool SaveIKSetting = false;
    private void Update()
    {
        if (SaveIKSetting)
        {
            SaveIKSetting = false;

            if (IKLeft != null)
            {
                IKLeft.SetParent(transform);
            }

            if (IKRight != null)
            {
                IKRight.SetParent(transform);
            }

            IKData.SaveItemIK(transform.root.GetComponent<IKFixer>().PlayerClass, gameObject.name,
            transform.localPosition,
            transform.localEulerAngles,
            transform.localScale,
            (IKLeft == null) ? Vector3.zero : IKLeft.localPosition,
            (IKLeft == null) ? Vector3.zero : IKLeft.localEulerAngles,
            (IKRight == null) ? Vector3.zero : IKRight.localPosition,
            (IKRight == null) ? Vector3.zero : IKRight.localEulerAngles
            );

            if (IKLeft != null)
                IKLeft.SetParent(transform.parent);

            if (IKRight != null)
                IKRight.SetParent(transform.parent);
        }
    }

    public void Attach (bool disparentHands = true, IKFixer optional = null)
    {
        if (optional == null)
            optional = transform.root.GetComponent<IKFixer>();

        if (optional == null)
            return;

        lastIK = optional;

        /*
         * DISPARENT HANDS, USED FOR WEAPONS
         * */
        if (disparentHands)
        {
            IKData.IK getIK = IKData.GetItemIK(lastIK.PlayerClass, gameObject.name);
            transform.localPosition = getIK.objLocalPosition;
            transform.localEulerAngles = getIK.objLocalEulerAngles;
            transform.localScale = getIK.objLocalScale;

            if (IKLeft != null)
            {
                IKLeft.localPosition = getIK.leftHandPosition;
                IKLeft.localEulerAngles = getIK.leftHandEulerAngles;
                IKLeft.SetParent(transform.parent);
            }

            if (IKRight != null)
            {
                IKRight.localPosition = getIK.rightHandPosition;
                IKRight.localEulerAngles = getIK.rightHandEulerAngles;
                IKRight.SetParent(transform.parent);
            }

            handsDisparented = true;
        }
        /*
         * */

        optional.attachLeft = IKLeft;
        optional.attachRight = IKRight;
    }

    private void Detach ()
    {
        if (lastIK == null)
            return;

        lastIK.attachLeft = null;
        lastIK.attachRight = null;
        lastIK.attachLeft_Foot = null;
        lastIK.attachRight_Foot = null;
    }

    private void OnDestroy()
    {
        if (handsDisparented)
        {
            if (IKLeft != null)
                Destroy(IKLeft.gameObject);
            if (IKRight != null)
                Destroy(IKRight.gameObject);
        }
    }
}
