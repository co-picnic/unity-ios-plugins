using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace CoPicnic.IOS.Speech
{
	public static class SpeechPlugin
	{
#if UNITY_IOS && !UNITY_EDITOR
		[DllImport("__Internal")]
		private static extern void speechSetLoggingEnabled(bool enabled);
		
		[DllImport("__Internal")]
		private static extern void speechSetContinuousMode(bool enabled);
		
		[DllImport("__Internal")]
		private static extern void speechRequestPermission(PermissionCallback callback);
		
		[DllImport("__Internal")]
		private static extern bool speechHasPermission();
		
		[DllImport("__Internal")]
		private static extern int speechCheckPermissionStatus();
		
		[DllImport("__Internal")]
		private static extern bool speechStartRecognition(string localeCode);
		
		[DllImport("__Internal")]
		private static extern void speechStopRecognition();
		
		[DllImport("__Internal")]
		private static extern void speechCancelRecognition();
		
		[DllImport("__Internal")]
		private static extern bool speechIsRecognitionActive();
		
		[DllImport("__Internal")]
		private static extern string speechGetSupportedLocales();
		
		[DllImport("__Internal")]
		private static extern bool speechIsLocaleSupported(string localeCode);
		
		[DllImport("__Internal")]
		private static extern void speechHandleRouteChange();
		
		[DllImport("__Internal")]
		private static extern void speechHandleInterruption(bool began);
		
		[DllImport("__Internal")]
		private static extern void speechSetReadyCallback(ReadyCallback callback);
		
		[DllImport("__Internal")]
		private static extern void speechSetPartialResultCallback(PartialResultCallback callback);
		
		[DllImport("__Internal")]
		private static extern void speechSetFinalResultCallback(FinalResultCallback callback);
		
		[DllImport("__Internal")]
		private static extern void speechSetErrorCallback(ErrorCallback callback);
		
		[DllImport("__Internal")]
		private static extern void speechSetCancelledCallback(CancelledCallback callback);
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void PermissionCallback(bool granted);
		
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void ReadyCallback();
		
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void PartialResultCallback(string text);
		
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void FinalResultCallback(string text);
		
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void ErrorCallback(string message);
		
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void CancelledCallback();

		public enum PermissionStatus
		{
			NotDetermined = 0,
			Granted = 1,
			Denied = -1
		}

		public static event Action OnReady;
		public static event Action<string> OnPartialResult;
		public static event Action<string> OnFinalResult;
		public static event Action<string> OnError;
		public static event Action OnCancelled;

		private static bool s_isInitialized = false;
		private static bool s_loggingEnabled = true;
		private static Action<bool> s_permissionUserCallback;

		public static void SetLoggingEnabled(bool enabled)
		{
			s_loggingEnabled = enabled;
#if UNITY_IOS && !UNITY_EDITOR
			speechSetLoggingEnabled(enabled);
#else
			Debug.Log($"[CoPicnic.iOS.Speech] Logging {(enabled ? "enabled" : "disabled")} (Editor)");
#endif
		}
		
		public static void SetContinuousMode(bool enabled)
		{
#if UNITY_IOS && !UNITY_EDITOR
			speechSetContinuousMode(enabled);
#else
			if (s_loggingEnabled) Debug.Log($"[CoPicnic.iOS.Speech] Editor: ContinuousMode={(enabled ? "ON" : "OFF")}");
#endif
		}

		public static void RequestPermission(Action<bool> callback)
		{
			EnsureInitialized();
			
#if UNITY_IOS && !UNITY_EDITOR
			// Store user callback and use a static AOT-safe handler
			s_permissionUserCallback = callback;
			speechRequestPermission(PermissionResultHandler);
#else
			if (s_loggingEnabled) Debug.Log("[CoPicnic.iOS.Speech] Editor: Permission granted (stub)");
			callback?.Invoke(true);
#endif
		}

		public static bool HasPermission()
		{
#if UNITY_IOS && !UNITY_EDITOR
			return speechHasPermission();
#else
			return true;
#endif
		}

		public static PermissionStatus CheckPermissionStatus()
		{
#if UNITY_IOS && !UNITY_EDITOR
			return (PermissionStatus)speechCheckPermissionStatus();
#else
			return PermissionStatus.Granted;
#endif
		}

		public static bool StartRecognition(string localeCode = "en-US")
		{
			EnsureInitialized();
			
#if UNITY_IOS && !UNITY_EDITOR
			return speechStartRecognition(localeCode);
#else
			if (s_loggingEnabled) Debug.Log($"[CoPicnic.iOS.Speech] Editor: Started recognition with locale {localeCode} (stub)");
			OnReady?.Invoke();
			return true;
#endif
		}

		public static void StopRecognition()
		{
#if UNITY_IOS && !UNITY_EDITOR
			speechStopRecognition();
#else
			if (s_loggingEnabled) Debug.Log("[CoPicnic.iOS.Speech] Editor: Stopped recognition (stub)");
#endif
		}

		public static void CancelRecognition()
		{
#if UNITY_IOS && !UNITY_EDITOR
			speechCancelRecognition();
#else
			if (s_loggingEnabled) Debug.Log("[CoPicnic.iOS.Speech] Editor: Cancelled recognition (stub)");
			OnCancelled?.Invoke();
#endif
		}

		public static bool IsRecognitionActive()
		{
#if UNITY_IOS && !UNITY_EDITOR
			return speechIsRecognitionActive();
#else
			return false;
#endif
		}

		public static string[] GetSupportedLocales()
		{
#if UNITY_IOS && !UNITY_EDITOR
			string localesString = speechGetSupportedLocales();
			return string.IsNullOrEmpty(localesString) ? new string[0] : localesString.Split(',');
#else
			return new string[] { "en-US", "pt-BR", "es-ES", "fr-FR", "de-DE" };
#endif
		}

		public static bool IsLocaleSupported(string localeCode)
		{
#if UNITY_IOS && !UNITY_EDITOR
			return speechIsLocaleSupported(localeCode);
#else
			return true;
#endif
		}

		public static void HandleRouteChange()
		{
#if UNITY_IOS && !UNITY_EDITOR
			speechHandleRouteChange();
#endif
		}

		public static void HandleInterruption(bool began)
		{
#if UNITY_IOS && !UNITY_EDITOR
			speechHandleInterruption(began);
#endif
		}

		private static void EnsureInitialized()
		{
			if (s_isInitialized) return;
#if UNITY_IOS && !UNITY_EDITOR
			speechSetReadyCallback(ReadyHandler);
			speechSetPartialResultCallback(PartialResultHandler);
			speechSetFinalResultCallback(FinalResultHandler);
			speechSetErrorCallback(ErrorHandler);
			speechSetCancelledCallback(CancelledHandler);
#endif
			s_isInitialized = true;
		}

		[AOT.MonoPInvokeCallback(typeof(PermissionCallback))]
		private static void PermissionResultHandler(bool granted)
		{
			var cb = s_permissionUserCallback;
			s_permissionUserCallback = null;
			cb?.Invoke(granted);
		}

		[AOT.MonoPInvokeCallback(typeof(ReadyCallback))]
		private static void ReadyHandler()
		{
			OnReady?.Invoke();
		}

		[AOT.MonoPInvokeCallback(typeof(PartialResultCallback))]
		private static void PartialResultHandler(string text)
		{
			OnPartialResult?.Invoke(text);
		}

		[AOT.MonoPInvokeCallback(typeof(FinalResultCallback))]
		private static void FinalResultHandler(string text)
		{
			OnFinalResult?.Invoke(text);
		}

		[AOT.MonoPInvokeCallback(typeof(ErrorCallback))]
		private static void ErrorHandler(string message)
		{
			OnError?.Invoke(message);
		}

		[AOT.MonoPInvokeCallback(typeof(CancelledCallback))]
		private static void CancelledHandler()
		{
			OnCancelled?.Invoke();
		}
	}
}


