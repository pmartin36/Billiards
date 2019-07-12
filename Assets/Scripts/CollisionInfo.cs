using UnityEngine;

public struct CollisionInfo {
	public bool above, below;
	public bool left, right;

	public bool climbingSlopeOld;
	public bool climbingSlope;
	public bool descendingSlope;
	public bool slidingDownMaxSlope;

	public bool ridingSlopedWall;

	public float slopeAngle, slopeAngleOld;
	public Vector2 slopeNormal;
	public Vector2 moveAmountOld;
	public int faceDir;
	public bool fallingThroughPlatform;

	public void Reset() {
		above = below = false;
		left = right = false;
		descendingSlope = false;
		slidingDownMaxSlope = false;
		slopeNormal = Vector2.zero;

		climbingSlopeOld = climbingSlope;
		climbingSlope = false;

		slopeAngleOld = slopeAngle;
		slopeAngle = 0;
	}
}
