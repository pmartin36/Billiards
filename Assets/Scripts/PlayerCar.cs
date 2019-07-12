using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCar : Car
{
	private InputPackage lastInput;

	protected override void Start() {
		base.Start();
		GasApplied = false;

		GameManager.Instance.LevelManager.Player = this;
		lastInput = new InputPackage();
	}

	protected override void FixedUpdate() {
        base.FixedUpdate();
		
    }

	public void HandleInput(InputPackage p) {
		Debug.Log(p.GasAmount);
		lastInput = p;
		GasApplied = Mathf.Abs(p.GasAmount) > 0 && !p.Break;
		BreakApplied = p.Break || p.GasAmount * velocity.x < -0.5f;
	}

	protected override float GetHorizontalMoveDelta() {
		if(BreakApplied) {
			return 0;
		}
		else {
			if (lastInput.GasAmount > 0) {
				return (velocity.x < 1f ? 10f : 5f) * lastInput.GasAmount;
			}
			else {
				return (velocity.x > -1f ? 6f : 3f) * lastInput.GasAmount;
			}
		}
	}
}
