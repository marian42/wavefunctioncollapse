using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Linq;
using System;

public class ModulePrototype : MonoBehaviour {
	[System.Serializable]
	public abstract class FaceDetails {
		public bool Walkable;

		public int Connector;

		public virtual void ResetConnector() {
			this.Connector = 0;
		}

		public ModulePrototype[] ExcludedNeighbours;

		public bool EnforceWalkableNeighbor = false;

		public bool IsOcclusionPortal = false;
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

	public float Probability = 1.0f;
	public bool Spawn = true;
	public bool IsInterior = false;

	public HorizontalFaceDetails Left;
	public VerticalFaceDetails Down;
	public HorizontalFaceDetails Back;
	public HorizontalFaceDetails Right;
	public VerticalFaceDetails Up;
	public HorizontalFaceDetails Forward;

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

	public Mesh GetMesh(bool createEmptyFallbackMesh = true) {
		var meshFilter = this.GetComponent<MeshFilter>();
		if (meshFilter != null && meshFilter.sharedMesh != null) {
			return meshFilter.sharedMesh;
		}
		if (createEmptyFallbackMesh) {
			var mesh = new Mesh();
			return mesh;
		}
		return null;
	}	
	
#if UNITY_EDITOR
	private static ModulePrototypeEditorData editorData;
	private static GUIStyle style;

	[DrawGizmo(GizmoType.InSelectionHierarchy | GizmoType.NotInSelectionHierarchy)]
	static void DrawGizmo(ModulePrototype modulePrototype, GizmoType gizmoType) {
		var transform = modulePrototype.transform;
		Vector3 position = transform.position;
		var rotation = transform.rotation;

		if (ModulePrototype.editorData == null || ModulePrototype.editorData.ModulePrototype != modulePrototype) {
			ModulePrototype.editorData = new ModulePrototypeEditorData(modulePrototype);
		}

		Gizmos.color = new Color(1f, 1f, 1f, 0.3f);
		if ((gizmoType & GizmoType.Selected) != 0) {
			for (int i = 0; i < 6; i++) {
				var hint = ModulePrototype.editorData.GetConnectorHint(i);
				if (hint.Mesh != null) {
					Gizmos.DrawMesh(hint.Mesh,
						position + rotation * Orientations.Direction[i].ToVector3() * AbstractMap.BLOCK_SIZE,
						rotation * Quaternion.Euler(Vector3.up * 90f * hint.Rotation));
				}
			}
		}
		for (int i = 0; i < 6; i++) {	
			if (modulePrototype.Faces[i].Walkable) {
				Gizmos.color = Color.red;
				Gizmos.DrawLine(position + Vector3.down * 0.1f, position + rotation * Orientations.Rotations[i] * Vector3.forward * AbstractMap.BLOCK_SIZE * 0.5f + Vector3.down * 0.1f);
			}
			if (modulePrototype.Faces[i].IsOcclusionPortal) {
				Gizmos.color = Color.blue;

				var dir = rotation * Orientations.Rotations[i] * Vector3.forward;
				Gizmos.DrawWireCube(position + dir, (Vector3.one - new Vector3(Mathf.Abs(dir.x), Mathf.Abs(dir.y), Mathf.Abs(dir.z))) * AbstractMap.BLOCK_SIZE);
			}			
		}

		if (ModulePrototype.style == null) {
			ModulePrototype.style = new GUIStyle();
			ModulePrototype.style.alignment = TextAnchor.MiddleCenter;
		}

		ModulePrototype.style.normal.textColor = Color.black;
		for (int i = 0; i < 6; i++) {
			var face = modulePrototype.Faces[i];
			Handles.Label(position + rotation * Orientations.Rotations[i] * Vector3.forward * InfiniteMap.BLOCK_SIZE / 2f, face.ToString(), ModulePrototype.style);
		}
	}
#endif
	
	public bool CompareRotatedVariants(int r1, int r2) {
		if (!(this.Faces[Orientations.UP] as VerticalFaceDetails).Invariant || !(this.Faces[Orientations.DOWN] as VerticalFaceDetails).Invariant) {
			return false;
		}

		for (int i = 0; i < 4; i++) {
			var face1 = this.Faces[Orientations.Rotate(Orientations.HorizontalDirections[i], r1)] as HorizontalFaceDetails;
			var face2 = this.Faces[Orientations.Rotate(Orientations.HorizontalDirections[i], r2)] as HorizontalFaceDetails;

			if (face1.Connector != face2.Connector) {
				return false;
			}

			if (!face1.Symmetric && !face2.Symmetric && face1.Flipped != face2.Flipped) {
				return false;
			}
		}

		return true;
	}

	void Update() { }

	void Reset() {
		this.Up = new VerticalFaceDetails();
		this.Down = new VerticalFaceDetails();
		this.Right = new HorizontalFaceDetails();
		this.Left = new HorizontalFaceDetails();
		this.Forward = new HorizontalFaceDetails();
		this.Back = new HorizontalFaceDetails();

		foreach (var face in this.Faces) {
			face.ExcludedNeighbours = new ModulePrototype[] { };
		}
	}
}
