﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wheel : MonoBehaviour
{
	private Car parent;
	private CircleCollider2D circ;
	private LayerMask layerMask;
	private SpriteRenderer spriteRenderer;

	public float Radius { get => circ.radius * transform.lossyScale.x; }

	private bool _isTouchingGround;
	public bool IsTouchingGround {
		get => _isTouchingGround;
		set {
			spriteRenderer.color = value ? Color.green : Color.red;
			_isTouchingGround = value;
		}
	}

	public float vGravity { get; set; }

    void Start() {
		parent = GetComponentInParent<Car>();
		circ = GetComponent<CircleCollider2D>();
		layerMask = 1 << LayerMask.NameToLayer("Level");
		spriteRenderer = GetComponent<SpriteRenderer>();
    }

	public void AddGravity(float deltaGravity) {
		if(!IsTouchingGround) {
			vGravity += deltaGravity;
		}
	}

	public void UpdateIsTouchingGround() {
		Collider2D hit = Physics2D.OverlapCircle(transform.position, Radius, layerMask);
		if(hit != null) {
			IsTouchingGround = true;
			vGravity = 0;
		}
		else {
			IsTouchingGround = false;
			
		}
	}

	public RaycastHit2D Cast(Vector2 dir, float length, float skinWidth = 0.015f) {
		// 0.03 - skinWidth * 2
		var hit = Physics2D.CircleCast(transform.position, Radius - skinWidth, dir, length + skinWidth, layerMask);
		return hit;
	}
}
