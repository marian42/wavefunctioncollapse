using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

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

	private static readonly int[] horizontalFaces = { 0, 2, 3, 5 };

	public static readonly string[] Names = { "-red", "-green", "-blue", "red", "green", "blue" };

	public static int Rotate(int direction, int rotations) {
		if (direction == 1 || direction == 4) {
			return direction;
		}
		return horizontalFaces[(Array.IndexOf(horizontalFaces, direction) + rotations) % 4];
	}

	public static bool IsHorizontal(int orientation) {
		return orientation != 1 && orientation != 4;
	}
}
