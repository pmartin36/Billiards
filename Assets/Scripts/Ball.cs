using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Ball : MonoBehaviour, CameraFollowable {

	public static Ball Prefab;
	public static Ball CreateInstance() {
		Prefab = Resources.Load<Ball>("Prefabs/Ball");
		return Instantiate(Prefab);
	}

	public float Speed { get; protected set; }
	public Vector2 Direction { get; protected set; }
	public Vector2 Velocity { get => Speed * Direction; }
	public Vector2 Position { get => transform.position; }

	public float BallRadius { get => circleCollider.radius * transform.lossyScale.x; }

	public bool Sunk { get; set; }

	private CircleCollider2D circleCollider;
	private SpriteRenderer spriteRenderer;

	private Player player;
	private Collider2D holeCollider;
	public LayerMask collidableLayermask;

	private void Start() {
		circleCollider = GetComponent<CircleCollider2D>();
		spriteRenderer = GetComponent<SpriteRenderer>();

		collidableLayermask = 1 << LayerMask.NameToLayer("Level");
	}

	public void Place(HexGrid grid, HexCoordinates pos) {
		transform.localScale = Vector3.one * grid.OuterRadius * transform.localScale.x / 2f;
		transform.position = grid[pos].PhysicalCoordinates + Vector3.back * 0.5f;
	}

	private void OnTriggerEnter2D(Collider2D collision) {
		if(collision.CompareTag("Player")) {
			if(Speed < 0.1f) {
				player = collision.GetComponent<Player>();
				player.Frozen = true;
				player.SetActive(false);

				Direction = (transform.position - player.transform.position).normalized;	
				Speed = player.Speed;

				Camera.main.GetComponent<CameraController>().SetFollowing(this, null);
			}
		}
		else if (collision.CompareTag("Hole")) {
			holeCollider = collision;
		}
	}

	private void OnTriggerExit2D(Collider2D collision) {
		if (collision.CompareTag("Hole") && holeCollider == collision) {
			holeCollider = null;
		}
	}

	private void FixedUpdate() {
		if(Sunk) return;

		if(holeCollider != null && holeCollider.bounds.Contains(transform.position)) {
			Vector2 diff = holeCollider.transform.position - transform.position;
			float mag = diff.magnitude;
			spriteRenderer.color = Color.Lerp(Color.black, Color.white, mag / 0.5f);
			if (mag < 0.25f) {
				if(mag > Speed * Time.fixedDeltaTime) {
					transform.Translate(Speed * Time.fixedDeltaTime * diff.normalized);
				}
				else {
					SetSunk();			
				}
				return;
			}
			else {
				Vector2 inboundVelocity = new Vector2(Direction.x, Direction.y) * Speed;
				Vector2 velocityFromHole = diff.normalized * (0.5f / diff.magnitude);

				Speed += Vector2.Dot(Direction, diff.normalized) * velocityFromHole.magnitude * Time.fixedDeltaTime;
				Direction = (inboundVelocity + velocityFromHole).normalized;
			}	
		}

		RaycastHit2D ballHit = Physics2D.CircleCastAll(transform.position, BallRadius, Direction, Speed * Time.fixedDeltaTime, collidableLayermask).FirstOrDefault();
		if(ballHit.collider != null) {
			ballHit.collider.GetComponent<IActionOnBallEnter>()?.DoAction(this);

			Vector2 normal = ballHit.normal;
			float dot = Vector2.Dot(Direction, normal);

			if(dot < 0) {
				Direction = (Direction - 2 * dot * normal).normalized;
				transform.position = new Vector3(ballHit.centroid.x, ballHit.centroid.y, transform.position.z);
			}
			else {
				transform.Translate(Velocity * Time.fixedDeltaTime);
				Speed = Mathf.Max(0, Speed - 1 * Time.fixedDeltaTime);
			}
		}
		else {
			transform.Translate(Velocity * Time.fixedDeltaTime);
			Speed = Mathf.Max(0, Speed - 1 * Time.fixedDeltaTime);
		}
		
		if(Speed < 0.1f && player != null && player.Frozen) {
			TransitionCamera();
		}
	}

	private void TransitionCamera() {
		CameraController c = Camera.main.GetComponent<CameraController>();
		if (!c.FollowingTransition && c.Following != (player as CameraFollowable)) {
			c.SetFollowing(player, () => {
				player.Frozen = false;
				player = null;
				if(Sunk) {
					Destroy(this.gameObject);
				}
			});
		}
	}

	private void SetSunk() {
		Sunk = true;
		transform.position = holeCollider.transform.position;
		Speed = 0;
		spriteRenderer.color = Color.black;
		circleCollider.enabled = false;
		TransitionCamera();

		// TODO: tell level manager what's up
	}

	private void Update() {
		
	}
}
