using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Always loot at camera
/// </summary>
public class LookAtCamera : MonoBehaviour
{
	// Update is called once per frame
	void LateUpdate ()
    {
        transform.rotation = Quaternion.LookRotation(transform.position - Camera.main.transform.position);
	}
}
