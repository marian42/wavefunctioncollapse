using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Linq;
using System;

public class ModulePrototype : AbstractModulePrototype {
	[System.Serializable]
	public abstract class FaceDetails {
		public bool Walkable;

		public int Connector;

		[HideInInspector]
		public Fingerprint Fingerprint;

		public virtual void ResetConnector() {
			this.Connector = 0;
		}

		public ModulePrototype[] ExcludedNeighbours;

		public bool EnforceWalkableNeighbor = false;
	}

	[System.Serializable]
	public class HorizontalFaceDetails : FaceDetails {
		public bool Symmetric;
		public bool Flipped;

		public override string ToString() {
			return this.Connector.ToString() + (this.Symmetric ? "s" : (this.Flipped ? "F" : ""));
		}

		public override void ResetConnector() {
			base.ResetConnector();
			this.Symmetric = false;
			this.Flipped = false;
		}
	}

	[System.Serializable]
	public class VerticalFaceDetails : FaceDetails {
		public bool Invariant;
		public int Rotation;

		public override string ToString() {
			return this.Connector.ToString() + (this.Invariant ? "i" : (this.Rotation != 0 ? "_bcd".ElementAt(this.Rotation).ToString() : ""));
		}

		public override void ResetConnector() {
			base.ResetConnector();
			this.Invariant = false;
			this.Rotation = 0;
		}
	}

	public HorizontalFaceDetails Left;
	public VerticalFaceDetails Down;
	public HorizontalFaceDetails Back;
	public HorizontalFaceDetails Right;
	public VerticalFaceDetails Up;
	public HorizontalFaceDetails Forward;

	public bool CreateRotatedVariants = true;
	public bool Spawn = true;

	public FaceDetails[] Faces {
		get {
			return new FaceDetails[] {
				this.Left,
				this.Down,
				this.Back,
				this.Right,
				this.Up,
				this.Forward
			};
		}
	}

	public Mesh GetMesh() {
		var meshFilter = this.GetComponent<MeshFilter>();
		if (meshFilter != null && meshFilter.sharedMesh != null) {
			return meshFilter.sharedMesh;
		}
		var mesh = new Mesh();
		return mesh;
	}

	private static GUIStyle style;

	
#if UNITY_EDITOR
	[DrawGizmo(GizmoType.InSelectionHierarchy | GizmoType.NotInSelectionHierarchy)]
	static void DrawGizmoForMyScript(ModulePrototype modulePrototype, GizmoType gizmoType) {
		Vector3 position = modulePrototype.transform.position;

		if (ModulePrototype.style == null) {
			ModulePrototype.style = new GUIStyle();
			ModulePrototype.style.alignment = TextAnchor.MiddleCenter;
		}

		ModulePrototype.style.normal.textColor = Color.black;
		for (int i = 0; i < 6; i++) {
			var face = modulePrototype.Faces[i];
			Handles.Label(position + modulePrototype.transform.rotation * Orientations.Rotations[i] * Vector3.forward * MapGenerator.BlockSize / 2f, face.ToString(), ModulePrototype.style);
		}

		for (int i = 0; i < 6; i++) {
			if (modulePrototype.Faces[i].Walkable) {
				Gizmos.color = Color.red;
				Gizmos.DrawLine(modulePrototype.transform.position + Vector3.down * 0.1f, modulePrototype.transform.position + modulePrototype.transform.rotation * Orientations.Rotations[i] * Vector3.forward + Vector3.down * 0.1f);
			}
		}
	}
#endif

	public void EnsureFingerprints() {
		var faces = this.Faces;
		if (faces[0].Fingerprint != null) {
			return;
		}
		
		var mesh = this.GetMesh();

		for (int i = 0; i < 6; i++) {
			faces[i].Fingerprint = new Fingerprint(mesh, Orientations.Rotations[i], i == 4);
		}
	}

	public bool ConnectorsSet {
		get {
			return this.Faces.Any(face => face.Connector != 0);
		}
	}

