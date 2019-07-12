using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CarController2D : PolygonRaycastController {

	public CollisionInfo collisions;
	[HideInInspector]
	public Vector2 playerInput;

	public Wheel FrontWheel { get; set; }
	public Wheel BackWheel { get; set; }
	public bool BothWheelsTouchingGround { get => FrontWheel.IsTouchingGround && BackWheel.IsTouchingGround; }

	private float maxAngleDiff = 61f;
	private float distBetweenWheels;

	public override void Start() {
		base.Start();

		Wheel[] wheels = GetComponentsInChildren<Wheel>();
		FrontWheel = wheels.FirstOrDefault(w => w.transform.localPosition.x > 0);
		BackWheel = wheels.FirstOrDefault(w => w.transform.localPosition.x < 0);

		distBetweenWheels = (FrontWheel.transform.position - BackWheel.transform.position).magnitude;
	}

	public void Move(ref Vector2 moveAmount) {
		Move(ref moveAmount, Vector2.zero);
	}

	public void Move(ref Vector2 moveAmount, Vector2 input) {
		PointsToCast p2c = GetPointsToCastViaCast(moveAmount);

		collisions.Reset();
		collisions.moveAmountOld = moveAmount;
		playerInput = input;

		Vector2 moveAmountCopy = new Vector2(moveAmount.x, moveAmount.y);

		HorizontalCollisions(ref moveAmount, p2c.Horizontal);
		if (moveAmount.y != 0) {
			VerticalCollisions(ref moveAmount, p2c.Vertical);
		}

		collisions.ridingSlopedWall = OverhangMovement(ref moveAmountCopy);
		if (collisions.ridingSlopedWall) {
			moveAmount = moveAmountCopy;
		}

		transform.position += (Vector3)moveAmount;
	}

	void HorizontalCollisions(ref Vector2 moveAmount, List<Vector2> points) {
		float directionX = Mathf.Sign(moveAmount.x);
		float rayLength = Mathf.Abs(moveAmount.x) + skinWidth;

		if (Mathf.Abs(moveAmount.x) < skinWidth) {
			rayLength = 2 * skinWidth;
		}

		float distanceToSlopeStart = rayLength;
		var moveAmountCopy = new Vector2(moveAmount.x, moveAmount.y);

		Wheel primaryWheel;
		Wheel secondaryWheel;
		if(moveAmount.x * transform.localScale.x > 0) {
			primaryWheel = FrontWheel;
			secondaryWheel = BackWheel;
		}
		else {
			primaryWheel = BackWheel;
			secondaryWheel = FrontWheel;
		}
		RaycastHit2D wheelHit = primaryWheel.Cast(Vector2.right * directionX, rayLength);
		if(wheelHit.collider == null) {
			wheelHit = secondaryWheel.Cast(Vector2.right * directionX, rayLength);
		}

		if(wheelHit.collider != null) {
			if (collisions.descendingSlope) {
				collisions.descendingSlope = false;
				moveAmount = collisions.moveAmountOld;
			}

			float slopeAngle = Vector2.Angle(wheelHit.normal, Vector2.up);
			float angleDiff = slopeAngle - transform.rotation.z;

			if (angleDiff < maxAngleDiff) {
				if (wheelHit.distance < distanceToSlopeStart - skinWidth) {
					distanceToSlopeStart = wheelHit.distance - skinWidth;
					moveAmount = new Vector2(moveAmountCopy.x, moveAmountCopy.y);

					ClimbSlope(ref moveAmount, slopeAngle, wheelHit.normal);
					moveAmount.x += distanceToSlopeStart * directionX;
				}
			}

			// actually stop the car from moving this direction if we're not climbing slope
			if (!collisions.climbingSlope || angleDiff > maxAngleDiff) {
				moveAmount.x = (wheelHit.distance - skinWidth) * directionX;
				rayLength = wheelHit.distance;

				if (collisions.climbingSlope) {
					moveAmount.y = Mathf.Tan(collisions.slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(moveAmount.x);
				}

				collisions.left = directionX == -1;
				collisions.right = directionX == 1;
			}
		} 

		for (int i = points.Count - 1; i >= 0; i--) {
			var point = points[i];
			RaycastHit2D hit = Physics2D.Raycast(point, Vector2.right * directionX, rayLength, collisionMask);

			Debug.DrawRay(point, Vector2.right * directionX * rayLength, Color.red);

			if (hit) {
				if (hit.distance == 0 || hit.distance > Mathf.Abs(moveAmount.x)) {
					continue;
				}

				float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
				float angleDiff = slopeAngle - transform.rotation.z;

				// actually stop the car from moving this direction if we're not climbing slope
				if (!collisions.climbingSlope || angleDiff > maxAngleDiff) {
					moveAmount.x = (hit.distance - skinWidth) * directionX;
					rayLength = hit.distance;

					if (collisions.climbingSlope) {
						moveAmount.y = Mathf.Tan(collisions.slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(moveAmount.x);
					}

					collisions.left = directionX == -1;
					collisions.right = directionX == 1;
				}
			}
		}
	}

	void VerticalCollisions(ref Vector2 moveAmount, List<Vector2> points) {
		float directionY = Mathf.Sign(moveAmount.y);
		float rayLength = Mathf.Abs(moveAmount.y) + skinWidth;

		HandleWheelVerticalHit(FrontWheel, ref moveAmount, ref rayLength, directionY);
		HandleWheelVerticalHit(BackWheel, ref moveAmount, ref rayLength, directionY);

		for (int i = 0; i < points.Count; i++) {
			var point = points[i];
			RaycastHit2D hit = Physics2D.Raycast(point, Vector2.up * directionY, rayLength, collisionMask);

			Debug.DrawRay(point, Vector2.up * directionY * rayLength, Color.red);
			HandleVerticalHit(hit, ref moveAmount, ref rayLength, directionY);
		}

		//if (collisions.climbingSlope) {
		//	float directionX = Mathf.Sign(moveAmount.x);
		//	rayLength = Mathf.Abs(moveAmount.x) + skinWidth;
		//	Vector2 rayOrigin = ((directionX == -1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight) + Vector2.up * moveAmount.y;
		//	RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLength, collisionMask);

		//	if (hit) {
		//		float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
		//		if (slopeAngle != collisions.slopeAngle) {
		//			moveAmount.x = (hit.distance - skinWidth) * directionX;
		//			collisions.slopeAngle = slopeAngle;
		//			collisions.slopeNormal = hit.normal;
		//		}
		//	}
		//}
	}

	void HandleWheelVerticalHit(Wheel wheel, ref Vector2 moveAmount, ref float rayLength, float directionY) {
		RaycastHit2D hit = wheel.Cast(directionY * Vector2.up, rayLength);
		HandleVerticalHit(hit, ref moveAmount, ref rayLength, directionY);
		if(hit.collider != null) {
			Debug.DrawLine(wheel.transform.position, hit.point, Color.blue);
		}
	}

	void HandleVerticalHit(RaycastHit2D hit, ref Vector2 moveAmount, ref float rayLength, float directionY) {
		if (hit.collider != null && hit.distance > 0) {
			moveAmount.y = (hit.distance - skinWidth) * directionY;
			rayLength = hit.distance;

			if (collisions.climbingSlope) {
				moveAmount.x = moveAmount.y / Mathf.Tan(collisions.slopeAngle * Mathf.Deg2Rad) * Mathf.Sign(moveAmount.x);
			}

			collisions.below = directionY == -1;
			collisions.above = directionY == 1;
		}
	}

	public void RotationCollisions(Vector2 moveAmount, float vGravity) {
		bool fwTouch = FrontWheel.UpdateIsTouchingGround();
		bool bwTouch = BackWheel.UpdateIsTouchingGround();
		if (!bwTouch && fwTouch) {
			// affect by gravity + slope movement
			TryGroundWheel(BackWheel, FrontWheel, moveAmount, vGravity);
		}
		else if (!fwTouch && bwTouch) {
			TryGroundWheel(FrontWheel, BackWheel, moveAmount, vGravity);
		}
		else if(!fwTouch && !bwTouch) {

		}
	}

	private void TryGroundWheel(Wheel airWheel, Wheel groundedWheel, Vector2 v, float vGravity) {
		float vgrav = Mathf.Min(Mathf.Cos(transform.eulerAngles.z * Mathf.Deg2Rad) * vGravity * Time.fixedDeltaTime, -0.01f);
		float dy = vgrav;

		// when top wheel has crested the slope, we don't want to drag it down from the bottom wheel
		if (!(collisions.climbingSlope && (airWheel.transform.position.y - groundedWheel.transform.position.y) > 0.25f)) {
			dy -= Mathf.Max(0, v.y);
		}

		Vector2 diff = airWheel.transform.position - groundedWheel.transform.position;
		float newY = diff.y + dy;
		float newYSqr = newY * newY;

		// which direction should the car rotate in
		float xDir = Mathf.Sign((Quaternion.Euler(0, 0, -transform.eulerAngles.z) * diff).x * transform.localScale.x);

		// find the x position, given the newY such that the magnitude remains the same
		Vector2 idealWheelPositionRelativeToGroundedWheel = new Vector2(
			xDir * Mathf.Sqrt(Mathf.Abs(diff.sqrMagnitude - newYSqr)),
			newY);

		// translation vector from old wheel position to new wheel position
		Vector2 wheelMove = idealWheelPositionRelativeToGroundedWheel - diff;

		float rotationAngle = 0;
		Vector2 newPosition = idealWheelPositionRelativeToGroundedWheel;
		RaycastHit2D hit = airWheel.Cast(wheelMove.normalized, wheelMove.magnitude, skinWidth);
		if (hit.collider != null && hit.distance > 0) {
			newPosition = diff + (hit.distance - skinWidth) * wheelMove.normalized;
			rotationAngle = Vector2.SignedAngle(diff, newPosition);
		}
		else {
			rotationAngle = Vector2.SignedAngle(diff, idealWheelPositionRelativeToGroundedWheel);
		}

		Vector2 pointCastDirection = wheelMove.normalized;
		int i = 0;
		bool hasHit = false;
		foreach(RaycastHit2D p in GetHitsForCastAtAngle(pointCastDirection)) {
			if(p.collider == null) {
				if(hasHit || i > 10) {
					break;
				}
				i++;
				continue;
			}
			hasHit = true;
			Vector2 startingPosition = p.point + v;

			Vector2 pDiff =  startingPosition - (Vector2)groundedWheel.transform.position; 
			Vector2 rotatedPoint = Quaternion.Euler(0, 0, rotationAngle) * pDiff + groundedWheel.transform.position;

			Vector2 dir = (rotatedPoint - startingPosition).normalized;

			hit = Physics2D.Linecast(startingPosition, rotatedPoint, collisionMask);
			Debug.DrawLine(startingPosition, rotatedPoint, Color.cyan);
			if (hit.collider != null && hit.distance > 0) {
				
				newPosition = pDiff + hit.distance * dir;
				rotationAngle = Vector2.SignedAngle(pDiff, newPosition);

				pointCastDirection = dir;
			}
		}

		transform.RotateAround(groundedWheel.transform.position, Vector3.forward, rotationAngle);
	}

	private Vector2 CalculateRotatedPosition(Vector2 position, Vector2 rotationPoint, float dy) {
		Vector2 diff = position - rotationPoint;
		dy *= diff.magnitude / distBetweenWheels;

		float newY = diff.y + dy;
		float newYSqr = newY * newY;

		// which direction should the car rotate in
		float xDir = Mathf.Sign((Quaternion.Euler(0, 0, -transform.eulerAngles.z) * diff).x * transform.localScale.x);

		// find the x position, given the newY such that the magnitude remains the same
		return new Vector2(
			xDir * Mathf.Sqrt(Mathf.Abs(diff.sqrMagnitude - newYSqr)),
			newY);
	}

	void ClimbSlope(ref Vector2 moveAmount, float slopeAngle, Vector2 slopeNormal) {
		float moveDistance = Mathf.Abs(moveAmount.x);
		float climbmoveAmountY = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * moveDistance;

		if (moveAmount.y <= climbmoveAmountY) {
			moveAmount.y = climbmoveAmountY;
			moveAmount.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign(moveAmount.x);
			collisions.below = true;
			collisions.climbingSlope = true;
			collisions.slopeAngle = slopeAngle;
			collisions.slopeNormal = slopeNormal;
		}
	}

	void DescendSlope(ref Vector2 moveAmount) {
		//RaycastHit2D maxSlopeHitLeft = Physics2D.Raycast(raycastOrigins.bottomLeft, Vector2.down, Mathf.Abs(moveAmount.y) + skinWidth, collisionMask);
		//RaycastHit2D maxSlopeHitRight = Physics2D.Raycast(raycastOrigins.bottomRight, Vector2.down, Mathf.Abs(moveAmount.y) + skinWidth, collisionMask);
		//if (maxSlopeHitLeft ^ maxSlopeHitRight) {
		//	SlideDownMaxSlope(maxSlopeHitLeft, ref moveAmount);
		//	SlideDownMaxSlope(maxSlopeHitRight, ref moveAmount);
		//}

		//if (!collisions.slidingDownMaxSlope) {
		//	float directionX = Mathf.Sign(moveAmount.x);
		//	Vector2 rayOrigin = (directionX == -1) ? raycastOrigins.bottomRight : raycastOrigins.bottomLeft;
		//	RaycastHit2D hit = Physics2D.Raycast(rayOrigin, -Vector2.up, Mathf.Infinity, collisionMask);

		//	if (hit) {
		//		float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
		//		if (slopeAngle != 0 && slopeAngle <= maxSlopeAngle) {
		//			if (Mathf.Sign(hit.normal.x) == directionX) {
		//				if (hit.distance - skinWidth <= Mathf.Tan(slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(moveAmount.x)) {
		//					float moveDistance = Mathf.Abs(moveAmount.x);
		//					float descendmoveAmountY = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * moveDistance;
		//					moveAmount.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign(moveAmount.x);
		//					moveAmount.y -= descendmoveAmountY;

		//					collisions.slopeAngle = slopeAngle;
		//					collisions.descendingSlope = true;
		//					collisions.below = true;
		//					collisions.slopeNormal = hit.normal;
		//				}
		//			}
		//		}
		//	}
		//}
	}

	private bool OverhangMovement(ref Vector2 moveAmount) {
		//float directionX = Mathf.Sign(moveAmount.x);

		//float xMovement = Mathf.Abs(moveAmount.x);

		//if (!collisions.below) {
		//	fire ray in top right(going up), top left(going up), and top left / right(going left / right depending on direction)
		//	if (moveAmount.y > xMovement) {
		//		float yRayLength = Mathf.Abs(moveAmount.y) + skinWidth;
		//		RaycastHit2D topLeftUp = Physics2D.Raycast(raycastOrigins.topLeft, Vector2.up, yRayLength, collisionMask);
		//		RaycastHit2D topRightUp = Physics2D.Raycast(raycastOrigins.topRight, Vector2.up, yRayLength, collisionMask);

		//		if (topLeftUp.collider != null ^ topRightUp.collider != null) {
		//			RaycastHit2D hit;
		//			float xdir;
		//			if (topLeftUp.collider != null) {
		//				hit = topLeftUp;
		//				xdir = 1;
		//			}
		//			else {
		//				hit = topRightUp;
		//				xdir = -1;
		//			}
		//			float slopeAngle = Vector2.Angle(hit.normal, Vector2.down);

		//			float distanceToSlopeStart = hit.distance - skinWidth;
		//			float slopeMoveAmount = moveAmount.y - distanceToSlopeStart;

		//			float slopeMoveAmountY = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * slopeMoveAmount;
		//			float slopeMoveAmountX = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * xdir * slopeMoveAmount;

		//			moveAmount.y = distanceToSlopeStart + slopeMoveAmountY;
		//			if (slopeMoveAmountX * moveAmount.x > 0) {
		//				moveAmount.x = slopeMoveAmountX > 0 ? Mathf.Max(slopeMoveAmountX, moveAmount.x) : Mathf.Min(slopeMoveAmountX, moveAmount.x);
		//			}
		//			else {
		//				moveAmount.x = slopeMoveAmountX;
		//			}
		//			Debug.Log($"Overhung Up {(topLeftUp == hit ? "Left" : "Right")} ({moveAmount.x}, {moveAmount.y})");
		//			return true;
		//		}
		//	}
		//	else {
		//		RaycastHit2D hit;
		//		RaycastHit2D bottomHit;
		//		if (directionX > 0) {
		//			hit = Physics2D.Raycast(raycastOrigins.topRight, Vector2.right, xMovement + skinWidth, collisionMask);
		//			bottomHit = Physics2D.Raycast(raycastOrigins.bottomRight, Vector2.right, xMovement + skinWidth, collisionMask);
		//		}
		//		else {
		//			hit = Physics2D.Raycast(raycastOrigins.topLeft, Vector2.left, xMovement + skinWidth, collisionMask);
		//			bottomHit = Physics2D.Raycast(raycastOrigins.bottomLeft, Vector2.left, xMovement + skinWidth, collisionMask);
		//		}

		//		if (hit.collider != null) {
		//			float slopeAngle = Vector2.Angle(hit.normal, Vector2.down);

		//			float d = hit.distance;
		//			if (bottomHit.collider != null) {
		//				xMovement = bottomHit.distance - skinWidth;
		//			}
		//			float distanceToSlopeStart = hit.distance - skinWidth;
		//			float slopeMoveAmount = xMovement - distanceToSlopeStart;

		//			float slopeMoveAmountY = -Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * slopeMoveAmount;
		//			float slopeMoveAmountX = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * slopeMoveAmount;

		//			moveAmount.x = (distanceToSlopeStart + slopeMoveAmountX) * directionX;
		//			moveAmount.y = Mathf.Min(slopeMoveAmountY, moveAmount.y);
		//			Debug.Log($"Overhung {(directionX > 0 ? "Right" : "Left")} ({moveAmount.x}, {moveAmount.y})");
		//			return true;
		//		}
		//	}
		//}
		return false;
	}
}
