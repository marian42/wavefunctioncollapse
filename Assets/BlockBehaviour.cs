using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockBehaviour : MonoBehaviour {

	public ModulePrototype Prototype;

	[System.NonSerialized]
	public BlockBehaviour[] Neighbours;
}