	public void GuessConnectors() {
		var fingerprints = new Dictionary<int, Fingerprint>();

		foreach (var modulePrototype in this.transform.parent.GetComponentsInChildren<ModulePrototype>()) {
			if (modulePrototype == this || !modulePrototype.ConnectorsSet) {
				continue;
			}
			modulePrototype.EnsureFingerprints();
			for (int direction = 0; direction < 6; direction++) {
				var face = modulePrototype.Faces[direction];
				if (!fingerprints.ContainsKey(face.Connector)) {
					fingerprints[face.Connector] = face.Fingerprint;
				}
			}
		}

		this.EnsureFingerprints();

		for (int i = 0; i < 6; i++) {
			bool found = false;
			var face = this.Faces[i];

			if (face is HorizontalFaceDetails) {
				var hface = face as HorizontalFaceDetails;
				foreach (var connector in fingerprints.Keys) {
					if (fingerprints[connector].Symmetric != hface.Fingerprint.Symmetric) {
						continue;
					}
					if (Fingerprint.Compare(fingerprints[connector].Base, hface.Fingerprint.Base)) {
						found = true;
						hface.Connector = connector;
						hface.Symmetric = fingerprints[connector].Symmetric;
						hface.Flipped = false;
						break;
					}
					if (!fingerprints[connector].Symmetric && Fingerprint.Compare(fingerprints[connector].Base, hface.Fingerprint.Flipped)) {
						found = true;
						hface.Connector = connector;
						hface.Symmetric = false;
						hface.Flipped = true;
						break;
					}
				}
				if (!found) {
					hface.Connector = getNewConnector(fingerprints);
					hface.Flipped = false;
					hface.Symmetric = hface.Fingerprint.Symmetric;
					fingerprints[hface.Connector] = face.Fingerprint;
				}
			}

			if (face is VerticalFaceDetails) {
				var vface = face as VerticalFaceDetails;
				foreach (var connector in fingerprints.Keys) {
					if (fingerprints[connector].Invariant != vface.Fingerprint.Invariant) {
						continue;
					}
					for (int r = 0; r < (vface.Fingerprint.Invariant ? 1 : 4); r++) {
						if (Fingerprint.Compare(fingerprints[connector].Rotated[r], vface.Fingerprint.Base)) {
							found = true;
							vface.Connector = connector;
							vface.Invariant = vface.Fingerprint.Invariant;
							vface.Rotation = r;
							break;
						}
					}
				}
				if (!found) {
					vface.Connector = getNewConnector(fingerprints);
					vface.Rotation = 0;
					vface.Invariant = vface.Fingerprint.Invariant;
					fingerprints[vface.Connector] = vface.Fingerprint;
				}
			}			
		}

		this.CreateRotatedVariants = !(this.Up.Invariant
			&& this.Down.Invariant
			&& (this.Forward.Connector == this.Back.Connector && this.Left.Connector == this.Right.Connector && this.Forward.Connector == this.Left.Connector)
			&& (this.Forward.Symmetric || (this.Forward.Flipped == this.Back.Flipped && this.Left.Flipped == this.Right.Flipped && this.Forward.Flipped == this.Right.Flipped)));
	}

	private int getNewConnector(Dictionary<int, Fingerprint> dict) {
		int result = 0;
		while (dict.ContainsKey(result)) result++;
		return result;
	}

	public static List<Module> CreateModules(MapGenerator mapGenerator) {
		int count = 0;
		var modules = new List<Module>();
		
		foreach (var prototype in ModulePrototype.GetAll()) {
			for (int face = 0; face < 6; face++) {
				if (prototype.Faces[face].ExcludedNeighbours == null) {
					prototype.Faces[face].ExcludedNeighbours = new ModulePrototype[0];
				}
			}

			for (int rotation = 0; rotation < (prototype.CreateRotatedVariants ? 4 : 1); rotation++) {
				modules.Add(new Module(prototype, rotation, count, mapGenerator));
				count++;
			}
		}

		foreach (var variation in Variation.GetAll()) {
			foreach (var module in modules.Where(module => module.Prototype == variation.Prototype)) {
				module.Models.Add(variation);
			}
		}

		foreach (var module in modules) {
			module.PossibleNeighbors = new Module[6][];
			for (int direction = 0; direction < 6; direction++) {
				var face = module.Prototype.Faces[Orientations.Rotate(direction, module.Rotation)];
				module.PossibleNeighbors[direction] = modules
					.Where(neighbor => module.Fits(direction, neighbor)
						&& (!mapGenerator.RespectNeighorExclusions || (
							!face.ExcludedNeighbours.Contains(neighbor.Prototype)
							&& !neighbor.Prototype.Faces[Orientations.Rotate((direction + 3) % 6, neighbor.Rotation)].ExcludedNeighbours.Contains(module.Prototype))
							&& (!face.EnforceWalkableNeighbor || neighbor.Prototype.Faces[Orientations.Rotate((direction + 3) % 6, neighbor.Rotation)].Walkable)
							&& (face.Walkable || !neighbor.Prototype.Faces[Orientations.Rotate((direction + 3) % 6, neighbor.Rotation)].EnforceWalkableNeighbor))
					)
					.ToArray();
			}
			module.Probability = module.Models.Sum(model => model.Probability);
		}

		return modules;
	}

	public static IEnumerable<ModulePrototype> GetAll() {
		foreach (Transform transform in GameObject.FindObjectOfType<ModulePrototype>().transform.parent) {
			var item = transform.GetComponent<ModulePrototype>();
			if (item != null && item.enabled) {
				yield return item;
			}
		}
	}

	void Update() { }
}
