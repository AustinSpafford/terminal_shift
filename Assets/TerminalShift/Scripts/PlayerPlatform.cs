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

	public LoopingAudioBlender WindAudioBlender = null;
	public float WindMinFallSpeed = 1.0f;
	public float WindMinVolume = 0.2f;
	public float WindMaxFallSpeed = 20.0f;
	public float WindMaxVolume = 1.0f;

	public GameObject DeathCrunchAudioPrefab = null;
	public GameObject PlatformRotationAudioPrefab = null;
	public GameObject PlatformMorphAudioPrefab = null;
	
	public float PlatformMorphDurationSeconds = 1.0f;

	public float PlatformRotationDurationSeconds = 1.0f;

	public float DistanceToPlatformBottom = 1.0f; // TODO: Refactor into a separate object/component.

	public Color RestingFogColor = Color.black;
	public float RestingFogEndDistance = 7.0f;
	public float RestingFogFadeSeconds = 1.0f;
	public Color DeadFogColor = Color.red;
	public float DeadFogEndDistance = 7.0f;
	public float DeadFogFadeSeconds = 0.25f;
	public float FreefallFogFadeSeconds = 2.0f;

	public bool SnapToNextObstacleHeight = true;

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
		fogManipulator = FindObjectOfType<FogManipulator>();
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

		UpdateWindVolume();
	}

	private GameObject morphingPlatformRoot = null;
	private GameObject currentMorphStartObject = null;
	private GameObject currentMorphEndObject = null;
	
	private FogManipulator fogManipulator = null;
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

		if (SnapToNextObstacleHeight &&
			nextObstacle != null)
		{
			scrollingElevatorShaft.AdvanceShaft(
				((transform.position.y - DistanceToPlatformBottom) - nextObstacle.transform.position.y));
			
			SnapToNextObstacleHeight = false;
		}

		if (CurrentAcceleration > 0.0f)
		{
			if (nextObstacle != null)
			{
				float signedDistanceToNextObstacle =
					((transform.position.y - DistanceToPlatformBottom) - nextObstacle.transform.position.y);
				
				bool hasReachedObstacle = 
					(signedDistanceToNextObstacle < Mathf.Epsilon);

				if (hasReachedObstacle)
				{
					if (IsAbleToPassThroughNextObstacle())
					{
						if (DebugEnabled)
						{
							Debug.LogFormat("Passed an obstacle at {0} mps!", CurrentFallSpeed);
						}

						nextObstacle = null;
					}
					else
					{
						if (DebugEnabled)
						{
							Debug.LogFormat("Rammed an obstacle at {0} mps!", CurrentFallSpeed);
						}

						bool impactWasLethal = (CurrentFallSpeed > LethalFallSpeed);

						// Snap to the obstacle (mainly to back up after penetrating into it).
						scrollingElevatorShaft.AdvanceShaft(signedDistanceToNextObstacle);

						CurrentAcceleration = 0.0f;
						CurrentFallSpeed = 0.0f;

						if (impactWasLethal)
						{
							if (fogManipulator != null)
							{
								fogManipulator.StartFadeToFogOverride(
									DeadFogColor,
									0.0f, // startDistance
									DeadFogEndDistance,
									DeadFogFadeSeconds);
							}

							if (DeathCrunchAudioPrefab != null)
							{
								Vector3 mainCameraPosition = Camera.main.transform.position;

								// Play the clip at roughly waist-height.
								Vector3 deathAudioPosition = 
									new Vector3(
										mainCameraPosition.x,
										Mathf.Lerp(transform.position.y, mainCameraPosition.y, 0.5f),
										mainCameraPosition.z);
								
								GameObject deathAudio = 
									GameObject.Instantiate(
										DeathCrunchAudioPrefab,
										deathAudioPosition,
										transform.rotation) as GameObject;

								deathAudio.transform.SetParent(transform);
							}
						}
						else
						{
							if (fogManipulator != null)
							{
								fogManipulator.StartFadeToFogOverride(
									RestingFogColor,
									0.0f, // startDistance
									RestingFogEndDistance,
									RestingFogFadeSeconds);
							}
						}
					}
				}
			}
		}
		else
		{
			if ((nextObstacle == null) ||
				IsAbleToPassThroughNextObstacle())
			{
				if (DebugEnabled)
				{
					Debug.Log("Satisfied an obstacle while at rest, resuming descent.");
				}

				CurrentAcceleration = Physics.gravity.magnitude;

				if (fogManipulator != null)
				{
					fogManipulator.StartFadeToOriginalFog(
						FreefallFogFadeSeconds);
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

			bool rotationHasFinished = (platformRotationRemainingDegrees < 15.0f);

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
						(platformDegreesFromObstacleOrientation < 15.0f);

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
		
		PlatformShape? newPlatformShape = null;

		if (Input.GetButtonDown("PlatformShapeA"))
		{
			newPlatformShape = PlatformShape.LetterA;
		}
		else if (Input.GetButtonDown("PlatformShapeB"))
		{
			newPlatformShape = PlatformShape.LetterB;
		}
		else if ((letterLIsPressed && (letterLAxisWasPressed == false)))
		{
			newPlatformShape = PlatformShape.LetterL;
		}
		else if ((letterRIsPressed && (letterRAxisWasPressed == false)))
		{
			newPlatformShape = PlatformShape.LetterR;
		}
		else if (Input.GetButtonDown("PlatformShapeX"))
		{
			newPlatformShape = PlatformShape.LetterX;
		}
		else if (Input.GetButtonDown("PlatformShapeY"))
		{
			newPlatformShape = PlatformShape.LetterY;
		}

		letterLAxisWasPressed = letterLIsPressed;
		letterRAxisWasPressed = letterRIsPressed;

		// Validate that the desired shape actually exists (has artwork).
		if (newPlatformShape.HasValue &&
			(GetShapeBinding(newPlatformShape.Value).ShapePrefab == null))
		{
			newPlatformShape = null;
		}

		if (newPlatformShape.HasValue)
		{
			PlatformMorphDesiredShape = newPlatformShape.Value;
		}
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
				Mathf.SmoothDamp(
					currentDegreesFromTarget,
					0.0f, // target
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

	private void UpdateWindVolume()
	{
		if (WindAudioBlender != null)
		{
			float windFallSpeedFraction =
				Mathf.Clamp01(Mathf.InverseLerp(
					WindMinFallSpeed,
					WindMaxFallSpeed,
					CurrentFallSpeed));

			WindAudioBlender.VolumeFraction =
				Mathf.Lerp(
					WindMinVolume,
					WindMaxVolume,
					windFallSpeedFraction);
		}
	}
}

