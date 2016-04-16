using UnityEngine;
using System.Collections.Generic;
using System.Text;

public class DropShadowOrienter : MonoBehaviour
{
	public Vector3 FacingDirection = Vector3.down;

	public void LateUpdate()
	{
		transform.rotation = 
			Quaternion.LookRotation(
				FacingDirection, 
				(transform.parent.rotation * Vector3.forward));
	}
}

