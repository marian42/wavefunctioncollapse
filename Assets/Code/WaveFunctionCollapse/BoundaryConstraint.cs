using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BoundaryConstraint {
	public enum ConstraintMode {
		EnforceConnector,
		ExcludeConnector
	}

	public int RelativeY = 0;
	public Orientations.Enum Direction;
	public ConstraintMode Mode;
	public int Connector;
}
