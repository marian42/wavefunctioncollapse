using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
public class TreeGenerator : MonoBehaviour {

	[Range(0.0f, 0.6f)]
	public float StemSize = 0.3f;

	[Range(0.1f, 4f)]
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

	public bool GenerateLeaves = true;

	[Range(0.2f, 1f)]
	public float LeafRadius = 0.3f;


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
		this.Root = new Node(Vector3.zero, this);
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

		this.Root.CalculateDistanceToLeaf();

		this.GetComponent<MeshFilter>().sharedMesh = this.CreateMesh(this.MeshSubdivisions);
	}

	public Mesh CreateMesh(int subdivisions) {
		var vertices = new List<Vector3>();
		var normals = new List<Vector3>();
		var uvs = new List<Vector2>();
		var treeTriangles = new List<int>();
		var indices = new Dictionary<Node, int>();

		foreach (var node in this.Root.GetTree()) {
			float radius = node.Children.Length == 0 ? 0 : this.StemSize * Mathf.Pow((node.MaxDistanceToLeaf + 6) / (float)(this.Root.MaxDistanceToLeaf + 6), this.SizeFalloff);
			indices[node] = vertices.Count;
			var direction = node.Children.Any() ? node.Children.Aggregate<Node, Vector3>(Vector3.zero, (v, n) => v + n.Direction).normalized : node.Direction;
			if (node.Parent == null) {
				node.MeshOrientation = Vector3.Cross(Vector3.forward, direction);
			} else {
				node.MeshOrientation = (node.Parent.MeshOrientation - direction * Vector3.Dot(direction, node.Parent.MeshOrientation)).normalized;
			}
			for (int i = 0; i < subdivisions; i++) {
				float progress = (float)i / (subdivisions - 1);
				var normal = Quaternion.AngleAxis(360f * progress, direction) * node.MeshOrientation;
				normal.Normalize();
				normals.Add(normal);
				float offset = 0;
				if (node.Depth < 4) {
					offset = Mathf.Pow(Mathf.Abs(Mathf.Sin(progress * 2f * Mathf.PI * 5f)), 0.5f) * 0.5f * (3 - node.Depth) / 3f;
				}
				vertices.Add(node.Position + normal * radius * (1f + offset));
				uvs.Add(new Vector2(progress * 6f, (node.Depth % 2) * 3f));
			}
		}
		
		foreach (var node in this.Root.GetTree()) {
			int nodeIndex = indices[node];

			foreach (var child in node.Children) {
				int childIndex = indices[child];

				for (int i = 0; i < subdivisions - 1; i++) {
					treeTriangles.Add(nodeIndex + i);
					treeTriangles.Add(nodeIndex + i + 1);
					treeTriangles.Add(childIndex + i);
				}

				if (child.Children.Length != 0) {
					for (int i = 0; i < subdivisions - 1; i++) {
						treeTriangles.Add(nodeIndex + i + 1);
						treeTriangles.Add(childIndex + i + 1);
						treeTriangles.Add(childIndex + i);
					}
				}
			}
		}
		
		var leafTriangles = new List<int>();

		if (this.GenerateLeaves) {
			foreach (var node in this.Root.GetTree()) {
				if (node.Children.Length != 0) {
					continue;
				}

				for (int i = 0; i < 16; i++) {
					int index = vertices.Count;

					var normal = Random.onUnitSphere;
					var tangent1 = Vector3.Cross(Random.onUnitSphere, normal);
					var tangent2 = Vector3.Cross(normal, tangent1);

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
					leafTriangles.AddRange(new int[] { index + 0, index + 1, index + 2, index + 2, index + 3, index + 0 });
				}
			}
		}
		

		var mesh = new Mesh();
		mesh.subMeshCount = 2;
		mesh.vertices = vertices.ToArray();
		mesh.normals = normals.ToArray();
		mesh.uv = uvs.ToArray();
		mesh.SetTriangles(treeTriangles.ToArray(), 0);
		if (this.GenerateLeaves) {
			mesh.SetTriangles(leafTriangles.ToArray(), 1);
		}
		return mesh;
	}
}
