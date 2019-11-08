using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEditor;

public class Node {
	public readonly Vector3 Position;
	public readonly Vector3 Direction;
	public readonly TreeGenerator Tree;
	
	public readonly int Depth;

	public float Light;

	public Node[] Children;

	public SphereCollider LeafCollider;

	public float Energy;

	public int Age = 0;

	public readonly Node Parent;

	public int MaxDistanceToLeaf {
		get;
		private set;
	}

	public Node(Vector3 position, TreeGenerator tree) {
		this.Position = position;
		this.Direction = Vector3.up;
		this.Depth = 0;
		this.Children = new Node[] { };
		this.Tree = tree;
	}

	public Node(Vector3 position, Node parent) {
		this.Position = position;
		this.Direction = (this.Position - parent.Position).normalized;
		this.Depth = parent.Depth + 1;
		this.Children = new Node[] { };
		parent.Children = parent.Children.Concat(new Node[] { this }).ToArray();
		this.Tree = parent.Tree;
		//parent.RemoveLeafCollider();
		this.CrateLeafCollider();
		this.Parent = parent;
	}

	public void CrateLeafCollider() {
		var go = new GameObject();
		go.transform.parent = this.Tree.transform;
		go.transform.localPosition = this.Position;
		this.LeafCollider = go.AddComponent<SphereCollider>();
		this.LeafCollider.radius = this.Tree.LeafColliderSize;
	}

	public void RemoveLeafCollider() {
		if (this.LeafCollider == null) {
			return;
		}
		GameObject.DestroyImmediate(this.LeafCollider.gameObject);
	}

	public void Draw() {
		TreeGenerator.GUIStyle.normal.textColor = Color.Lerp(Color.red, Color.black, (this.Energy - this.Tree.MinEnergy) / (this.Tree.MaxEnergy - this.Tree.MinEnergy));
		if (this.Children.Length < 2) {
			Handles.Label(this.Position, this.Energy.ToString("0.00"), TreeGenerator.GUIStyle);
		}
		//Gizmos.DrawWireSphere(this.Position, 0.1f);
		foreach (var child in this.Children) {
			Gizmos.DrawLine(this.Position, child.Position);
			child.Draw();
		}
	}

	public IEnumerable<Node> GetTree() {
		return this.Children.SelectMany(node => node.GetTree()).Concat(new Node[] { this });
	}
	
	private float raycast(Vector3 position, Vector3 direction, float skip = 0f) {
		var ray = new Ray(this.Tree.transform.position + position + direction.normalized * skip, direction);
		float result = float.PositiveInfinity;
		RaycastHit hit;
		if (Physics.Raycast(ray, out hit)) {
			result = hit.distance;
		}
		return result + skip;
	}


	public void Branch() {
		if (this.Children.Count() != 1) {
			return;
		}

		float length = this.Tree.BranchLength * Mathf.Pow(this.Tree.BranchLengthFalloff, this.Depth);

		var childDir = this.Children[0].Direction;

		var directions = Enumerable.Range(0, 20).Select(_ => childDir * Mathf.Cos(this.Tree.BranchAngle * Mathf.Deg2Rad) + Vector3.Cross(childDir, Random.onUnitSphere) * Mathf.Sign(this.Tree.BranchAngle * Mathf.Rad2Deg)).ToArray();
		var distances = directions.Select(d => this.raycast(this.Position, d, this.Tree.LeafColliderSize * 1.1f)).ToArray();
		int index = Enumerable.Range(0, 20).GetBest(i => distances[i]);
		if (distances[index] < length) {
			return;
		}

		var child = new Node(this.Position + directions[index] * length, this);
	}

	public void Grow() {
		if (this.Children.Count() != 0) {
			return;
		}

		float length = this.Tree.BranchLength * Mathf.Pow(this.Tree.BranchLengthFalloff, this.Depth);

		var directions = Enumerable.Range(0, 20).Select(_ => (this.Direction + this.Tree.Distort * Random.onUnitSphere).normalized).ToArray();
		var distances = directions.Select(d => this.raycast(this.Position, d, this.Tree.LeafColliderSize * 1.1f)).ToArray();
		int index = Enumerable.Range(0, 20).GetBest(i => distances[i]);
		if (distances[index] < length) {
			return;
		}

		var child = new Node(this.Position + directions[index] * length, this);
	}

	public void CalculateEnergy() {
		float result = 0f;
		/*for (int i = 0; i < 5; i++) {
			var dir = Random.onUnitSphere;
			dir.y = Mathf.Abs(dir.y);
			float dist = this.Tree.raycast(this.Position, dir, this.Tree.LeafColliderSize * 1.1f);
			result += 1f - Mathf.Exp(-dist);
		}*/
		result -= this.Tree.DepthPenalty * this.Depth;
		result /= 10f;

		result += 1f - Mathf.Exp(-this.raycast(this.Position, Vector3.up, this.Tree.LeafColliderSize * 1.1f));
		this.Energy = result;
	}

	public void CalculateDistanceToLeaf() {
		if (this.Children.Length == 0) {
			this.MaxDistanceToLeaf = 1;
		} else {
			foreach (var child in this.Children) {
				child.CalculateDistanceToLeaf();
			}
			this.MaxDistanceToLeaf = this.Children.Max(child => child.MaxDistanceToLeaf) + 1;
		}
	}
}