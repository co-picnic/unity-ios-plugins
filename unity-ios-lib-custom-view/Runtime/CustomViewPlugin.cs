using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Ursula.IOS.CustomView
{
	public static class CustomViewPlugin
	{
#if UNITY_IOS && !UNITY_EDITOR
		[DllImport("__Internal")]
		private static extern bool customViewShow();
		
		[DllImport("__Internal")]
		private static extern void customViewDismiss();
		
		[DllImport("__Internal")]
		private static extern bool customViewIsVisible();
		
		[DllImport("__Internal")]
		private static extern void customViewSetText(string text);
#endif

		// Events
		public static event Action<string> OnTextSubmitted;
		public static event Action<string> OnButtonTapped;
		public static event Action<string> OnViewError;

		private static GameObject s_callbackObject;
		private static bool s_isInitialized = false;

		private static void EnsureInitialized()
		{
			if (s_isInitialized) return;

			s_callbackObject = new GameObject("CustomViewCallbackObject");
			s_callbackObject.AddComponent<CustomViewCallbackReceiver>();
			GameObject.DontDestroyOnLoad(s_callbackObject);

			s_isInitialized = true;
		}

		// View Management

		public static bool Show()
		{
			EnsureInitialized();
#if UNITY_IOS && !UNITY_EDITOR
			return customViewShow();
#else
			Debug.Log("[CustomView] Editor: Showing view (stub)");
			// Simulate submission after 2 seconds
			s_callbackObject.GetComponent<CustomViewCallbackReceiver>().SimulateSubmit();
			return true;
#endif
		}

		public static void Dismiss()
		{
#if UNITY_IOS && !UNITY_EDITOR
			customViewDismiss();
#else
			Debug.Log("[CustomView] Editor: Dismissing view (stub)");
#endif
		}

		public static bool IsVisible()
		{
#if UNITY_IOS && !UNITY_EDITOR
			return customViewIsVisible();
#else
			return false;
#endif
		}

		public static void SetText(string text)
		{
#if UNITY_IOS && !UNITY_EDITOR
			customViewSetText(text);
#else
			Debug.Log($"[CustomView] Editor: Set text to '{text}' (stub)");
#endif
		}

		// Callback Receiver Component
		private class CustomViewCallbackReceiver : MonoBehaviour
		{
			public void OnTextSubmitted(string text)
			{
				Debug.Log($"[CustomView] Text submitted: {text}");
				CustomViewPlugin.OnTextSubmitted?.Invoke(text);
			}

			public void OnButtonTapped(string buttonId)
			{
				Debug.Log($"[CustomView] Button tapped: {buttonId}");
				CustomViewPlugin.OnButtonTapped?.Invoke(buttonId);
			}

			public void OnViewError(string error)
			{
				Debug.LogError($"[CustomView] Error: {error}");
				CustomViewPlugin.OnViewError?.Invoke(error);
			}

#if UNITY_EDITOR
			public void SimulateSubmit()
			{
				StartCoroutine(SimulateSubmitRoutine());
			}

			private System.Collections.IEnumerator SimulateSubmitRoutine()
			{
				yield return new WaitForSeconds(2f);
				OnTextSubmitted("Editor stub input");
			}
#endif
		}
	}
}



