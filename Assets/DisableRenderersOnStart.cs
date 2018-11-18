using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DisableRenderersOnStart : MonoBehaviour
{

	void Start ()
	{
	    foreach (var r in gameObject.GetComponentsInChildren<Renderer>())
	    {
	        r.enabled = false;
	    }
	}
	
}
