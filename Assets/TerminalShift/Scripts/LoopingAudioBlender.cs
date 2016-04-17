using UnityEngine;
using System.Collections.Generic;
using System.Text;

public class LoopingAudioBlender : MonoBehaviour
{
	public AudioClip[] AvailableAudioClips = null;

	public float RandomPitchMin = 1.0f;
	public float RandomPitchMax = 1.0f;

	public float VolumeFraction = 1.0f;

	public void Start()
	{
		outgoingAudioSource = GetComponent<AudioSource>();

		originalComponentVolumeFraction = outgoingAudioSource.volume;

		// Duplicate the existing audio source.
		{
			incomingAudioSource = gameObject.AddComponent<AudioSource>();

			incomingAudioSource.outputAudioMixerGroup = outgoingAudioSource.outputAudioMixerGroup;
			incomingAudioSource.mute = outgoingAudioSource.mute;
			incomingAudioSource.priority = outgoingAudioSource.priority;

			foreach (AudioSourceCurveType curveType in System.Enum.GetValues(typeof(AudioSourceCurveType)))
			{
				incomingAudioSource.SetCustomCurve(
					curveType, 
					outgoingAudioSource.GetCustomCurve(curveType));
			}
		}

		// Populate both of the audio sources by scheduling two clips in a row.
		{
			ScheduleNextClip();
			ScheduleNextClip();

			outgoingAudioSource.Play();
		}
	}

	public void Update()
	{
		if (outgoingAudioSource.isPlaying == false)
		{
			ScheduleNextClip();
		}

		float outgoingClipMaxFadeSeconds = (outgoingAudioSource.clip.length / 2.0f);
		float incomingClipMaxFadeSeconds = (incomingAudioSource.clip.length / 2.0f);
		float fadeTotalSeconds = Mathf.Min(outgoingClipMaxFadeSeconds, incomingClipMaxFadeSeconds);

		float fadeFraction = 
			Mathf.Clamp01(Mathf.InverseLerp(
				(outgoingAudioSource.clip.length - fadeTotalSeconds), // fadeStart
				outgoingAudioSource.clip.length, // fadeEnd
				outgoingAudioSource.time));

		if ((fadeFraction > 0.0f) &&
			(incomingAudioSource.isPlaying == false))
		{
			incomingAudioSource.Play();
		}

		float maxVolumeFraction = (originalComponentVolumeFraction * VolumeFraction);

		outgoingAudioSource.volume = ((1.0f - fadeFraction) * maxVolumeFraction);
		incomingAudioSource.volume = (fadeFraction * maxVolumeFraction);
	}

	private void ScheduleNextClip()
	{
		// Swap the sources.
		{
			AudioSource temp = outgoingAudioSource;
			outgoingAudioSource = incomingAudioSource;
			incomingAudioSource = temp;

			// Ensure the new incoming isn't playing yet.
			incomingAudioSource.Stop();
		}

		incomingAudioSource.clip = 
			AvailableAudioClips[randomValues.Next(AvailableAudioClips.Length)];

		incomingAudioSource.pitch =
			Mathf.Lerp(RandomPitchMin, RandomPitchMax, (float)randomValues.NextDouble());
	}

	private AudioSource outgoingAudioSource = null;
	private AudioSource incomingAudioSource = null;

	private System.Random randomValues = new System.Random();

	private float originalComponentVolumeFraction = 1.0f;
}

