﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent (typeof(InputManager))]
public class LevelManager : ContextManager
{
	private HexGrid _hexGrid;
	public HexGrid Grid {
		get => _hexGrid;
		set {
			_hexGrid = value;
		}
	}

	public LevelData LevelData;
	public LevelType LevelType;

	public bool InPlacementMode = true;
	public event EventHandler<bool> PlacementModeChange;  // TODO: Maybe better to just tell the camera to change position

	// For Placement Mode
	private Tower Tower;

	public Line LinePrefab;
	private Line Line;

	private Camera main;
	private InputPackage lastInput;
	private HexInfo lastHoverHex;

	private float maxCameraSize = 20f;
	private float minCameraSize = 5f;
	private float cameraSizeDiff { get => maxCameraSize - minCameraSize; }

	private bool dragging = false;
	private bool lineCreationInProgress = false;

	// For Play Mode
	public Player playerPrefab;
	public PlayerCar Player { get; set; }
	public Tower StartTower { get => Grid.StartingPoint.TowerHead; }

	// Win Conditions
	public int Depth { get; set; }
	public WinCondition WinCondition { get; private set; }

	// Canvas
	public ScreenSpaceCanvas Canvas;

	public override void Awake() {
		base.Awake();
		main = Camera.main;
		Canvas = GameObject.FindObjectOfType<ScreenSpaceCanvas>();
		HexGrid.GridGenerated += GridGenerated;

		Grid = GameObject.FindObjectOfType<HexGrid>();
		if(LevelData != null) {
			Grid.Init(LevelData, LinePrefab);
		}
		else {
			Grid.Init();
		}

		switch (LevelType) {
			case LevelType.Switches:			
				Switch.SwitchActivated += SwitchActivated;

				WinCondition = new SwitchWinCondition(Depth);
				if (LevelData == null) {
					Grid.CreateHolesAndBalls(WinCondition.Total);
				}
				break;
			case LevelType.Tiles:
				EmptyCell.TileFlipped += TileFlipped;

				WinCondition = new TileFlipCondition(Grid.GetNumReachableEmptyCells(), Depth);
				break;
			case LevelType.Kills:
				// TODO: Add event for enemy dying and link to EnemyKilled

				WinCondition = new EnemyWinCondition(Depth);
				break;
			default:
				break;
		}

		SetCameraPosition(Grid.StartingPoint.PhysicalCoordinates);
	}

	public override void Start()
    {
		Tower = Tower.CreateInstance();
		Tower.transform.localScale = Vector3.one * Grid.OuterRadius * Tower.transform.localScale.x / 2f;

		// Canvas.InitEnergySlider(GameManager.Instance.PlayerData.MaxEnergy);
    }

	public void GridGenerated(object sender, HexGrid grid) {
		this.Grid = grid;
		// start the game
		// Player = Instantiate(playerPrefab, grid.StartingPoint.PhysicalCoordinates, Quaternion.identity);
		// Player.Init(grid.StartingPoint.TowerHead, GameManager.Instance.PlayerData);
	}

	private void SetCameraPosition(Vector3 position) {
		Bounds clamp = Grid.PaddedGridDimensions;

		float x = position.x;
		float y = position.y;
		// left side of camera further right than clamp
		if(clamp.max.x < x) {
			x = clamp.max.x ;
		}
		else if(clamp.min.x > x) {
			x = clamp.min.x ;
		}

		// bottom side of camera further up than clamp
		if (clamp.max.y < y) {
			y = clamp.max.y ;
		}
		else if(clamp.min.y > y) {
			y = clamp.min.y ;
		}

		main.transform.position = new Vector3(x, y, -10);
	}

	private void LeavePlacementMode() {
		InPlacementMode = false;
		GameObject.Destroy(Tower.gameObject);
		PlacementModeChange?.Invoke(this, InPlacementMode);

		Tower t = Grid.StartingPoint.TowerHead;
		t.Place(Grid);
		// Player.BeginLevel(t);
	}

	public override void HandleInput(InputPackage p) {
		// Common Input

		//if(InPlacementMode) {
		//	if (p.Enter) {
		//		LeavePlacementMode();
		//	}
		//	else {
		//		PlacementModeInput(p);
		//	}
		//}
		//else {
		//	PlayModeInput(p);
		//}	

		PlayModeInput(p);
	}

	public void PlayModeInput(InputPackage p) {
		Player?.HandleInput(p);
	}

