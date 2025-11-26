## CoPicnic iOS Camera Plugin

Native iOS camera plugin for Unity using Swift + Objective-C++ bridge.

Features:
- Selfie/photo capture via system camera UI
- Photo library picker
- Camera permission handling
- Native-side image resizing and JPEG compression
- File path callback to Unity (loads Texture2D and cleans up temp file)
- CoPicnic logging toggle

Requirements:
- Unity with iOS Build Support
- Scripting Backend: IL2CPP, Architecture: ARM64
- Xcode (iOS 13+ recommended)

Usage:
1) Import this package (copy into `Assets/` or add via UPM from disk `package.json`).
2) Subscribe to events and call `CameraPlugin.OpenCamera()` or `OpenPhotoLibrary()`.
3) Build for iOS; the postprocess step auto-configures Xcode (frameworks + Info.plist keys).

See `Runtime/CameraPlugin.cs` for API and `Plugins/iOS/Source` for native code.




