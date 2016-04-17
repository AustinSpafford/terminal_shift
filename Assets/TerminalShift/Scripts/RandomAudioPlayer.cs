using UnityEngine;
using System.Collections.Generic;
using System.Text;

public class RandomAudioPlayer : MonoBehaviour
{
	public AudioClip[] AvailableAudioClips = null;

	public float RandomPitchMin = 1.0f;
	public float RandomPitchMax = 1.0f;
	
	public bool DestroyOnCompletion = true;

	public void Start()
	{
		audioSource = GetComponent<AudioSource>();
		
		audioSource.clip = 
			AvailableAudioClips[randomValues.Next(AvailableAudioClips.Length)];

		audioSource.pitch =
			Mathf.Lerp(RandomPitchMin, RandomPitchMax, (float)randomValues.NextDouble());
		
		audioSource.Play();
	}

	public void Update()
	{
		if ((audioSource.isPlaying == false) &&
			DestroyOnCompletion)
		{
			GameObject.Destroy(gameObject);
		}
	}

	private AudioSource audioSource = null;

	private System.Random randomValues = new System.Random();
}

