using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
public class TreeGenerator : MonoBehaviour {

	[Range(0.0f, 0.3f)]
	public float StemSize = 0.3f;

	[Range(0.8f, 1f)]
	public float SizeFalloff = 0.9f;

	[Range(0f, 0.5f)]
	public float Distort = 0.5f;

	[Range(0f, 1.2f)]
	public float BranchLength = 2f;

	[Range(0.1f, 0.5f)]
	public float LeafColliderSize = 0.2f;

	[Range(0f, 90f)]
	public float BranchAngle = 30f;

	[Range(0.9f, 1f)]
	public float BranchLengthFalloff = 0.9f;

	[Range(0.0f, 0.01f)]
	public float DepthPenalty = 0.0f;

	[Range(10, 1000)]
	public int Iterations = 100;

	public Material Material;

	public Material LeafMaterial;

	public Node Root;

	public float MinEnergy;
	public float MaxEnergy;


#if UNITY_EDITOR
	[DrawGizmo(GizmoType.Selected)]
	static void DrawGizmo(TreeGenerator tree, GizmoType gizmoType) {
		return;
		Gizmos.color = Color.green;
		if (tree.Root != null && false) {
			tree.Root.Draw();
		}
	}
#endif

	private void calculateEnergy() {
		foreach (var node in this.Root.GetTree()) {
			if (node.Children.Length == 2) {
				continue;
			}
			node.CalculateEnergy();
		}
		this.MinEnergy = this.Root.GetTree().Where(n => n.Children.Length < 2).Select(n => n.Energy).Min();
		this.MaxEnergy = this.Root.GetTree().Where(n => n.Children.Length < 2).Select(n => n.Energy).Max();
	}

	public static GUIStyle GUIStyle;
	
	public void Reset() {
		TreeGenerator.GUIStyle = new GUIStyle();
		TreeGenerator.GUIStyle.normal.textColor = Color.red;
		this.Age = 0;
		this.transform.DeleteChildren();
		this.Root = new Node(this.transform.position, this);
		this.calculateEnergy();
	}

	public int Age = 0;

	public void Grow(int batchSize) {
		if (this.Root == null) {
			this.Reset();
		}

		var nodes = this.Root.GetTree().ToArray();
		nodes = nodes.OrderByDescending(n => n.Energy).ToArray();

		foreach (var node in nodes) {
			node.Age++;
		}
		this.Age++;

		foreach (var node in nodes) {
			if (node.Children.Length == 0) {
				node.Grow();
				batchSize--;
			}
			if (node.Children.Length == 1 && node.Depth > 1) {
				node.Branch();
				batchSize--;
			}
			if (batchSize < 0) {
				break;
			}
		}

		this.calculateEnergy();
	}

	public void Prune(float amount) {
		var nodes = this.Root.GetTree().Where(n => n.Children.Length == 0).ToArray();
		nodes = nodes.OrderByDescending(n => n.Energy).ToArray();

		for (int i = 0; i < nodes.Length * amount; i++) {
			if (nodes[i].Parent == null) {
				continue;
			}
			nodes[i].Parent.Children = nodes[i].Parent.Children.Where(n => n != nodes[i]).ToArray();
		}		
	}

	private void createBranch(Vector3 from, Vector3 to, float radius) {
		var gameObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
		gameObject.transform.parent = this.transform;
		gameObject.transform.position = 0.5f * (from + to);
		gameObject.transform.rotation = Quaternion.FromToRotation(Vector3.up, (to - from));
		gameObject.transform.localScale = new Vector3(radius, Vector3.Distance(from, to) / 2f, radius);
		gameObject.GetComponent<MeshRenderer>().sharedMaterial = this.Material;
	}

	public int MeshSubdivisions = 5;

	[Range(1, 20)]
	public int BatchSize = 5;

