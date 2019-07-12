using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StupidCircle : MonoBehaviour
{
	private Rigidbody2D rigid;
	private CircleCollider2D circ;
	private StupidCar car;

    void Start() {
		rigid = GetComponent<Rigidbody2D>();
		circ = GetComponent<CircleCollider2D>();
		car = GetComponentInParent<StupidCar>();
    }

    void FixedUpdate() {
		float g = 9.8f * Time.fixedDeltaTime * Time.fixedDeltaTime;
		RaycastHit2D hit = Physics2D.CircleCast(circ.bounds.center, circ.radius, Vector2.down, g, 1 << LayerMask.NameToLayer("Level"));
		if (hit.collider != null) {
			var dir = Quaternion.Euler(0, 0, 90) * hit.normal;
			car.AddForce(dir * 25f);
		}
		else {
			
		}
	}
}
