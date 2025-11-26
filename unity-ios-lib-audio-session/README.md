## CoPicnic iOS Audio Session Plugin

Native iOS audio session plugin for Unity using Swift + Objective-C++ bridge.

Features:
- Centralized AVAudioSession configuration
- Audio route detection (speaker, headphones, Bluetooth)
- Output volume and approximate mute state
- Interruption and silence hint notifications
- Main-thread Unity callbacks

Requirements:
- Unity with iOS Build Support
- Scripting Backend: IL2CPP, Architecture: ARM64
- Xcode (iOS 13+ recommended)

Usage:
1) Import this package (copy into `Assets/` or add via UPM from disk `package.json`).
2) Configure and activate the audio session at startup.
3) Subscribe to events as needed.

Example:

```csharp
using CoPicnic.IOS.Audio;

public class AudioManager : UnityEngine.MonoBehaviour
{
	void Start()
	{
		AudioSessionPlugin.SetLoggingEnabled(true);
		
		AudioSessionPlugin.Setup(
			AudioSessionPlugin.Category.PlayAndRecord,
			AudioSessionPlugin.Mode.VoiceChat,
			AudioSessionPlugin.Options.DefaultToSpeaker |
			AudioSessionPlugin.Options.AllowBluetooth |
			AudioSessionPlugin.Options.AllowBluetoothA2DP
		);
		
		AudioSessionPlugin.Activate();
		
		AudioSessionPlugin.OnRouteChanged += (reason, route) =>
		{
			UnityEngine.Debug.Log($"[CoPicnic.iOS.Audio] Route changed: {reason}");
		};
	}
}
```

See:
- `Runtime/AudioSessionPlugin.cs` for the C# API
- `Plugins/iOS/Source` for native code and bridge
- `Plugins/iOS/Editor/AudioSessionPostProcess.cs` for Xcode setup