	public void Build() {
		this.Reset();

		for (int i = 0; i < this.Iterations / this.BatchSize; i++) {
			this.Grow(this.BatchSize);
		}

		this.Prune(0.2f);

		this.GetComponent<MeshFilter>().sharedMesh = this.CreateMesh(this.MeshSubdivisions);
		this.CreateLeaves();
	}

	public Mesh CreateMesh(int subdivisions) {
		var mesh = new Mesh();

		var vertices = new List<Vector3>();
		var normals = new List<Vector3>();
		var triangles = new List<int>();
		var indices = new Dictionary<Node, int>();

		foreach (var node in this.Root.GetTree()) {
			float radius = node.Children.Any() ? this.StemSize * Mathf.Pow(this.SizeFalloff, node.Depth) : 0f;
			indices[node] = vertices.Count;
			var direction = node.Children.Any() ? node.Children.Aggregate<Node, Vector3>(Vector3.zero, (v, n) => v + n.Direction).normalized : node.Direction;
			var tangent = Vector3.Cross(Vector3.forward, direction);
			for (int i = 0; i < subdivisions; i++) {
				var normal = Quaternion.AngleAxis(360f * (float)i / subdivisions, direction) * tangent;
				normal.Normalize();
				normals.Add(normal);
				vertices.Add(node.Position + normal * radius - this.transform.position);
			}
		}
		
		foreach (var node in this.Root.GetTree()) {
			int nodeIndex = indices[node];

			foreach (var child in node.Children) {
				int childIndex = indices[child];

				for (int i = 0; i < subdivisions; i++) {
					triangles.Add(nodeIndex + i);
					triangles.Add(nodeIndex + (i + 1) % subdivisions);
					triangles.Add(childIndex + i);
				}

				if (child.Children.Length != 0) {
					for (int i = 0; i < subdivisions; i++) {
						triangles.Add(nodeIndex + (i + 1) % subdivisions);
						triangles.Add(childIndex + (i + 1) % subdivisions);
						triangles.Add(childIndex + i);
					}
				}
			}
		}

		mesh.vertices = vertices.ToArray();
		mesh.normals = normals.ToArray();
		mesh.triangles = triangles.ToArray();

		return mesh;
	}

	[Range(0.2f, 1f)]
	public float LeafRadius = 0.3f;

	public void CreateLeaves() {
		var mesh = new Mesh();

		var vertices = new List<Vector3>();
		var normals = new List<Vector3>();
		var triangles = new List<int>();
		var uvs = new List<Vector2>();

		foreach (var node in this.Root.GetTree()) {
			if (node.Children.Length != 0) {
				continue;
			}

			for (int i = 0; i < 16; i++) {
				int index = vertices.Count;

				var normal = Random.onUnitSphere;
				var tangent1 = Vector3.Cross(Random.onUnitSphere, normal).normalized;
				var tangent2 = Vector3.Cross(normal, tangent1).normalized;

				vertices.Add(node.Position + tangent1 * this.LeafRadius);
				uvs.Add(new Vector2(0f, 1f));
				vertices.Add(node.Position + tangent2 * this.LeafRadius);
				uvs.Add(new Vector2(1f, 1f));
				vertices.Add(node.Position - tangent1 * this.LeafRadius);
				uvs.Add(new Vector2(1f, 0f));
				vertices.Add(node.Position - tangent2 * this.LeafRadius);
				uvs.Add(new Vector2(0f, 0f));
				normals.Add(normal);
				normals.Add(normal);
				normals.Add(normal);
				normals.Add(normal);
				triangles.AddRange(new int[] { index + 0, index + 1, index + 2, index + 2, index + 3, index + 0 });
			}
		}

		mesh.vertices = vertices.ToArray();
		mesh.normals = normals.ToArray();
		mesh.triangles = triangles.ToArray();
		mesh.uv = uvs.ToArray();

		var gameObject = new GameObject();
		gameObject.transform.parent = this.transform;
		gameObject.transform.position = Vector3.zero;
		gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
		gameObject.AddComponent<MeshRenderer>().sharedMaterial = this.LeafMaterial;
	}
}
