using UnityEngine;
using System.Collections.Generic;
using System.Text;

public class ScrollingElevatorShaft : MonoBehaviour
{
	[System.Serializable]
	public class ShaftSegmentPrefab
	{
		public GameObject Prefab;
		public Bounds AnalyzedSegmentLocalBounds;
		public bool AnalyzedSegmentContainsObstacle;
	}

	public ShaftSegmentPrefab[] SegmentPrefabs = null;

	public float RequiredShaftTopY = 30.0f;
	public float RequiredShaftBottomY = -30.0f;

	public bool DebugEnabled = false;
	
	public void Awake()
	{
		UpdateShaftSegmentPrefabs();
	}

	public void AdvanceShaft(
		float movementDistance)
	{
		// Advance all the existing shaft segments.
		foreach (ShaftSegmentInstance segmentInstance in segmentInstances)
		{
			Vector3 localShaftPosition = segmentInstance.Instance.transform.localPosition;

			localShaftPosition.y += movementDistance;

			segmentInstance.Instance.transform.localPosition = localShaftPosition;
		}

		// Retire any shaft segments that have moved out of range.
		for (
			int index = 0;
			index < segmentInstances.Count;
			/* internal increment */)
		{
			ShaftSegmentInstance segmentInstance = segmentInstances[index];

			float segmentBottom = (
				segmentInstance.Instance.transform.localPosition.y +
				segmentInstance.SourcePrefab.AnalyzedSegmentLocalBounds.min.y);

			if (segmentBottom > RequiredShaftTopY)
			{
				GameObject.Destroy(segmentInstance.Instance);

				segmentInstances.RemoveAt(index);
			}
			else
			{
				index++;
			}
		}

		// Create new shaft segments.
		{
			Bounds shaftBounds = new Bounds();
			
			foreach (ShaftSegmentInstance segmentInstance in segmentInstances)
			{
				Bounds segmentBounds = segmentInstance.SourcePrefab.AnalyzedSegmentLocalBounds;
				segmentBounds.center += segmentInstance.Instance.transform.localPosition;

				shaftBounds.Encapsulate(segmentBounds);
			}

			while (shaftBounds.min.y > RequiredShaftBottomY)
			{
				ShaftSegmentPrefab randomSegmentPrefab = 
					SegmentPrefabs[segmentRandomizer.Next(SegmentPrefabs.Length)];

				Quaternion randomSegmentRotation =
					Quaternion.Euler(
						0.0f,
						(90.0f * segmentRandomizer.Next(4)),
						0.0f);

				float newSegmentPositionY = (
					shaftBounds.min.y -
					randomSegmentPrefab.AnalyzedSegmentLocalBounds.max.y);
				
				GameObject newShaftSegment =
					Instantiate(randomSegmentPrefab.Prefab) as GameObject;

				newShaftSegment.transform.SetParent(
					transform,
					worldPositionStays: false);
				
				newShaftSegment.transform.localPosition =
						new Vector3(0.0f, newSegmentPositionY, 0.0f);
				
				newShaftSegment.transform.localRotation =
					(randomSegmentRotation * newShaftSegment.transform.localRotation);

				ShaftSegmentInstance segmentInstance = new ShaftSegmentInstance()
				{
					Instance = newShaftSegment,
					SourcePrefab = randomSegmentPrefab,
				};
				
				Bounds segmentBounds = segmentInstance.SourcePrefab.AnalyzedSegmentLocalBounds;
				segmentBounds.center += segmentInstance.Instance.transform.localPosition;

				segmentInstances.Add(segmentInstance);

				shaftBounds.Encapsulate(segmentBounds);
			}
		}
	}

	[System.Serializable]
	private class ShaftSegmentInstance
	{
		public GameObject Instance;
		public ShaftSegmentPrefab SourcePrefab;
	}

	private List<ShaftSegmentInstance> segmentInstances = new List<ShaftSegmentInstance>();

	private System.Random segmentRandomizer = new System.Random();

	private void UpdateShaftSegmentPrefabs()
	{
		foreach (ShaftSegmentPrefab segmentPrefab in SegmentPrefabs)
		{
			segmentPrefab.AnalyzedSegmentLocalBounds = new Bounds();

			foreach (MeshFilter meshFilter in segmentPrefab.Prefab.GetComponentsInChildren<MeshFilter>())
			{
				Bounds segmentSpaceMeshBounds = new Bounds();

				segmentSpaceMeshBounds.Encapsulate(
					meshFilter.transform.TransformPoint(meshFilter.sharedMesh.bounds.min));
				
				segmentSpaceMeshBounds.Encapsulate(
					meshFilter.transform.TransformPoint(meshFilter.sharedMesh.bounds.max));

				segmentPrefab.AnalyzedSegmentLocalBounds.Encapsulate(segmentSpaceMeshBounds);
			}
			
			if (segmentPrefab.AnalyzedSegmentLocalBounds.size.y < Mathf.Epsilon)
			{
				throw new System.InvalidOperationException("We must have a mesh for every segment!");
			}

			segmentPrefab.AnalyzedSegmentContainsObstacle =
				(segmentPrefab.Prefab.GetComponentInChildren<FloorObstacle>() != null);
		}
	}
}

