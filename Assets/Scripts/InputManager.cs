﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputManager : MonoBehaviour
{
	private Camera main;
	private LayerMask groundMask;

	private void Start() {
		main = Camera.main;
	}

	private void Update() {
		InputPackage p = new InputPackage();
		p.MousePositionScreenSpace = Input.mousePosition;
		p.MousePositionWorldSpace = (Vector2)main.ScreenToWorldPoint(Input.mousePosition);

		p.MouseWheelDelta = Input.mouseScrollDelta.y;
		p.LeftMouse = Input.GetButton("LeftMouse");
		p.RightMouse = Input.GetButton("RightMouse");

		p.Shift = Input.GetKey(KeyCode.LeftShift);
		p.Enter = Input.GetButtonDown("Enter");

		p.GasAmount = Input.GetAxis("Gas");
		p.Break = Input.GetButton("Break");

		p.Dash = Input.GetButtonDown("Dash");
		p.Jump = Input.GetButton("Jump");

		p.Horizontal = Input.GetAxis("Horizontal");
		p.Vertical = Input.GetAxis("Vertical");

		GameManager.Instance.ContextManager.HandleInput(p);
	}
}

public class InputPackage {
	public Vector3 MousePositionScreenSpace { get; set; }
	public Vector3 MousePositionWorldSpace { get; set; }

	public float MouseWheelDelta { get; set; }
	public bool LeftMouse { get; set; }
	public bool RightMouse { get; set; }
	public bool Shift { get; set; }

	public bool Enter { get; set; }
	public float GasAmount { get; set; }
	public bool Break { get; set; }
	public bool Dash { get; set; }
	public bool Jump { get; set; }
	public float Horizontal { get; set; }
	public float Vertical { get; set; }
}