	public void PlacementModeInput(InputPackage p) {
		if (Mathf.Abs(p.MouseWheelDelta) > 0.2f) {
			float camSize = main.orthographicSize - p.MouseWheelDelta;
			float prevCamSize = main.orthographicSize;
			main.orthographicSize = Mathf.Clamp(camSize, minCameraSize, maxCameraSize);

			// mouse pointer world space should not change when zooming
			Vector3 pointerPositionAfterCamResize = main.ScreenToWorldPoint(p.MousePositionScreenSpace);
			SetCameraPosition(main.transform.position + (p.MousePositionWorldSpace - pointerPositionAfterCamResize));
		}

		Vector2 vpSpace = main.WorldToViewportPoint(p.MousePositionWorldSpace);
		bool mouseOnScreen = vpSpace.x < 1 && vpSpace.x >= 0 && vpSpace.y < 1 && vpSpace.y >= 0;

		if (!lineCreationInProgress && !p.RightMouse) {
			Grid.TryGetTowerLocation(p.MousePositionWorldSpace, Tower);
		}

		HexInfo h = Grid.TryGetCellInfoFromWorldPosition(p.MousePositionWorldSpace, out bool mouseOverCellSuccess);
		// left mousedown
		if (lastInput != null && !lastInput.LeftMouse && p.LeftMouse) {
			if(p.Shift) {
				if (mouseOverCellSuccess) {
					if (h.TowerHead != null) {
						lineCreationInProgress = true;
						Tower.gameObject.SetActive(false);
						Line = Instantiate(LinePrefab, h.PhysicalCoordinates, Quaternion.identity);
						Line.gameObject.SetActive(true);
						Line.Init(Grid, p.MousePositionWorldSpace);
					}
				}
			}
		}
		// left mousehold
		else if (lastInput != null && lastInput.LeftMouse && p.LeftMouse) {
			// dragging camera
			if(lineCreationInProgress) {
				// Update line with position
				Line.Update(p.MousePositionWorldSpace);
			}
			else {
				var diff = (p.MousePositionScreenSpace - lastInput.MousePositionScreenSpace) * Time.deltaTime *
					Mathf.Lerp(0.5f, 1.4f, (main.orthographicSize - minCameraSize) / cameraSizeDiff) * Settings.ScrollSpeed;
				Vector3 newPosition = main.transform.position - diff;
				SetCameraPosition(newPosition);

				if (diff.sqrMagnitude > 0.01f) {
					dragging = true;
				}
			}
		}
		// left mouseup
		else if (lastInput != null && lastInput.LeftMouse && !p.LeftMouse) {
			if(lineCreationInProgress) {
				// verify final spot is actually tower head
				// if so, create line
				bool success = mouseOverCellSuccess && Line.IsValidPlacement(h);
				if (success) {
					Line.Place(h);
					Debug.Log("Line placed");
				}
				else {
					Destroy(Line.gameObject);
					Debug.Log("Line not placed");
				}
				lineCreationInProgress = false;
				Tower.gameObject.SetActive(true);
			}
			else {
				// place
				if (!dragging && mouseOnScreen && Tower.gameObject.activeInHierarchy) {
					// lock this tower to the grid space
					Tower.Place(Grid);
					// create new tower for placement mode (the one we drag around that's transparent)
					Tower = Tower.CreateInstance();
					Tower.transform.localScale = Vector3.one * Grid.OuterRadius * Tower.transform.localScale.x / 2f;
				}				
			}
			dragging = false;
		}

		// right mouse down
		if ((lastInput == null || !lastInput.RightMouse) && p.RightMouse) {
			if(mouseOverCellSuccess) {
				h.ToggleMarkerStatus();
			}
		}
		// right mouse hold
		else if (lastInput == null || lastInput.RightMouse && p.RightMouse) {
			if(lastHoverHex != null && mouseOverCellSuccess && lastHoverHex.Coordinates != h.Coordinates) {
				h.ToggleMarkerStatus();
			}
		}
		// right mouse up
		else if (!p.RightMouse && lineCreationInProgress) {
			
		}

		lastInput = p;
		lastHoverHex = mouseOverCellSuccess ? h : null;
	}

	public void SwitchActivated(object sender, EventArgs e) {
		Switch s = sender as Switch;
		WinCondition.IncrementCurrent();
		if(WinCondition.IsTargetReached) {
			(Grid.EndingPoint.HexGameObject as EndGate).Open();
		}
	}

	public void EnemyKilled(object sender, EventArgs e) {

	}

	public void TileFlipped(object sender, EventArgs e) {

	}

	public void SwitchLevel(int direction) {
		Canvas.BlackScreen(() => GameManager.Instance.ReloadLevel());
	}

	public void UpdateUIFromPlayer(float energy) {
		// Canvas.UpdateEnergySlider(energy);
	}

	private void OnDestroy() {
		HexGrid.GridGenerated -= GridGenerated;
		Switch.SwitchActivated -= SwitchActivated;
		EmptyCell.TileFlipped -= TileFlipped;
		PlacementModeChange = null;
	}
}

public enum LevelType {
	Switches,
	Tiles,
	Kills
}
