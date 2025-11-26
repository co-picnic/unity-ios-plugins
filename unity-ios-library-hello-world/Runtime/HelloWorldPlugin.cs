using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Ursula.IOS.HelloWorld
{
	public static class HelloWorldPlugin
	{
#if UNITY_IOS && !UNITY_EDITOR
		[DllImport("__Internal")]
		private static extern IntPtr helloWorldGetMessage();

		[DllImport("__Internal")]
		private static extern void helloWorldFreeString(IntPtr str);

		[DllImport("__Internal")]
		private static extern void helloWorldSendToUnity(string gameObjectName, string methodName, string message);
#endif

		public static string GetMessage()
		{
#if UNITY_IOS && !UNITY_EDITOR
			IntPtr ptr = helloWorldGetMessage();
			if (ptr == IntPtr.Zero)
			{
				return string.Empty;
			}
			try
			{
				string message = Marshal.PtrToStringAnsi(ptr);
				return message ?? string.Empty;
			}
			finally
			{
				helloWorldFreeString(ptr);
			}
#else
			return "Hello from Editor stub (iOS build required).";
#endif
		}

		public static void SendToUnity(string gameObjectName, string methodName, string message)
		{
#if UNITY_IOS && !UNITY_EDITOR
			if (string.IsNullOrEmpty(gameObjectName) || string.IsNullOrEmpty(methodName))
			{
				Debug.LogWarning("HelloWorldPlugin.SendToUnity called with empty target or method.");
				return;
			}
			helloWorldSendToUnity(gameObjectName, methodName, message ?? string.Empty);
#else
			Debug.Log($"[Editor stub] Would send to {gameObjectName}.{methodName}: {message}");
#endif
		}
	}
}


