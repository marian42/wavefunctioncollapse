using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

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

	public static T PickRandom<T>(this ICollection<T> collection) {
		int index = UnityEngine.Random.Range(0, collection.Count);
		return collection.ElementAt(index);
	}

	public static void DeleteChildren(this Transform transform) {
		int c = 0;
		while (transform.childCount != 0) {
			if (Application.isPlaying) {
				GameObject.DestroyImmediate(transform.GetChild(0).gameObject);
			} else {
				GameObject.DestroyImmediate(transform.GetChild(0).gameObject);
			}
			if (c++ > 10000) {
				throw new System.Exception();
			}
		}
	}

	public static T GetBest<T>(this IEnumerable<T> enumerable, Func<T, float> property) {
		float bestValue = float.NegativeInfinity;
		T bestItem = default(T);
		foreach (var item in enumerable) {
			float value = property.Invoke(item);
			if (value > bestValue) {
				bestValue = value;
				bestItem = item;
			}
		}

		return bestItem;
	}
	
}
