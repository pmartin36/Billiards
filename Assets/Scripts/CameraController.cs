using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
	public CameraFollowable Following = null;
	public bool FollowingTransition = false;
	private Vector3 Position { set => transform.position = new Vector3(value.x, value.y, -10); }

	private Camera cam;

    void Start() {
		GameManager.Instance.LevelManager.PlacementModeChange += PlacementModeChange;
		cam = GetComponent<Camera>();
    }

    void Update() {
        if(Following != null && !FollowingTransition) {
			Position = Following.Position;
		}
    }

	private void PlacementModeChange(object sender, bool active) {
		LevelManager lm = sender as LevelManager;
		this.Following = lm.Player.GetComponent<CameraFollowable>();
		cam.orthographicSize = 5f;
		Position = Following.Position;
	}

	public void SetFollowing(CameraFollowable f, System.Action completedTransitionCallback) {
		if(Following != null && f != Following) {
			StartCoroutine(TransitionFollowing(f, completedTransitionCallback));
		}
		else if (f != Following) {
			Following = f;
			completedTransitionCallback?.Invoke();
		}
	}

	private IEnumerator TransitionFollowing(CameraFollowable newFollowable, System.Action completedTransitionCallback) {
		float time = 0;
		FollowingTransition = true;
		while (time <= 1f) {
			Position = Vector3.Lerp(Following.Position, newFollowable.Position, Mathf.SmoothStep(0f, 1f, time+=Time.deltaTime));
			yield return null;
		}
		FollowingTransition = false;
		Following = newFollowable;
		completedTransitionCallback?.Invoke();	
	}
}

public interface CameraFollowable {
	Vector2 Position { get; }
	Vector2 Velocity { get; }
}
