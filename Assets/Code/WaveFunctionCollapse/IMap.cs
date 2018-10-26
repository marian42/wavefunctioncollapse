using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IMap {
	Slot GetSlot(Vector3i position);
}
