using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hole : MonoBehaviour
{

	public static Hole Prefab;
	public static Hole CreateInstance() {
		Prefab = Resources.Load<Hole>("Prefabs/Hole");
		return Instantiate(Prefab);
	}

	public void Place(HexGrid grid, HexCoordinates pos) {
		transform.localScale = Vector3.one * grid.OuterRadius * transform.localScale.x / 2f;
		transform.position = grid[pos].PhysicalCoordinates + Vector3.back * 0.5f;
	}
}
