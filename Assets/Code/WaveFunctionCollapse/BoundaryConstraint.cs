using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BoundaryConstraint {
	public enum ConstraintMode {
		EnforceConnector,
		ExcludeConnector
	}

	public enum ConstraintDirection {
		Up,
		Down,
		Horizontal
	}

	public int RelativeY = 0;
	public ConstraintDirection Direction;
	public ConstraintMode Mode;
	public int Connector;
}
