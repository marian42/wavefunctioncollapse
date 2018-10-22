using UnityEditor;
using UnityEngine;
using System.Linq;

[CustomEditor(typeof(BlockBehaviour))]
public class BlockBehaviourEditor : Editor {

	public override void OnInspectorGUI() {
		DrawDefaultInspector();

		BlockBehaviour block = (BlockBehaviour)target;
		for (int i = 0; i < 6; i++) {
			if (block.Neighbours != null && block.Neighbours[i] != null && GUILayout.Button("Exclude neighbor " + block.Neighbours[i].Prototype.gameObject.name + " (" + Orientations.Names[i] + ")")) {
				var p1 = block.Prototype;
				var p2 = block.Neighbours[i].Prototype;
				var list1 = p1.Faces[i].ExcludedNeighbours;
				var list2 = p2.Faces[(i + 3) % 6].ExcludedNeighbours;
				if (!list1.Contains(p2)) {
					p1.Faces[i].ExcludedNeighbours = list1.Concat(new ModulePrototype[] {p2}).ToArray();
					Debug.Log("Added exclusion rule.");
				}
				if (!list2.Contains(p1)) {
					p2.Faces[(i + 3) % 6].ExcludedNeighbours = list2.Concat(new ModulePrototype[] { p1 }).ToArray();
					Debug.Log("Added exclusion rule.");
				}
			}
		}
	}
}