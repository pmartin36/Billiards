using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Car : MonoBehaviour
{
	protected CarController2D controller;

	public float MaxJumpHeight;
	public float MinJumpHeight;
	public float TimeToJumpApex;
	protected float gravity;
	protected float maxJumpVelocity, minJumpVelocity;

	public float MaxSpeed;

	protected Vector2 velocity;
	protected float vGravity;
	protected Vector2 inputDirection = Vector2.left;

	protected bool GasApplied;
	protected bool BreakApplied;

	protected virtual void Start() {	
		controller = GetComponent<CarController2D>();
		gravity = -(2 * MaxJumpHeight) / Mathf.Pow(TimeToJumpApex, 2);
		maxJumpVelocity = Mathf.Abs(gravity) * TimeToJumpApex;
		minJumpVelocity = Mathf.Sqrt(2 * Mathf.Abs(gravity) * MinJumpHeight);

		GasApplied = true;
		BreakApplied = false;
	}

	protected virtual void FixedUpdate() {
        Vector2 dir = Utils.AngleToVector(transform.eulerAngles.z + 90);

		bool fwTouch = controller.FrontWheel.UpdateIsTouchingGround();
		bool bwTouch = controller.BackWheel.UpdateIsTouchingGround();
		CalculateVelocity(fwTouch, bwTouch);

		Vector2 v = velocity * Time.fixedDeltaTime;
		controller.Move(ref v, inputDirection);

		controller.RotationCollisions(v, vGravity);

		// update non-touching tires		
		if(controller.BothWheelsTouchingGround && Mathf.Abs(v.y) <= 0.01f) {
			vGravity = 0;
		}

		if (v.y > 0.0001f) {
			velocity.y = (v.y / Time.fixedDeltaTime);
			vGravity = 0f;
		}
		else {
			velocity.y = vGravity;
		}

		if (BreakApplied || !GasApplied) {
			Vector2 frictionDelta = new Vector2(velocity.x, 0);
			frictionDelta *= GetFrictionDeltaModifier();
			velocity -= frictionDelta * Time.fixedDeltaTime;
		}
	}

	

	protected void CalculateVelocity(bool frontWheelTouching, bool backWheelTouching) {
		vGravity += gravity * Time.fixedDeltaTime;
		Vector2 v = Vector2.up * vGravity;
		Vector2 delta = Vector2.zero;
		if(frontWheelTouching || backWheelTouching) {
			delta = GetHorizontalMoveDelta() * Vector2.right * Time.fixedDeltaTime;
			// TODO: Implement Max Speed
		
		}
		v += delta;
		velocity += v;
	}

	protected virtual float GetHorizontalMoveDelta() {
		return Mathf.Abs(velocity.x) < 1f ? 10f : 5;
	}

	protected virtual float GetFrictionDeltaModifier() {
		return BreakApplied && controller.collisions.below ? 3f : 1f;
	}
}
