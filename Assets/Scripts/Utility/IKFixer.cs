/*! 
@author EasyMOBA <easymoba.com>
@lastupdate 13 February 2018
*/

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Its not a fixer actually. It's part of the IKData
/// </summary>
public class IKFixer : MonoBehaviour
{
    [Tooltip ("Aim axis fixer, your characters spine rotation can be wrong, use this. 0 is X, 1 is Y, 2 is Z")]
    public ushort aimAxis = 0; // 0 is X, 1 is Y, 2 is Z
    [Tooltip ("Aim direction modifier, do it - for inverse")]
    public short aimMod = 1;

    [HideInInspector]
    public Transform attachLeft;
    [HideInInspector]
    public Transform attachRight;
    [HideInInspector]
    public Transform attachLeft_Foot;
    [HideInInspector]
    public Transform attachRight_Foot;

    /// <summary>
    /// The ik class of player on IKData.
    /// </summary>
    [Tooltip ("Default is the default value. this value will read from ikdata.json")]
    public string PlayerClass = "Default";

    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    void OnAnimatorIK(int layerIndex)
    {
        if (attachLeft != null)
        {
            animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1);
            animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 1);
            animator.SetIKPosition(AvatarIKGoal.LeftHand, attachLeft.position);
            animator.SetIKRotation(AvatarIKGoal.LeftHand, attachLeft.rotation);
        }

        if (attachRight != null)
        {
            animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 1);
            animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 1);
            animator.SetIKPosition(AvatarIKGoal.RightHand, attachRight.position);
            animator.SetIKRotation(AvatarIKGoal.RightHand, attachRight.rotation);
        }

        if (attachLeft_Foot != null)
        {
            animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 1);
            animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, 1);
            animator.SetIKPosition(AvatarIKGoal.LeftFoot, attachLeft_Foot.position);
            animator.SetIKRotation(AvatarIKGoal.LeftFoot, attachLeft_Foot.rotation);
        }
        if (attachRight_Foot != null)
        {
            animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 1);
            animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, 1);
            animator.SetIKPosition(AvatarIKGoal.RightFoot, attachRight_Foot.position);
            animator.SetIKRotation(AvatarIKGoal.RightFoot, attachRight_Foot.rotation);
        }
    }
}