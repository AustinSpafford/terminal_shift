using UnityEngine;
using System.Collections.Generic;
using System.Text;

public class FogManipulator : MonoBehaviour
{
	public void Awake()
	{
		originalFogConfig = new FogConfig()
		{			
			FogColor = RenderSettings.fogColor,
			FogStartDistance = RenderSettings.fogStartDistance,
			FogEndDistance = RenderSettings.fogEndDistance,
		};

		StartFadeToOriginalFog(1.0f);
		fadeFraction = 1.0f;
	}

	public void Update()
	{
		if (fadeFraction < 1.0f)
		{
			fadeFraction =
				Mathf.Clamp01(fadeFraction + (Time.deltaTime / fadeDurationSeconds));

			RenderSettings.fogColor =
				Color.Lerp(
					fadeStartFogConfig.FogColor, 
					fadeEndFogConfig.FogColor, 
					fadeFraction);

			RenderSettings.fogStartDistance =
				Mathf.Lerp(
					fadeStartFogConfig.FogStartDistance, 
					fadeEndFogConfig.FogStartDistance, 
					fadeFraction);

			RenderSettings.fogEndDistance =
				Mathf.Lerp(
					fadeStartFogConfig.FogEndDistance, 
					fadeEndFogConfig.FogEndDistance, 
					fadeFraction);
		}
	}

	public void StartFadeToFogOverride(
		Color overrideColor,
		float overrideFogStartDistance,
		float overrideFogEndDistance,
		float fadeSeconds)
	{
		FogConfig overrideFogConfig = new FogConfig()
		{			
			FogColor = overrideColor,
			FogStartDistance = overrideFogStartDistance,
			FogEndDistance = overrideFogEndDistance,
		};

		StartFadeInternal(overrideFogConfig, fadeSeconds);
	}

	public void StartFadeToOriginalFog(
		float fadeSeconds)
	{
		StartFadeInternal(originalFogConfig, fadeSeconds);
	}

	private struct FogConfig
	{
		public Color FogColor;
		public float FogStartDistance;
		public float FogEndDistance;
	}

	private FogConfig originalFogConfig;

	private FogConfig fadeStartFogConfig;
	private FogConfig fadeEndFogConfig;
	private float fadeFraction = 1.0f;
	private float fadeDurationSeconds = 1.0f;
	
	private void StartFadeInternal(
		FogConfig overrideFogConfig,
		float fadeSeconds)
	{
		fadeStartFogConfig = new FogConfig()
		{			
			FogColor = RenderSettings.fogColor,
			FogStartDistance = RenderSettings.fogStartDistance,
			FogEndDistance = RenderSettings.fogEndDistance,
		};

		fadeEndFogConfig = overrideFogConfig;

		fadeFraction = 0.0f;
		fadeDurationSeconds = fadeSeconds;
	}
}

