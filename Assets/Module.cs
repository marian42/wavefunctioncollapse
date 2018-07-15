using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

[System.Serializable]
public class Module {
	public readonly Quaternion Orientation;

	public readonly Mesh mesh;

	public Fingerprint[] Fingerprints;

	public int[] Connectors;

	[System.Serializable]
	public class Fingerprint {
		public Vector4[] Items;

		public Fingerprint(Vector4[] items) {
			this.Items = items;
		}

		public Vector4 this[int i] {
			get {
				return this.Items[i];
			}
		}
	}

	public Module(Mesh mesh, Quaternion orientation, MapGenerator mapGenerator) {
		this.mesh = mesh;
		this.Orientation = orientation;

		this.Fingerprints = Enumerable.Range(0, 6).Select(i => this.getFingerprint(i)).ToArray();
		this.Connectors = this.Fingerprints.Select(fingerprint => mapGenerator.GetConnector(fingerprint)).ToArray();
	}

	public static Module CreateEmpty(MapGenerator mapGenerator) {
		var mesh = new Mesh();
		return new Module(mesh, Quaternion.identity, mapGenerator);
	}

	public Fingerprint getFingerprint(int index) {
		var triangles = this.mesh.triangles;

		var rot = Quaternion.Inverse(Orientations.All[index]) * this.Orientation;
		var vertices = this.mesh.vertices.Select(v => rot * v).ToArray();

		var result = new List<Vector4>();

		for (int t = 0; t < triangles.Length - 2; t += 3) {
			for (int i = 0; i < 3; i++) {
				Vector3 v1 = vertices[triangles[t + i]];
				Vector3 v2 = vertices[triangles[t + (i + 1) % 3]];

				if (v1.z > MapGenerator.BlockSize / 2f * 0.99f && v2.z > MapGenerator.BlockSize / 2f * 0.99f) {
					if (index == 4) {
						v1.y *= -1f;
						v2.y *= -1f;
					} else if (index > 2) {
						v1.x *= -1f;
						v2.x *= -1f;
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
		return new Fingerprint(result.ToArray());
	}

	public static Boolean CompareFingerprints(Fingerprint a, Fingerprint b) {
		const float threshold = 0.001f;
		const float p = 0.8f;
		int c1 = a.Items.Count(v1 => b.Items.Any(v2 => Vector4.Distance(v1, v2) < threshold));
		int c2 = b.Items.Count(v1 => a.Items.Any(v2 => Vector4.Distance(v1, v2) < threshold));

		return (a.Items.Count() == 0 || c1 >= Mathf.FloorToInt((float)a.Items.Count() * p) + 1)
			&& (b.Items.Count() == 0 || c2 >= Mathf.FloorToInt((float)b.Items.Count() * p) + 1);
	}
}
