using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Variation : AbstractModulePrototype {
	public ModulePrototype Prototype;

	public static IEnumerable<Variation> GetAll() {
		foreach (Transform transform in GameObject.FindObjectOfType<ModulePrototype>().transform.parent) {
			var item = transform.GetComponent<Variation>();
			if (item != null && item.enabled) {
				yield return item;
			}
		}
	}
}
