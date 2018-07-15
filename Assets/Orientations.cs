using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Orientations {
	private static Quaternion[] items;

	public static Quaternion[] All {
		get {
			if (Orientations.items == null) {
				Orientations.initialize();
			}
			return Orientations.items;
		}
	}

	private static void initialize() {
		Orientations.items = new Quaternion[] {
			Quaternion.LookRotation(Vector3.left),
			Quaternion.LookRotation(Vector3.down),
			Quaternion.LookRotation(Vector3.back),
			Quaternion.LookRotation(Vector3.right),
			Quaternion.LookRotation(Vector3.up),
			Quaternion.LookRotation(Vector3.forward)
		};
	}
}
