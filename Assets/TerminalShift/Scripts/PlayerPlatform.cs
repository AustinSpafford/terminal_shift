using UnityEngine;
using System.Collections.Generic;
using System.Linq;
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

	[System.Serializable]
	public struct PlatformShapeBinding
	{
		public PlatformShape ShapeType;
		public GameObject ShapePrefab;
	}

	public float CurrentAcceleration = 0.0f;
	public float CurrentFallSpeed = 0.0f;
	public float TerminalFallSpeed = 50.0f;
	public float LethalFallSpeed = 10.0f;
	
	public float PlatformMorphDurationSeconds = 1.0f;

	public float PlatformRotationDurationSeconds = 1.0f;

	public float DistanceToPlatformBottom = 1.0f; // TODO: Refactor into a separate object/component.

	public PlatformShapeBinding[] ShapeBindings = null;
	
	// TODO: Refactor the morphing platform into a separate object/component.
	public PlatformShape PlatformMorphDesiredShape = PlatformShape.LetterX;
	public PlatformShape PlatformMorphStartShape = PlatformShape.LetterX;
	public PlatformShape PlatformMorphEndShape = PlatformShape.LetterX;
	public float PlatformMorphFraction = 1.0f;
	
	public Quaternion PlatformRotationDesiredOrientation = Quaternion.identity;
	public float PlatformRotationAngularVelocity = 0.0f;

	public PlatformShape EffectivePlatformShape = PlatformShape.LetterX;

	public bool DebugEnabled = false;

	public void Awake()
	{
		scrollingElevatorShaft = FindObjectOfType<ScrollingElevatorShaft>();

		morphingPlatformRoot = new GameObject();
		morphingPlatformRoot.name = "morphing_platform_root";

		morphingPlatformRoot.transform.SetParent(
			transform,
			worldPositionStays: false);

		UpdatePlatformMorphCreatedPlatformObjects();
		
		CurrentAcceleration = Physics.gravity.magnitude;
	}

	public void Update()
	{
		UpdatePlatformMorph();

		UpdatePlatformRotation();

		InteractWithNearestObstacle();

		CurrentFallSpeed = 
			Mathf.Min(
				(CurrentFallSpeed + (CurrentAcceleration * Time.deltaTime)),
				TerminalFallSpeed);

		scrollingElevatorShaft.AdvanceShaft(CurrentFallSpeed * Time.deltaTime);
	}

	private GameObject morphingPlatformRoot = null;
	private GameObject currentMorphStartObject = null;
	private GameObject currentMorphEndObject = null;

	private ScrollingElevatorShaft scrollingElevatorShaft = null;

	private FloorObstacle nextObstacle = null;

	private bool letterLAxisWasPressed = false;
	private bool letterRAxisWasPressed = false;

	private FloorObstacle FindNextObstacle()
	{
		FloorObstacle nearestObstacle = null;

		foreach (ScrollingElevatorShaft.ShaftSegmentInstance segmentInstance in scrollingElevatorShaft.SegmentInstances)
		{
			if (segmentInstance.SourcePrefab.AnalyzedSegmentContainsObstacle)
			{
				foreach (FloorObstacle candidateObstacle in segmentInstance.Instance.GetComponentsInChildren<FloorObstacle>())
				{
					if (candidateObstacle.transform.position.y < (transform.position.y - DistanceToPlatformBottom))
					{
						if ((nearestObstacle == null) ||
							(candidateObstacle.transform.position.y > nearestObstacle.transform.position.y))
						{
							nearestObstacle = candidateObstacle;
						}
					}
				}
			}
		}

		return nearestObstacle;
	}

	private PlatformShapeBinding GetShapeBinding(
		PlatformShape platformShape)
	{
		return ShapeBindings
			.Where(element => (element.ShapeType == platformShape))
			.FirstOrDefault();
	}
	
	private void InteractWithNearestObstacle()
	{
		if (nextObstacle == null)
		{
			nextObstacle = FindNextObstacle();
		}

		if (nextObstacle != null)
		{
			float signedDistanceToNextObstacle =
				((transform.position.y - DistanceToPlatformBottom) - nextObstacle.transform.position.y);

			if (signedDistanceToNextObstacle <= 0.0f)
			{
				if (IsAbleToPassThroughNextObstacle())
				{
					if (DebugEnabled)
					{
						Debug.LogFormat("Passed an obstacle at {0} mps!", CurrentFallSpeed);
					}

					CurrentAcceleration = Physics.gravity.magnitude;

					nextObstacle = null;
				}
				else
				{
					bool impactWasLethal = (CurrentFallSpeed > LethalFallSpeed);

					// If we penetrated the obstacle all, back up to where we're resting on the surface.
					if (signedDistanceToNextObstacle < -0.01f)
					{
						if (DebugEnabled)
						{
							Debug.LogFormat(
								"Backing up the shaft {0} meters.", 
								(-1 * signedDistanceToNextObstacle));
						}

						scrollingElevatorShaft.AdvanceShaft(signedDistanceToNextObstacle);
					}

					CurrentAcceleration = 0.0f;
					CurrentFallSpeed = 0.0f;
				}
			}
		}
	}

	private bool IsAbleToPassThroughNextObstacle()
	{
		bool result = false;

		if (nextObstacle == null)
		{
			result = true;
		}
		else if (EffectivePlatformShape == nextObstacle.RequiredPlatformShape)
		{
			float platformRotationRemainingDegrees =
				Quaternion.Angle(morphingPlatformRoot.transform.rotation, PlatformRotationDesiredOrientation);

			bool rotationHasFinished = (platformRotationRemainingDegrees < 10.0f);

			if (rotationHasFinished)
			{
				if (nextObstacle.AllOrientationsAccepted)
				{
					result = true;
				}
				else
				{
					float platformDegreesFromObstacleOrientation =
						Quaternion.Angle(morphingPlatformRoot.transform.rotation, nextObstacle.transform.rotation);

					bool platformMatchesObstacleOrientation =
						(platformDegreesFromObstacleOrientation < 10.0f);

					if (platformMatchesObstacleOrientation)
					{
						result = true;
					}
				}
			}
		}

		return result;
	}

	private void UpdatePlatformMorph()
	{
		UpdatePlatformMorphDesiredShape();

		if (PlatformMorphDesiredShape != PlatformMorphEndShape)
		{
			UpdatePlatformMorphCreatedPlatformObjects();
		}

		PlatformMorphFraction = 
			Mathf.Clamp01(
				PlatformMorphFraction + 
				(Time.deltaTime / PlatformMorphDurationSeconds));

		// If we've finally finished a morph, set it as our new gameplay-shape.
		if (PlatformMorphFraction >= (1.0f - Mathf.Epsilon))
		{
			EffectivePlatformShape = PlatformMorphEndShape;
			
			if (currentMorphStartObject != null)
			{
				GameObject.Destroy(currentMorphStartObject);
			}
		}
		
		UpdateDisplayedPlatformMorph();
	}

	private void UpdatePlatformMorphCreatedPlatformObjects()
	{
		if (currentMorphStartObject != null)
		{
			GameObject.Destroy(currentMorphStartObject);
		}
			
		if (currentMorphEndObject != null)
		{
			GameObject.Destroy(currentMorphEndObject);
		}

		PlatformMorphStartShape = PlatformMorphEndShape;
		PlatformMorphEndShape = PlatformMorphDesiredShape;
		PlatformMorphFraction = 0.0f;
			
		PlatformShapeBinding startShapeBinding = GetShapeBinding(PlatformMorphStartShape);
		PlatformShapeBinding endShapeBinding = GetShapeBinding(PlatformMorphEndShape);

		if (startShapeBinding.ShapePrefab != null)
		{
			currentMorphStartObject = GameObject.Instantiate(startShapeBinding.ShapePrefab);

			currentMorphStartObject.transform.SetParent(
				morphingPlatformRoot.transform,
				worldPositionStays: false);
		}
			
		if (endShapeBinding.ShapePrefab != null)
		{
			currentMorphEndObject = GameObject.Instantiate(endShapeBinding.ShapePrefab);

			currentMorphEndObject.transform.SetParent(
				morphingPlatformRoot.transform,
				worldPositionStays: false);
		}
	}

	private void UpdateDisplayedPlatformMorph()
	{
		if (currentMorphStartObject != null)
		{
			currentMorphStartObject.transform.localScale = 
				((1.0f - PlatformMorphFraction) * Vector3.one);
		}
		
		if (currentMorphEndObject != null)
		{
			currentMorphEndObject.transform.localScale = 
				(PlatformMorphFraction * Vector3.one);
		}
	}

	private void UpdatePlatformMorphDesiredShape()
	{
		bool letterLIsPressed = (Input.GetAxis("PlatformShapeL") > 0.5f);
		bool letterRIsPressed = (Input.GetAxis("PlatformShapeR") > 0.5f);

		if (Input.GetButtonDown("PlatformShapeA"))
		{
			PlatformMorphDesiredShape = PlatformShape.LetterA;
		}
		else if (Input.GetButtonDown("PlatformShapeB"))
		{
			PlatformMorphDesiredShape = PlatformShape.LetterB;
		}
		else if (letterLIsPressed && (letterLAxisWasPressed == false))
		{
			PlatformMorphDesiredShape = PlatformShape.LetterL;
		}
		else if (letterRIsPressed && (letterRAxisWasPressed == false))
		{
			PlatformMorphDesiredShape = PlatformShape.LetterR;
		}
		else if (Input.GetButtonDown("PlatformShapeX"))
		{
			PlatformMorphDesiredShape = PlatformShape.LetterX;
		}
		else if (Input.GetButtonDown("PlatformShapeY"))
		{
			PlatformMorphDesiredShape = PlatformShape.LetterY;
		}

		letterLAxisWasPressed = letterLIsPressed;
		letterRAxisWasPressed = letterRIsPressed;
	}
	
	private void UpdatePlatformRotation()
	{
		UpdatePlatformRotationDesiredOrientation();
		
		UpdatePlatformRotationCurrentOrientation();
	}

	private void UpdatePlatformRotationCurrentOrientation()
	{
		float currentDegreesFromTarget =
			Quaternion.Angle(
				morphingPlatformRoot.transform.rotation,
				PlatformRotationDesiredOrientation);
		
		if (currentDegreesFromTarget > Mathf.Epsilon)
		{
			float newDegreesFromTarget =
				Mathf.SmoothDampAngle(
					currentDegreesFromTarget,
					0.0f, // targetAngle
					ref PlatformRotationAngularVelocity,
					PlatformRotationDurationSeconds);
		
			morphingPlatformRoot.transform.rotation =
				Quaternion.Slerp(
					PlatformRotationDesiredOrientation,
					morphingPlatformRoot.transform.rotation,
					(newDegreesFromTarget / currentDegreesFromTarget));
		}
	}

	private void UpdatePlatformRotationDesiredOrientation()
	{
		if (Input.GetButtonDown("PlatformRotateClockwise"))
		{
			PlatformRotationDesiredOrientation =
				(Quaternion.Euler(0, 90, 0) * PlatformRotationDesiredOrientation);
		}
		else if (Input.GetButtonDown("PlatformRotateCounterclockwise"))
		{
			PlatformRotationDesiredOrientation =
				(Quaternion.Euler(0, -90, 0) * PlatformRotationDesiredOrientation);
		}
	}
}

