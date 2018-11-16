using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Fingerprint {
	public readonly Vector4[] Base;
	public readonly Vector4[] Flipped;

	public readonly Vector4[][] Rotated;

	public readonly bool Symmetric;
	public readonly bool Invariant;

	public Fingerprint(Mesh mesh, Quaternion orientation, bool top = false) {
		this.Rotated = Enumerable.Range(0, 4).Select(i => Fingerprint.create(mesh, orientation, i, false, top)).ToArray();
		this.Base = this.Rotated[0];
		this.Flipped = Fingerprint.create(mesh, orientation, 0, flipX: true);

		this.Symmetric = Fingerprint.Compare(this.Base, this.Flipped);
		this.Invariant = Fingerprint.Compare(this.Rotated[0], this.Rotated[1]);
	}

	private static Vector4[] create(Mesh mesh, Quaternion rotation, int index, bool flipX = false, bool top = false) {
		var triangles = mesh.triangles;

		var rot = Quaternion.Inverse(rotation) * Quaternion.Euler(Vector3.up * 90f * index);
		var vertices = mesh.vertices.Select(v => rot * v).ToArray();
		var result = new List<Vector4>();

		for (int t = 0; t < triangles.Length - 2; t += 3) {
			for (int i = 0; i < 3; i++) {
				Vector3 v1 = vertices[triangles[t + i]];
				Vector3 v2 = vertices[triangles[t + (i + 1) % 3]];

				if (v1.z > InfiniteMap.BLOCK_SIZE / 2f * 0.99f && v2.z > InfiniteMap.BLOCK_SIZE / 2f * 0.99f) {
					if (flipX) {
						v1.x *= -1f;
						v2.x *= -1f;
					}
					if (top) {
						v1.y *= -1f;
						v2.y *= -1f;
					}
					if (v1.x + v1.y < v2.x + v2.y) {
						var v3 = v1;
						v1 = v2;
						v2 = v3;
					}
					result.Add(new Vector4(v1.x, v1.y, v2.x, v2.y));
				}
			}
		}
		return result.ToArray();
	}

	public static bool Compare(Vector4[] a, Vector4[] b) {
		const float threshold = 0.001f;
		const float p = 0.8f;
		int c1 = a.Count(v1 => b.Any(v2 => Vector4.Distance(v1, v2) < threshold));
		int c2 = b.Count(v1 => a.Any(v2 => Vector4.Distance(v1, v2) < threshold));

		return (a.Count() == 0 || c1 >= Mathf.FloorToInt((float)a.Count() * p) + 1)
			&& (b.Count() == 0 || c2 >= Mathf.FloorToInt((float)b.Count() * p) + 1);
	}
}
