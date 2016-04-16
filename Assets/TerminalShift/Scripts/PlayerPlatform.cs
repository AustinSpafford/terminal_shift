using UnityEngine;
using System.Collections.Generic;
using System.Text;

public class PlayerPlatform : MonoBehaviour
{
	public float CurrentSpeed = 0.0f;
	public float TerminalSpeed = 50.0f;

	public void Awake()
	{
		scrollingElevatorShaft = FindObjectOfType<ScrollingElevatorShaft>();
	}

	public void Update()
	{
		CurrentSpeed = 
			Mathf.Min(
				(CurrentSpeed + (Physics.gravity.magnitude * Time.deltaTime)),
				TerminalSpeed);

		scrollingElevatorShaft.AdvanceShaft(CurrentSpeed * Time.deltaTime);
	}

	private ScrollingElevatorShaft scrollingElevatorShaft = null;
}

