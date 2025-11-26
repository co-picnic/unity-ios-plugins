using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace CoPicnic.IOS.Audio
{
	public static class AudioSessionPlugin
	{
#if UNITY_IOS && !UNITY_EDITOR
		[DllImport("__Internal")]
		private static extern void audioSetLoggingEnabled(bool enabled);
		
		[DllImport("__Internal")]
		private static extern bool audioSessionSetup(string category, string mode, uint options);
		
		[DllImport("__Internal")]
		private static extern bool audioSessionActivate();
		
		[DllImport("__Internal")]
		private static extern bool audioSessionDeactivate();
		
		[DllImport("__Internal")]
		private static extern float audioSessionGetOutputVolume();
		
		[DllImport("__Internal")]
		private static extern bool audioSessionIsOtherAudioPlaying();
		
		[DllImport("__Internal")]
		private static extern bool audioSessionIsMutedApprox();
		
		[DllImport("__Internal")]
		private static extern string audioSessionGetCurrentRouteJSON();
		
		[DllImport("__Internal")]
		private static extern void audioSessionStartObservingRouteChanges();
		
		[DllImport("__Internal")]
		private static extern void audioSessionStopObservingRouteChanges();
		
		[DllImport("__Internal")]
		private static extern void audioSessionStartObservingInterruptions();
		
		[DllImport("__Internal")]
		private static extern void audioSessionStopObservingInterruptions();
		
		[DllImport("__Internal")]
		private static extern void audioSessionStartObservingSilenceHint();
		
		[DllImport("__Internal")]
		private static extern void audioSessionStopObservingSilenceHint();
		
		[DllImport("__Internal")]
		private static extern void audioSessionSetRouteChangedCallback(RouteChangedCallback callback);
		
		[DllImport("__Internal")]
		private static extern void audioSessionSetInterruptionBeganCallback(InterruptionBeganCallback callback);
		
		[DllImport("__Internal")]
		private static extern void audioSessionSetInterruptionEndedCallback(InterruptionEndedCallback callback);
		
		[DllImport("__Internal")]
		private static extern void audioSessionSetSilenceHintCallback(SilenceHintCallback callback);
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void RouteChangedCallback(string reason, string routeJSON);
		
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void InterruptionBeganCallback();
		
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void InterruptionEndedCallback(bool shouldResume);
		
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void SilenceHintCallback(bool shouldSilence);

		public enum Category
		{
			Ambient,
			SoloAmbient,
			Playback,
			Record,
			PlayAndRecord,
			MultiRoute
		}

		public enum Mode
		{
			Default,
			VoiceChat,
			VideoChat,
			GameChat,
			VideoRecording,
			Measurement,
			MoviePlayback,
			SpokenAudio
		}

		[Flags]
		public enum Options : uint
		{
			None = 0,
			MixWithOthers = 0x1,
			DuckOthers = 0x2,
			AllowBluetooth = 0x4,
			DefaultToSpeaker = 0x8,
			InterruptSpokenAudioAndMixWithOthers = 0x11,
			AllowBluetoothA2DP = 0x20,
			AllowAirPlay = 0x40
		}

		public static event Action<string, AudioRoute> OnRouteChanged;
		public static event Action OnInterruptionBegan;
		public static event Action<bool> OnInterruptionEnded;
		public static event Action<bool> OnSilenceHintChanged;

		private static bool s_isInitialized = false;
		private static bool s_loggingEnabled = true;

		public static void SetLoggingEnabled(bool enabled)
		{
			s_loggingEnabled = enabled;
#if UNITY_IOS && !UNITY_EDITOR
			audioSetLoggingEnabled(enabled);
#else
			Debug.Log($"[CoPicnic.iOS.Audio] Logging {(enabled ? "enabled" : "disabled")} (Editor)");
#endif
		}

		public static bool Setup(Category category, Mode mode, Options options)
		{
			EnsureInitialized();
			
#if UNITY_IOS && !UNITY_EDITOR
			string categoryStr = category.ToString();
			string modeStr = mode.ToString();
			return audioSessionSetup(categoryStr, modeStr, (uint)options);
#else
			if (s_loggingEnabled) Debug.Log($"[CoPicnic.iOS.Audio] Editor: Setup {category}/{mode}/{options}");
			return true;
#endif
		}

		public static bool Activate()
		{
			EnsureInitialized();
			
#if UNITY_IOS && !UNITY_EDITOR
			return audioSessionActivate();
#else
			if (s_loggingEnabled) Debug.Log("[CoPicnic.iOS.Audio] Editor: Activated");
			return true;
#endif
		}

		public static bool Deactivate()
		{
#if UNITY_IOS && !UNITY_EDITOR
			return audioSessionDeactivate();
#else
			if (s_loggingEnabled) Debug.Log("[CoPicnic.iOS.Audio] Editor: Deactivated");
			return true;
#endif
		}

		public static float GetOutputVolume()
		{
#if UNITY_IOS && !UNITY_EDITOR
			return audioSessionGetOutputVolume();
#else
			return 0.75f;
#endif
		}

		public static bool IsOtherAudioPlaying()
		{
#if UNITY_IOS && !UNITY_EDITOR
			return audioSessionIsOtherAudioPlaying();
#else
			return false;
#endif
		}

		public static bool IsMutedApprox()
		{
#if UNITY_IOS && !UNITY_EDITOR
			return audioSessionIsMutedApprox();
#else
			return false;
#endif
		}

		public static AudioRoute GetCurrentRoute()
		{
#if UNITY_IOS && !UNITY_EDITOR
			string json = audioSessionGetCurrentRouteJSON();
			return AudioRoute.FromJSON(json);
#else
			return new AudioRoute();
#endif
		}

		public static void StartObservingRouteChanges()
		{
			EnsureInitialized();
#if UNITY_IOS && !UNITY_EDITOR
			audioSessionStartObservingRouteChanges();
#endif
		}

		public static void StopObservingRouteChanges()
		{
#if UNITY_IOS && !UNITY_EDITOR
			audioSessionStopObservingRouteChanges();
#endif
		}

		public static void StartObservingInterruptions()
		{
			EnsureInitialized();
#if UNITY_IOS && !UNITY_EDITOR
			audioSessionStartObservingInterruptions();
#endif
		}

		public static void StopObservingInterruptions()
		{
#if UNITY_IOS && !UNITY_EDITOR
			audioSessionStopObservingInterruptions();
#endif
		}

		public static void StartObservingSilenceHint()
		{
			EnsureInitialized();
#if UNITY_IOS && !UNITY_EDITOR
			audioSessionStartObservingSilenceHint();
#endif
		}

		public static void StopObservingSilenceHint()
		{
#if UNITY_IOS && !UNITY_EDITOR
			audioSessionStopObservingSilenceHint();
#endif
		}

		private static void EnsureInitialized()
		{
			if (s_isInitialized) return;
#if UNITY_IOS && !UNITY_EDITOR
			audioSessionSetRouteChangedCallback(RouteChangedHandler);
			audioSessionSetInterruptionBeganCallback(InterruptionBeganHandler);
			audioSessionSetInterruptionEndedCallback(InterruptionEndedHandler);
			audioSessionSetSilenceHintCallback(SilenceHintHandler);
#endif
			s_isInitialized = true;
		}

		[AOT.MonoPInvokeCallback(typeof(RouteChangedCallback))]
		private static void RouteChangedHandler(string reason, string routeJSON)
		{
			AudioRoute route = AudioRoute.FromJSON(routeJSON);
			OnRouteChanged?.Invoke(reason, route);
		}

		[AOT.MonoPInvokeCallback(typeof(InterruptionBeganCallback))]
		private static void InterruptionBeganHandler()
		{
			OnInterruptionBegan?.Invoke();
		}

		[AOT.MonoPInvokeCallback(typeof(InterruptionEndedCallback))]
		private static void InterruptionEndedHandler(bool shouldResume)
		{
			OnInterruptionEnded?.Invoke(shouldResume);
		}

		[AOT.MonoPInvokeCallback(typeof(SilenceHintCallback))]
		private static void SilenceHintHandler(bool shouldSilence)
		{
			OnSilenceHintChanged?.Invoke(shouldSilence);
		}
	}

	[Serializable]
	public class AudioRoute
	{
		public AudioPort[] inputs = new AudioPort[0];
		public AudioPort[] outputs = new AudioPort[0];

		public static AudioRoute FromJSON(string json)
		{
			try
			{
				return JsonUtility.FromJson<AudioRoute>(json);
			}
			catch
			{
				return new AudioRoute();
			}
		}

		public bool IsOutputToSpeaker()
		{
			foreach (var output in outputs)
			{
				if (output.type == "BuiltInSpeaker") return true;
			}
			return false;
		}

		public bool IsOutputToHeadphones()
		{
			foreach (var output in outputs)
			{
				if (output.type == "Headphones") return true;
			}
			return false;
		}

		public bool IsOutputToBluetooth()
		{
			foreach (var output in outputs)
			{
				if (output.type.Contains("Bluetooth")) return true;
			}
			return false;
		}
	}

	[Serializable]
	public class AudioPort
	{
		public string name;
		public string type;
		public string uid;
	}
}


