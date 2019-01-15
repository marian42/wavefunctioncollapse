using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Extensions
{
	public static void Draw(this Bounds bounds, Color color) {
		var e = bounds.extents;
		Debug.DrawLine(bounds.center + new Vector3(+e.x, +e.y, +e.z), bounds.center + new Vector3(-e.x, +e.y, +e.z), color);
		Debug.DrawLine(bounds.center + new Vector3(+e.x, -e.y, +e.z), bounds.center + new Vector3(-e.x, -e.y, +e.z), color);
		Debug.DrawLine(bounds.center + new Vector3(+e.x, -e.y, -e.z), bounds.center + new Vector3(-e.x, -e.y, -e.z), color);
		Debug.DrawLine(bounds.center + new Vector3(+e.x, +e.y, -e.z), bounds.center + new Vector3(-e.x, +e.y, -e.z), color);

		Debug.DrawLine(bounds.center + new Vector3(+e.x, +e.y, +e.z), bounds.center + new Vector3(+e.x, -e.y, +e.z), color);
		Debug.DrawLine(bounds.center + new Vector3(-e.x, +e.y, +e.z), bounds.center + new Vector3(-e.x, -e.y, +e.z), color);
		Debug.DrawLine(bounds.center + new Vector3(-e.x, +e.y, -e.z), bounds.center + new Vector3(-e.x, -e.y, -e.z), color);
		Debug.DrawLine(bounds.center + new Vector3(+e.x, +e.y, -e.z), bounds.center + new Vector3(+e.x, -e.y, -e.z), color);

		Debug.DrawLine(bounds.center + new Vector3(+e.x, +e.y, +e.z), bounds.center + new Vector3(+e.x, +e.y, -e.z), color);
		Debug.DrawLine(bounds.center + new Vector3(+e.x, -e.y, +e.z), bounds.center + new Vector3(+e.x, -e.y, -e.z), color);
		Debug.DrawLine(bounds.center + new Vector3(-e.x, +e.y, +e.z), bounds.center + new Vector3(-e.x, +e.y, -e.z), color);
		Debug.DrawLine(bounds.center + new Vector3(-e.x, -e.y, +e.z), bounds.center + new Vector3(-e.x, -e.y, -e.z), color);
	}
	
	public static Vector3 ToVector3(this Vector3Int vector) {
		return (Vector3)(vector);
	}
}
