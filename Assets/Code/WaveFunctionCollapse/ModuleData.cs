using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Wave Function Collapse/Module Data", fileName = "modules.asset")]
public class ModuleData : ScriptableObject {
	public static Module[] Current;

	public Module[] Modules;
}
