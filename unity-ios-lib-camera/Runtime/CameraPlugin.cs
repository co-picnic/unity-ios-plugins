using System;
using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;

namespace CoPicnic.IOS.Camera
{
	public static class CameraPlugin
	{
#if UNITY_IOS && !UNITY_EDITOR
		[DllImport("__Internal")]
		private static extern void cameraSetLoggingEnabled(bool enabled);
		
		[DllImport("__Internal")]
		private static extern void cameraSetPermissionCallback(PermissionCallback callback);
		
		[DllImport("__Internal")]
		private static extern void cameraRequestPermission();
		
		[DllImport("__Internal")]
		private static extern int cameraCheckPermission();
		
		[DllImport("__Internal")]
		private static extern bool cameraIsAvailable();
		
		[DllImport("__Internal")]
		private static extern bool cameraOpen(int maxSize, int quality);
		
		[DllImport("__Internal")]
		private static extern bool cameraOpenPhotoLibrary(int maxSize, int quality);
		
		[DllImport("__Internal")]
		private static extern void cameraDismiss();
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void PermissionCallback(bool granted);

		public enum PermissionStatus
		{
			NotDetermined = 0,
			Granted = 1,
			Denied = -1
		}

		public static event Action<bool> OnPermissionResult;
		public static event Action<Texture2D> OnImageCaptured;
		public static event Action OnCaptureCancelled;
		public static event Action<string> OnCaptureError;

		private static GameObject s_callbackObject;
		private static bool s_isInitialized = false;
		private static bool s_loggingEnabled = true;

		public static int MaxImageSize { get; set; } = 1024;
		public static int JpegQuality { get; set; } = 85;

		public static void SetLoggingEnabled(bool enabled)
		{
			s_loggingEnabled = enabled;
#if UNITY_IOS && !UNITY_EDITOR
			cameraSetLoggingEnabled(enabled);
#else
			Debug.Log($"[CoPicnic.iOS.Camera] Logging {(enabled ? "enabled" : "disabled")} (Editor)");
#endif
		}

		private static void EnsureInitialized()
		{
			if (s_isInitialized) return;

			s_callbackObject = new GameObject("CameraCallbackObject");
			s_callbackObject.AddComponent<CameraCallbackReceiver>();
			GameObject.DontDestroyOnLoad(s_callbackObject);

#if UNITY_IOS && !UNITY_EDITOR
			cameraSetPermissionCallback(PermissionCallbackHandler);
#endif
			s_isInitialized = true;
		}

		public static void RequestPermission()
		{
			EnsureInitialized();
#if UNITY_IOS && !UNITY_EDITOR
			cameraRequestPermission();
#else
			if (s_loggingEnabled) Debug.Log("[CoPicnic.iOS.Camera] Editor: Permission granted (stub)");
			OnPermissionResult?.Invoke(true);
#endif
		}

		public static PermissionStatus CheckPermission()
		{
#if UNITY_IOS && !UNITY_EDITOR
			return (PermissionStatus)cameraCheckPermission();
#else
			return PermissionStatus.Granted;
#endif
		}

		public static bool IsAvailable()
		{
#if UNITY_IOS && !UNITY_EDITOR
			return cameraIsAvailable();
#else
			return true;
#endif
		}

		[AOT.MonoPInvokeCallback(typeof(PermissionCallback))]
		private static void PermissionCallbackHandler(bool granted)
		{
			OnPermissionResult?.Invoke(granted);
		}

		public static bool OpenCamera()
		{
			EnsureInitialized();
#if UNITY_IOS && !UNITY_EDITOR
			if (!IsAvailable())
			{
				Debug.LogError("[CoPicnic.iOS.Camera] Camera not available on this device");
				return false;
			}
			if (CheckPermission() != PermissionStatus.Granted)
			{
				if (s_loggingEnabled) Debug.LogWarning("[CoPicnic.iOS.Camera] Permission not granted");
				return false;
			}
			return cameraOpen(MaxImageSize, JpegQuality);
#else
			if (s_loggingEnabled) Debug.Log("[CoPicnic.iOS.Camera] Editor: Opening camera (stub)");
			s_callbackObject.GetComponent<CameraCallbackReceiver>().SimulateCapture();
			return true;
#endif
		}

		public static bool OpenPhotoLibrary()
		{
			EnsureInitialized();
#if UNITY_IOS && !UNITY_EDITOR
			return cameraOpenPhotoLibrary(MaxImageSize, JpegQuality);
#else
			if (s_loggingEnabled) Debug.Log("[CoPicnic.iOS.Camera] Editor: Opening photo library (stub)");
			s_callbackObject.GetComponent<CameraCallbackReceiver>().SimulateCapture();
			return true;
#endif
		}

		public static void Dismiss()
		{
#if UNITY_IOS && !UNITY_EDITOR
			cameraDismiss();
#else
			if (s_loggingEnabled) Debug.Log("[CoPicnic.iOS.Camera] Editor: Dismissing camera (stub)");
#endif
		}

		private class CameraCallbackReceiver : MonoBehaviour
		{
			public void OnImageCaptured(string filePath)
			{
				StartCoroutine(LoadImageFromFile(filePath));
			}

			public void OnCaptureCancelled(string _)
			{
				CameraPlugin.OnCaptureCancelled?.Invoke();
			}

			public void OnCaptureError(string error)
			{
				Debug.LogError($"[CoPicnic.iOS.Camera] Error: {error}");
				CameraPlugin.OnCaptureError?.Invoke(error);
			}

			private IEnumerator LoadImageFromFile(string filePath)
			{
				if (string.IsNullOrEmpty(filePath) || !System.IO.File.Exists(filePath))
				{
					Debug.LogError($"[CoPicnic.iOS.Camera] Image file not found: {filePath}");
					CameraPlugin.OnCaptureError?.Invoke("Image file not found");
					yield break;
				}

				byte[] imageData = System.IO.File.ReadAllBytes(filePath);
				Texture2D texture = new Texture2D(2, 2);
				if (texture.LoadImage(imageData))
				{
					CameraPlugin.OnImageCaptured?.Invoke(texture);
				}
				else
				{
					Debug.LogError("[CoPicnic.iOS.Camera] Failed to load image data");
					CameraPlugin.OnCaptureError?.Invoke("Failed to load image");
				}

				try
				{
					System.IO.File.Delete(filePath);
				}
				catch (Exception e)
				{
					Debug.LogWarning($"[CoPicnic.iOS.Camera] Failed to delete temp file: {e.Message}");
				}
			}

#if UNITY_EDITOR
			public void SimulateCapture()
			{
				StartCoroutine(SimulateCaptureRoutine());
			}

			private IEnumerator SimulateCaptureRoutine()
			{
				yield return new WaitForSeconds(1f);
				
				Texture2D dummyTexture = new Texture2D(256, 256);
				Color[] pixels = new Color[256 * 256];
				for (int i = 0; i < pixels.Length; i++)
				{
					pixels[i] = new Color(
						UnityEngine.Random.value,
						UnityEngine.Random.value,
						UnityEngine.Random.value
					);
				}
				dummyTexture.SetPixels(pixels);
				dummyTexture.Apply();
				
				CameraPlugin.OnImageCaptured?.Invoke(dummyTexture);
			}
#endif
		}
	}
}




