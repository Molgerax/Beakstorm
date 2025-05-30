#if !(UNITY_DASHBOARD_WIDGET || UNITY_WEBPLAYER || UNITY_WII || UNITY_WIIU || UNITY_NACL || UNITY_FLASH || UNITY_BLACKBERRY) // Disable under unsupported platforms.
#if !UNITY_2019_1_OR_NEWER
#define AK_ENABLE_TIMELINE
#endif
#if AK_ENABLE_TIMELINE
/*******************************************************************************
The content of this file includes portions of the proprietary AUDIOKINETIC Wwise
Technology released in source code form as part of the game integration package.
The content of this file may not be used without valid licenses to the
AUDIOKINETIC Wwise Technology.
Note that the use of the game engine is subject to the Unity(R) Terms of
Service at https://unity3d.com/legal/terms-of-service
License Usage
Licensees holding valid licenses to the AUDIOKINETIC Wwise Technology may use
this file in accordance with the end user license agreement provided with the
software or, alternatively, in accordance with the terms contained
in a written agreement between you and Audiokinetic Inc.
Copyright (c) 2025 Audiokinetic Inc.
*******************************************************************************/
[UnityEngine.Timeline.TrackColor(0.855f, 0.8623f, 0.870f)]
[UnityEngine.Timeline.TrackClipType(typeof(AkEventPlayable))]
[UnityEngine.Timeline.TrackBindingType(typeof(UnityEngine.GameObject))]
[System.Obsolete(AkUnitySoundEngine.Deprecation_2019_2_0)]
#if UNITY_2019_1_OR_NEWER
[UnityEngine.Timeline.HideInMenu]
#endif
/// @brief A track within timeline that holds \ref AkEventPlayable clips. 
/// @details AkEventTracks are bound to a specific GameObject, which is the default emitter for all of the \ref AkEventPlayable clips. There is an option to override this in /ref AkEventPlayable.
/// \sa
/// - \ref AkEventPlayable
/// - \ref AkEventPlayableBehavior
public class AkEventTrack : UnityEngine.Timeline.TrackAsset
{
	public override UnityEngine.Playables.Playable CreateTrackMixer(UnityEngine.Playables.PlayableGraph graph, UnityEngine.GameObject go, int inputCount)
	{
		var playable = UnityEngine.Playables.ScriptPlayable<AkEventPlayableBehavior>.Create(graph);
		UnityEngine.Playables.PlayableExtensions.SetInputCount(playable, inputCount);
		var clips = GetClips();
		foreach (var clip in clips)
		{
			var akEventPlayable = clip.asset as AkEventPlayable;
			akEventPlayable.owningClip = clip;
		}
		return playable;
	}
}
#endif // AK_ENABLE_TIMELINE
#endif // #if ! (UNITY_DASHBOARD_WIDGET || UNITY_WEBPLAYER || UNITY_WII || UNITY_WIIU || UNITY_NACL || UNITY_FLASH || UNITY_BLACKBERRY) // Disable under unsupported platforms.
