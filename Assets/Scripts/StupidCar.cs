using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StupidCar : MonoBehaviour
{
	private Rigidbody2D rigid;

	private void Start() {
		rigid = GetComponent<Rigidbody2D>();
	}

	public void AddForce(Vector2 force) {
		rigid.AddForce(force);
	}
}
