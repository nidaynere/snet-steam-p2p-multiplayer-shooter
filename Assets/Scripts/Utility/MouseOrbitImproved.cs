/*! 
@author Veli V http://wiki.unity3d.com/index.php?title=MouseOrbitImproved
@lastupdate 13 February 2018
*/

/*MODIFIED VERSION OF
 * http://wiki.unity3d.com/index.php?title=MouseOrbitImproved
 * */

using UnityEngine;
using System.Collections;

/// <summary>
/// This is enabled when the player dies (ragdolled players with orbit camera), and you are on a vehicle.
/// </summary>
[AddComponentMenu("Camera-Control/Mouse Orbit with zoom")]
public class MouseOrbitImproved : MonoBehaviour
{
    public static MouseOrbitImproved instance;
    public Transform target;
    public float distance = 5.0f;
    public float xSpeed = 120.0f;
    public float ySpeed = 120.0f;

    public float yMinLimit = -20f;
    public float yMaxLimit = 80f;

    float x = 0.0f;
    float y = 0.0f;

    private void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    [HideInInspector]
    public bool currentMode = false;

    public void CameraMode(bool value)
    { // False = Playermode, True= Orbit
        try
        {
            if (!value)
            {
                instance.target = null;
                if (SNet_Controller.user.cameraHolder != null)
                {
                    Camera.main.transform.SetParent(SNet_Controller.user.cameraHolder);
                    Camera.main.transform.localPosition = Vector3.zero;
                    Camera.main.transform.localRotation = Quaternion.identity;
                }
            }
            else
            {
                instance.Start();
                instance.target = SNet_Controller.user.identity.animator.animator.GetBoneTransform(HumanBodyBones.Hips);
                instance.transform.SetParent(null);
            }
        }
        catch
        {

        }

        currentMode = value;
    }

    // Use this for initialization
    public void Start()
    {
        Vector3 angles = transform.eulerAngles;
        x = angles.y;
        y = angles.x; 
    }

    void LateUpdate()
    {
        if (target)
        {
            x += Input.GetAxis("Mouse X") * xSpeed * distance * 0.02f;
            y -= Input.GetAxis("Mouse Y") * ySpeed * 0.02f;

            y = ClampAngle(y, yMinLimit, yMaxLimit);

            Quaternion rotation = Quaternion.Euler(y, x, 0);

            Vector3 negDistance = new Vector3(0.0f, 0.0f, -distance);
            Vector3 position = rotation * negDistance + target.position;

            transform.rotation = rotation;
            transform.position = position;
        }
    }

    public static float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360F)
            angle += 360F;
        if (angle > 360F)
            angle -= 360F;
        return Mathf.Clamp(angle, min, max);
    }
}