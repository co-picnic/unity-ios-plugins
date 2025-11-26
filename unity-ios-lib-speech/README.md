## CoPicnic iOS Speech Recognition Plugin

Native iOS speech recognition plugin for Unity using Apple's Speech framework (Swift + Objective-C++ bridge).

Features:
- On-device speech-to-text (SFSpeech)
- Real-time partial and final results
- Multi-language support
- Permission handling for Speech + Microphone
- Main-thread Unity callbacks
- CoPicnic logging toggle

Requirements:
- Unity with iOS Build Support
- Scripting Backend: IL2CPP, Architecture: ARM64
- Xcode (iOS 13+ recommended)

Usage:
1) Import this package (copy into `Assets/` or add via UPM from disk `package.json`).
2) Request permission and start recognition.

Example:

```csharp
using CoPicnic.IOS.Speech;
using UnityEngine;

public class SpeechExample : MonoBehaviour
{
	void Start()
	{
		SpeechPlugin.SetLoggingEnabled(true);
		
		SpeechPlugin.RequestPermission((granted) =>
		{
			if (!granted) { Debug.LogError("Speech permission denied"); return; }
			
			SpeechPlugin.OnReady += () => Debug.Log("Ready for speech");
			SpeechPlugin.OnPartialResult += text => Debug.Log("Partial: " + text);
			SpeechPlugin.OnFinalResult += text => Debug.Log("Final: " + text);
			SpeechPlugin.OnError += msg => Debug.LogError("Error: " + msg);
			
			SpeechPlugin.StartRecognition("en-US");
		});
	}
}
```

See:
- `Runtime/SpeechPlugin.cs` for the C# API
- `Plugins/iOS/Source` for native code and bridge
- `Plugins/iOS/Editor/SpeechPostProcess.cs` for Xcode setup (frameworks + Info.plist)


