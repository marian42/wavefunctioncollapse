using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Room {
	public HashSet<Slot> Slots;
	public List<Portal> Portals;

	public Room() {
		this.Slots = new HashSet<Slot>();
	}
}
