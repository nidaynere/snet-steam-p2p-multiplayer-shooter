/*! 
@author EasyMOBA <easymoba.com>
@lastupdate 13 February 2018
*/

using UnityEngine;
using UnityEditor;

/// <summary>
/// SNet_Editor script makes an auto player.
/// </summary>
public class SNet_Editor_AddPlayer : EditorWindow
{
    // Add menu named "My Window" to the Window menu
    [MenuItem("SNet Tools/Generate Player")]
    static void Init()
    {
        // Get existing open window or if none, make a new one:
        SNet_Editor_AddPlayer window = (SNet_Editor_AddPlayer) GetWindow(typeof(SNet_Editor_AddPlayer));
        window.minSize = window.maxSize = new Vector2(800, 500);

        window.Show();
    }

    /// <summary>
    /// New game object will be converted to playable character
    /// </summary>
    public static GameObject source;

    void OnGUI()
    {
        GUILayout.Label("SNet Player Adder", EditorStyles.boldLabel);
        GUILayout.Label("This tool helps you to create a new character with a new 3d humanoid model.", EditorStyles.label);
        GUILayout.Label("You must ragdoll the player first. Please follow the tutorials at the web site www.easymoba.com", EditorStyles.label);

        GUILayout.Space(20);
        source = (GameObject) EditorGUILayout.ObjectField (source, typeof(GameObject), true);
        GUILayout.Space(20);
        if (source != null)
        {
            Animator anim = source.GetComponent<Animator>();
            if (anim == null)
            {
                GUILayout.Label("The target gameobject has not Animator component. Be sure it has a humanoid skeleton.", EditorStyles.boldLabel);
                return;
            }
            
            Rigidbody rb = anim.GetBoneTransform(HumanBodyBones.Hips).GetComponent<Rigidbody>();
            if (rb == null)
            {
                GUILayout.Label("The hips of target skeleton has not rigidbody component. The target gameobject may have not been ragdolled properly. You must initialize the ragdoll first.", EditorStyles.boldLabel);
                return;
            }

            if (source.GetComponent<SNet_Identity>())
            {
                GUILayout.Label("The target gameobject has an SNet_Identity component. Please create the gameobject from the main model file, not from the prefab.", EditorStyles.boldLabel);
                return;
            }

            if (GUILayout.Button("Generate"))
            {
                /* INITIALIZE */

                GameObject go = new GameObject("CameraHolder");
                Transform cameraHolder = Instantiate(go.transform, source.transform);
                cameraHolder.name = "CameraHolder";
                cameraHolder.localPosition = new Vector3(0.22f, 2.11f, -1.5f);
                cameraHolder.localEulerAngles = new Vector3(15, 0, 0);

                Transform focusHolder = Instantiate(go.transform, source.transform);
                focusHolder.name = "FocusHolder";
                focusHolder.localPosition = new Vector3(0.22f, 1.961f, -0.909f);
                focusHolder.localEulerAngles = new Vector3(15, 0, 0);

                DestroyImmediate(go);

                anim.runtimeAnimatorController = (RuntimeAnimatorController) AssetDatabase.LoadAssetAtPath("Assets/PlayerAnimator.controller", typeof (RuntimeAnimatorController));
                anim.applyRootMotion = true;
                source.AddComponent<SNet_Animator>();
                source.AddComponent<SNet_Rigidbody>();
                source.AddComponent<SNet_Transform>();
                source.AddComponent<IKFixer>();
                source.AddComponent<UserFallDamage>();
                source.AddComponent<RagdollHelper>();
                source.AddComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;
                source.AddComponent<CapsuleCollider>();
                source.AddComponent<AnimEventReceiver>();
                source.layer = 8;

                /*ADD BONE DAMAGE MULTIPLIER*/
                Collider[] cldrs = source.transform.GetComponentsInChildren<Collider>(true);
                foreach (Collider c in cldrs)
                {
                    if (c.gameObject == source)
                        continue;

                    BoneDamageMultiplier bone = c.gameObject.AddComponent<BoneDamageMultiplier>();

                    if (anim.GetBoneTransform(HumanBodyBones.Head) == c.transform || anim.GetBoneTransform(HumanBodyBones.Neck) == c.transform)
                    {
                        bone.damageModifier = 2f; // For head shots
                    }
                }
            }
        }
    }
}
