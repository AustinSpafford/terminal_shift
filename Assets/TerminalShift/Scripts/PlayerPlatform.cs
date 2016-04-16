using UnityEngine;
using System.Collections.Generic;
using System.Text;

public class PlayerPlatform : MonoBehaviour
{
	public enum PlatformShape
	{
		LetterA,
		LetterB,
		LetterL,
		LetterR,
		LetterX,
		LetterY,
	}

	public float CurrentSpeed = 0.0f;
	public float TerminalSpeed = 50.0f;
	
	public PlatformShape PlatformMorphStartShape = PlatformShape.LetterX;
	public PlatformShape PlatformMorphEndShape = PlatformShape.LetterX;
	public float PlatformMorphFraction = 1.0f;

	public PlatformShape EffectivePlatformShape = PlatformShape.LetterX;

	public void Awake()
	{
		scrollingElevatorShaft = FindObjectOfType<ScrollingElevatorShaft>();
	}

	public void Update()
	{
		UpdatePlatformMorphEndShape();

		CurrentSpeed = 
			Mathf.Min(
				(CurrentSpeed + (Physics.gravity.magnitude * Time.deltaTime)),
				TerminalSpeed);

		scrollingElevatorShaft.AdvanceShaft(CurrentSpeed * Time.deltaTime);
	}

	private ScrollingElevatorShaft scrollingElevatorShaft = null;

	private bool letterLAxisWasPressed = false;
	private bool letterRAxisWasPressed = false;

	private void UpdatePlatformMorphEndShape()
	{
		bool letterLIsPressed = (Input.GetAxis("PlatformShapeL") > 0.5f);
		bool letterRIsPressed = (Input.GetAxis("PlatformShapeR") > 0.5f);

		if (Input.GetButtonDown("PlatformShapeA"))
		{
			PlatformMorphEndShape = PlatformShape.LetterA;
		}
		else if (Input.GetButtonDown("PlatformShapeB"))
		{
			PlatformMorphEndShape = PlatformShape.LetterB;
		}
		else if (letterLIsPressed && (letterLAxisWasPressed == false))
		{
			PlatformMorphEndShape = PlatformShape.LetterL;
		}
		else if (letterRIsPressed && (letterRAxisWasPressed == false))
		{
			PlatformMorphEndShape = PlatformShape.LetterR;
		}
		else if (Input.GetButtonDown("PlatformShapeX"))
		{
			PlatformMorphEndShape = PlatformShape.LetterX;
		}
		else if (Input.GetButtonDown("PlatformShapeY"))
		{
			PlatformMorphEndShape = PlatformShape.LetterY;
		}

		letterLAxisWasPressed = letterLIsPressed;
		letterRAxisWasPressed = letterRIsPressed;
	}
}

