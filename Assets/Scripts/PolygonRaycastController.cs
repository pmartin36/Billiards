using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PolygonRaycastController : MonoBehaviour
{
	public LayerMask collisionMask;

	public const float skinWidth = .015f;
	const float dstBetweenRays = .15f;

	[HideInInspector]
	public PolygonCollider2D collider;

	public virtual void Awake() {
		collider = GetComponent<PolygonCollider2D>();
	}

	public virtual void Start() {
		
	}

	public IEnumerable<RaycastHit2D> GetHitsForCastAtAngle(Vector2 dir) {
		Vector2 pos = collider.bounds.min;
		LayerMask layermask = 1 << gameObject.layer;
		float dist = collider.bounds.size.magnitude;
		while (true) {		
			Vector2 castDir = -dir;
			RaycastHit2D hit = Physics2D.Raycast(pos + dir * 0.5f * dist, castDir, dist  * 1.5f, layermask);
			Vector2 translate = new Vector2(castDir.y, -castDir.x) * dstBetweenRays;
			pos += translate; // rotate -90
			
			yield return hit;
		}
	}

	public PointsToCast GetPointsToCastViaCast(Vector3 direction) {
		PointsToCast p2c = new PointsToCast();

		LayerMask layermask = 1 << gameObject.layer;

		// horizontal - bottom to top
		float castDistance = collider.bounds.size.x + skinWidth * 2;
		float y = collider.bounds.min.y + 0.0001f;
		float start;
		Vector2 castDirection;
		if(direction.x < 0) {
			start = collider.bounds.min.x - skinWidth;
			castDirection = Vector2.right;
		}
		else {
			start = collider.bounds.max.x + skinWidth;
			castDirection = Vector2.left;
		}
		while(y < collider.bounds.max.y) {
			RaycastHit2D hit = Physics2D.Raycast(new Vector2(start, y), castDirection, castDistance, layermask);
			// should always hit
			p2c.Horizontal.Add(hit.point + castDirection * skinWidth);
			y += dstBetweenRays;
		}
		if (p2c.Horizontal.Last().y < collider.bounds.max.y) {
			RaycastHit2D hit = Physics2D.Raycast(new Vector2(start, collider.bounds.max.y), castDirection, castDistance, layermask);
			// should always hit
			p2c.Horizontal.Add(hit.point + castDirection * skinWidth);
		}

		// vertical - left to right
		castDistance = collider.bounds.size.y + skinWidth * 2;
		float x = collider.bounds.min.x + 0.0001f;
		if(direction.y < 0) {
			start = collider.bounds.min.y - skinWidth;
			castDirection = Vector2.up;
		}
		else {
			start = collider.bounds.max.x + skinWidth;
			castDirection = Vector2.down;
		}
		while(x < collider.bounds.max.x) {
			RaycastHit2D hit = Physics2D.Raycast(new Vector2(x, start), castDirection, castDistance, layermask);
			// should always hit
			p2c.Vertical.Add(hit.point + castDirection * skinWidth);
			x += dstBetweenRays;
		}
		if(p2c.Vertical.Last().x < collider.bounds.max.x) {
			RaycastHit2D hit = Physics2D.Raycast(new Vector2(collider.bounds.max.x, start), castDirection, castDistance, layermask);
			// should always hit
			p2c.Vertical.Add(hit.point + castDirection * skinWidth);
		}

		return p2c;
	}

	public PointsToCast GetPointsToCast(Vector3 direction) {
		PointsToCast p2c = new PointsToCast();


		Vector2 pos = transform.position;
		Vector2 prev = transform.rotation * collider.points.Last();
		prev.Scale(transform.localScale);

		foreach (Vector2 point in collider.points) {
			Vector2 p = (transform.rotation * point);
			p.Scale(transform.localScale);
			if (p.x * direction.x > 0) {
				Vector2 skin = Vector2.right * Mathf.Sign(direction.x) * skinWidth;
				p2c.Horizontal.Add(p + pos - skin);

				// if the distance between this ray and last is greater than
				// distBetweenRays, create rays spaced distBetweenRays apart
				float dist = Mathf.Abs(p.y - prev.y);
				if ( dist > dstBetweenRays
					&& prev.x * direction.x > 0 ) {
					float d = 0;
					while( d < dist ) {
						p2c.Horizontal.Add( Vector2.Lerp(p, prev, d / dist) + pos - skin);
						d += dstBetweenRays;
					}
				}
			}

			if(p.y * direction.y > 0) {
				Vector2 skin = Vector2.up * Mathf.Sign(direction.y) * skinWidth;
				p2c.Vertical.Add(p + pos - skin);

				// if the distance between this ray and last is greater than
				// distBetweenRays, create rays spaced distBetweenRays apart
				float dist = Mathf.Abs(p.x - prev.x);
				if (dist > dstBetweenRays
					&& prev.y * direction.y > 0) {
					float d = 0;
					while (d < dist) {
						p2c.Vertical.Add(Vector2.Lerp(p, prev, d / dist) + pos - skin);
						d += dstBetweenRays;
					}
				}
			}
			prev = p;
		}

		return p2c;
	}
}

public class PointsToCast {
	public List<Vector2> Horizontal;
	public List<Vector2> Vertical;

	public PointsToCast() {
		Horizontal = new List<Vector2>();
		Vertical = new List<Vector2>();
	}
}
