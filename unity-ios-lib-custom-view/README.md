## Unity iOS Custom View Plugin

Overlay transparent SwiftUI views on top of Unity for native text input and buttons.

### Install (as UPM package)
- Unity → Window → Package Manager → Add package from disk...
- Select `unity-ios-lib-custom-view/package.json`

### Requirements
- Unity iOS Build Support
- iOS target: IL2CPP + ARM64
- iOS 13.0+ (SwiftUI)
- Xcode installed

### Usage
Create a simple tester:
```csharp
using UnityEngine;
using UnityEngine.UI;
using Ursula.IOS.CustomView;

public class CustomViewDemo : MonoBehaviour
{
	[SerializeField] private Text resultText;
	[SerializeField] private Button showViewButton;
	
	void Start()
	{
		CustomViewPlugin.OnTextSubmitted += OnTextSubmitted;
		CustomViewPlugin.OnButtonTapped += OnButtonTapped;
		CustomViewPlugin.OnViewError += OnError;
		showViewButton.onClick.AddListener(() => CustomViewPlugin.Show());
	}
	
	void OnTextSubmitted(string text) => resultText.text = $"You entered: {text}";
	void OnButtonTapped(string id) => resultText.text = id == "cancel" ? "Cancelled" : $"Button: {id}";
	void OnError(string err) => Debug.LogError(err);
}
```

### What it does
- Presents a SwiftUI overlay (`CustomView`) over Unity’s GL view
- TextField with native keyboard, Submit/Cancel buttons
- Sends callbacks back to Unity via `UnitySendMessage`
- PostProcess configures Xcode (Swift, SwiftUI, modulemap, headers)

### API (C#)
```csharp
CustomViewPlugin.Show();
CustomViewPlugin.Dismiss();
CustomViewPlugin.IsVisible();
CustomViewPlugin.SetText("prefill");

CustomViewPlugin.OnTextSubmitted += (text) => { /* ... */ };
CustomViewPlugin.OnButtonTapped += (id) => { /* ... */ };
CustomViewPlugin.OnViewError += (err) => { /* ... */ };
```

### Files
```
unity-ios-lib-custom-view/
├── package.json
├── README.md
├── Runtime/
│   ├── Ursula.IOS.CustomView.asmdef
│   └── CustomViewPlugin.cs
└── Plugins/
    └── iOS/
        ├── Source/
        │   ├── ViewManager.swift
        │   ├── CustomView.swift
        │   ├── KeyboardResponder.swift
        │   ├── CustomViewBridge.mm
        │   └── UnityFramework.modulemap
        └── Editor/
            └── CustomViewPostProcess.cs
```

### Notes
- In Editor, calls are stubbed (simulated)
- Test on device for real SwiftUI/keyboard behavior
- If Xcode build fails with headers/modulemap errors, ensure:
  - UnityFramework target has `DEFINES_MODULE=YES`
  - `MODULEMAP_FILE=$(SRCROOT)/UnityFramework/UnityFramework.modulemap`
  - Unity headers (e.g., `Classes/Unity/UnityInterface.h`) are Public



