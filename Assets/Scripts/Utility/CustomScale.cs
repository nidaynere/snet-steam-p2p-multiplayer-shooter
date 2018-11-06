using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomScale : MonoBehaviour
{
    public float customScale = 1;
	// Use this for initialization
	void Awake ()
    {
        transform.localScale *= customScale;
	}
}
