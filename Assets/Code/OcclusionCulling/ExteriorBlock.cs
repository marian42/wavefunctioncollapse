using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExteriorBlock {
	public bool Visible {
		get;
		private set;
	}

	private Renderer[] renderers;

	public readonly Bounds Bounds;

	public ExteriorBlock(Slot slot, MapBehaviour mapBehaviour) {
		this.Visible = true;
		this.renderers = slot.GameObject.GetComponentsInChildren<Renderer>();
		this.Bounds = new Bounds(mapBehaviour.GetWorldspacePosition(slot.Position), Vector3.one * 2f);
	}

	public void SetVisibility(bool value) {
		if (this.Visible == value) {
			return;
		}
		this.Visible = value;
		for (int i = 0; i < this.renderers.Length; i++) {
			this.renderers[i].enabled = value;
		}
	}
}
