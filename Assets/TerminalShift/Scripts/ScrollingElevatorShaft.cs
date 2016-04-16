using UnityEngine;
using System.Collections.Generic;
using System.Text;

public class ScrollingElevatorShaft : MonoBehaviour
{
	public GameObject[] ShaftSegmentPrefabs = null;

	public float ShaftSegmentHeight = 10.0f;

	public float RequiredShaftTopY = 30.0f;
	public float RequiredShaftBottomY = -30.0f;

	public bool DebugEnabled = false;

	public void AdvanceShaft(
		float movementDistance)
	{
		// Advance all the existing shaft segments.
		foreach (GameObject shaftSegment in currentShaftSegments)
		{
			Vector3 localShaftPosition = shaftSegment.transform.localPosition;

			localShaftPosition.y += movementDistance;

			shaftSegment.transform.localPosition = localShaftPosition;
		}

		// Retire any shaft segments that have moved out of range.
		for (
			int index = 0;
			index < currentShaftSegments.Count;
			/* internal increment */)
		{
			float segmentBottom = (currentShaftSegments[index].transform.localPosition.y - (ShaftSegmentHeight / 2.0f));

			if (segmentBottom > RequiredShaftTopY)
			{
				GameObject.Destroy(currentShaftSegments[index]);

				currentShaftSegments.RemoveAt(index);
			}
			else
			{
				index++;
			}
		}

		// Create new shaft segments.
		{
			float lowestLocalPositionY = (RequiredShaftTopY + (ShaftSegmentHeight / 2.0f));
			
			foreach (GameObject shaftSegment in currentShaftSegments)
			{
				if (shaftSegment.transform.localPosition.y < lowestLocalPositionY)
				{
					lowestLocalPositionY = shaftSegment.transform.localPosition.y;
				}
			}

			while (lowestLocalPositionY > (RequiredShaftBottomY - (ShaftSegmentHeight / 2.0f)))
			{
				lowestLocalPositionY -= ShaftSegmentHeight;

				GameObject randomSegmentPrefab = ShaftSegmentPrefabs[segmentRandomizer.Next(ShaftSegmentPrefabs.Length)];
				
				GameObject newShaftSegment =
					Instantiate(
						randomSegmentPrefab,
						new Vector3(0.0f, lowestLocalPositionY, 0.0f),
						Quaternion.identity) as GameObject;

				newShaftSegment.transform.parent = transform;

				currentShaftSegments.Add(newShaftSegment);
			}
		}
	}

	private List<GameObject> currentShaftSegments = new List<GameObject>();

	private System.Random segmentRandomizer = new System.Random();
}

