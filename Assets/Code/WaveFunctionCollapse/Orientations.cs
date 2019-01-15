using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public class Orientations {
	public const int LEFT = 0;
	public const int DOWN = 1;
	public const int BACK = 2;
	public const int RIGHT = 3;
	public const int UP = 4;
	public const int FORWARD = 5;

	public enum Enum {
		Left = 0,
		Down = 1,
		Back = 2,
		Right = 3,
		Up = 4,
		Forward = 5
	}

	private static Quaternion[] rotations;
	private static Vector3[] vectors;
	private static Vector3Int[] directions;

	public static Quaternion[] Rotations {
		get {
			if (Orientations.rotations == null) {
				Orientations.initialize();
			}
			return Orientations.rotations;
		}
	}

	public static Vector3Int[] Direction {
		get {
			if (Orientations.directions == null) {
				Orientations.initialize();
			}
			return Orientations.directions;
		}
	}

	private static void initialize() {
		Orientations.vectors = new Vector3[] {
			Vector3.left,
			Vector3.down,
			Vector3.back,
			Vector3.right,
			Vector3.up,
			Vector3.forward
		};

		Orientations.rotations = Orientations.vectors.Select(vector => Quaternion.LookRotation(vector)).ToArray();
		Orientations.directions = Orientations.vectors.Select(vector => Vector3Int.RoundToInt(vector)).ToArray();
	}

	public static readonly int[] HorizontalDirections = { 0, 2, 3, 5 };

	public static readonly string[] Names = { "-Red (Left)", "-Green (Down)", "-Blue (Back)", "+Red (Right)", "+Green (Up)", "+Blue (Forward)" };

	public static int Rotate(int direction, int amount) {
		if (direction == 1 || direction == 4) {
			return direction;
		}
		return HorizontalDirections[(Array.IndexOf(HorizontalDirections, direction) + amount) % 4];
	}

	public static bool IsHorizontal(int orientation) {
		return orientation != 1 && orientation != 4;
	}

	public static int GetIndex(Vector3 direction) {
		if (direction.x < 0) {
			return 0;
		} else if (direction.y < 0) {
			return 1;
		} else if (direction.z < 0) {
			return 2;
		} else if (direction.x > 0) {
			return 3;
		} else if (direction.y > 0) {
			return 4;
		} else {
			return 5;
		}
	}
}
