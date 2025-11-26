# Creating a New Unity iOS Plugin

A step-by-step guide to create your own native iOS plugin following the established architecture.

---

## Overview

Every plugin consists of **4 core components** that work together:

```
Swift Implementation → ObjC++ Bridge → C# Wrapper → Xcode PostProcessor
```

---

## Project Structure

```
your-plugin-name/
├── package.json                    # Package metadata
├── README.md                       # Plugin documentation
├── Runtime/
│   ├── YourPlugin.cs              # C# public API
│   └── YourNamespace.asmdef       # Assembly definition
└── Plugins/
    └── iOS/
        ├── Source/
        │   ├── YourClass.swift           # Swift implementation
        │   ├── YourBridge.mm             # Objective-C++ bridge
        │   └── UnityFramework.modulemap  # Module mapping
        └── Editor/
            └── YourPostProcess.cs        # Xcode configuration
```

---

## Step-by-Step Guide

### 1. Swift Implementation

**File**: `Plugins/iOS/Source/YourClass.swift`

Write your native iOS logic. Keep it simple and focused.

```swift
import Foundation

@objc public class YourFeature: NSObject {
    @objc public static let shared = YourFeature()

    // Your native methods
    @objc public func doSomething() -> String {
        return "Result from native code"
    }

    // Callbacks to Unity (if needed)
    @objc public func sendToUnity(_ message: String) {
        UnitySendMessage("GameObjectName", "MethodName", message)
    }
}
```

**Key points**:

- Mark class and methods with `@objc` for ObjC++ interop
- Use `@objc public static let shared` for singleton pattern
- Return simple types (String, Int, Bool) or use callbacks

---

### 2. Objective-C++ Bridge

**File**: `Plugins/iOS/Source/YourBridge.mm`

Expose Swift methods to C# via C-callable functions.

```objc
#import <Foundation/Foundation.h>
#import <UnityFramework/UnityFramework-Swift.h>
#import "UnityInterface.h"
#include <stdlib.h>
#include <string.h>

extern "C"
{
    // Helper: Copy C string (Unity will free it)
    static char* cStringCopy(const char* string)
    {
        if (string == NULL) return NULL;
        size_t length = strlen(string) + 1;
        char* res = (char*)malloc(length);
        if (res != NULL) memcpy(res, string, length);
        return res;
    }

    // Your exposed functions
    char* yourFeatureDoSomething()
    {
        NSString* result = [[YourFeature shared] doSomething];
        return cStringCopy([result UTF8String]);
    }

    void yourFeatureFreeString(char* str)
    {
        if (str != NULL) free(str);
    }

    void yourFeatureSendToUnity(const char* message)
    {
        [[YourFeature shared] sendToUnity:[NSString stringWithUTF8String:message]];
    }
}
```

**Key points**:

- Use lowercase function names (e.g., `yourFeatureDoSomething`)
- Always provide a `freeString` function for string returns
- Wrap in `extern "C"` block for C linkage

---

### 3. C# Wrapper

**File**: `Runtime/YourPlugin.cs`

Provide a clean, type-safe C# API for Unity developers.

```csharp
using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace YourCompany.IOS.YourFeature
{
    public static class YourPlugin
    {
#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern IntPtr yourFeatureDoSomething();

        [DllImport("__Internal")]
        private static extern void yourFeatureFreeString(IntPtr str);

        [DllImport("__Internal")]
        private static extern void yourFeatureSendToUnity(string message);
#endif

        // Public API
        public static string DoSomething()
        {
#if UNITY_IOS && !UNITY_EDITOR
            IntPtr ptr = yourFeatureDoSomething();
            if (ptr == IntPtr.Zero) return string.Empty;

            try
            {
                return Marshal.PtrToStringAnsi(ptr) ?? string.Empty;
            }
            finally
            {
                yourFeatureFreeString(ptr);  // Always free!
            }
#else
            return "Editor stub - build for iOS to test";
#endif
        }

        public static void SendMessage(string message)
        {
#if UNITY_IOS && !UNITY_EDITOR
            yourFeatureSendToUnity(message ?? string.Empty);
#else
            Debug.Log($"[Editor] Would send: {message}");
#endif
        }

        // Events (optional)
        public static event Action<string> OnResult;

        // Callback from native (called via UnitySendMessage)
        public void HandleNativeCallback(string data)
        {
            OnResult?.Invoke(data);
        }
    }
}
```

**Key points**:

- Use `#if UNITY_IOS && !UNITY_EDITOR` to prevent Editor crashes
- Always free native strings in `finally` block
- Provide Editor stubs for testing
- Use events for async callbacks

---

### 4. Xcode PostProcessor

**File**: `Plugins/iOS/Editor/YourPostProcess.cs`

Automate Xcode configuration after Unity builds.

```csharp
#if UNITY_IOS
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEngine;

namespace YourCompany.IOS.YourFeature.Editor
{
    public static class YourPostProcess
    {
        [PostProcessBuild]
        public static void OnPostProcessBuild(BuildTarget buildTarget, string buildPath)
        {
            if (buildTarget != BuildTarget.iOS) return;

            string projectPath = PBXProject.GetPBXProjectPath(buildPath);
            var project = new PBXProject();
            project.ReadFromFile(projectPath);

            string frameworkGuid = project.GetUnityFrameworkTargetGuid();
            string mainGuid = project.GetUnityMainTargetGuid();

            // 1. Enable Swift module
            project.AddBuildProperty(frameworkGuid, "DEFINES_MODULE", "YES");

            // 2. Set modulemap (copy from your plugin)
            CopyModuleMap(project, frameworkGuid, buildPath);

            // 3. Make Unity headers public
            MakeHeaderPublic(project, frameworkGuid, "Classes/Unity/UnityInterface.h");

            // 4. Embed Swift libraries
            project.SetBuildProperty(mainGuid, "ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES", "YES");

            // 5. Add frameworks (if needed)
            // project.AddFrameworkToProject(frameworkGuid, "AVFoundation.framework", false);
            // project.AddFrameworkToProject(frameworkGuid, "Speech.framework", false);

            // 6. Add Info.plist keys (if needed)
            // string plistPath = Path.Combine(buildPath, "Info.plist");
            // PlistDocument plist = new PlistDocument();
            // plist.ReadFromFile(plistPath);
            // plist.root.SetString("NSCameraUsageDescription", "We need camera access");
            // plist.WriteToFile(plistPath);

            project.WriteToFile(projectPath);
            Debug.Log("YourPostProcess: Xcode configured successfully");
        }

        private static void CopyModuleMap(PBXProject project, string targetGuid, string buildPath)
        {
            string dstDir = Path.Combine(buildPath, "UnityFramework");
            Directory.CreateDirectory(dstDir);
            string dstFile = Path.Combine(dstDir, "UnityFramework.modulemap");

            if (!File.Exists(dstFile))
            {
                string srcFile = FindModuleMapInProject();
                if (!string.IsNullOrEmpty(srcFile))
                {
                    FileUtil.CopyFileOrDirectory(srcFile, dstFile);
                    project.AddFile(dstFile, "UnityFramework/UnityFramework.modulemap");
                }
            }

            if (File.Exists(dstFile))
            {
                project.SetBuildProperty(targetGuid, "MODULEMAP_FILE",
                    "$(SRCROOT)/UnityFramework/UnityFramework.modulemap");
            }
        }

        private static void MakeHeaderPublic(PBXProject project, string targetGuid, string path)
        {
            string guid = project.FindFileGuidByProjectPath(path);
            if (!string.IsNullOrEmpty(guid))
            {
                project.AddPublicHeaderToBuild(targetGuid, guid);
            }
        }

        private static string FindModuleMapInProject()
        {
            string assetsPath = Application.dataPath;
            var files = Directory.GetFiles(assetsPath, "UnityFramework.modulemap",
                SearchOption.AllDirectories);

            return files?.FirstOrDefault(f => f.Contains("/Plugins/iOS/"));
        }
    }
}
#endif
```

**Key points**:

- Runs automatically after iOS build
- Configure frameworks, headers, and Info.plist
- Copy modulemap from plugin to build directory
- Test by building to iOS and checking Xcode project

---

### 5. Package Metadata

**File**: `package.json`

```json
{
  "name": "com.yourcompany.your-plugin-name",
  "version": "1.0.0",
  "displayName": "Your Plugin Display Name",
  "description": "Brief description of what your plugin does",
  "unity": "2019.4",
  "author": {
    "name": "Your Company",
    "email": "dev@yourcompany.com"
  },
  "keywords": ["ios", "swift", "plugin", "unity"]
}
```

---

## Assembly Definition

**File**: `Runtime/YourNamespace.asmdef`

```json
{
  "name": "YourCompany.IOS.YourFeature",
  "rootNamespace": "YourCompany.IOS.YourFeature",
  "references": [],
  "includePlatforms": [],
  "excludePlatforms": [],
  "allowUnsafeCode": false,
  "overrideReferences": false,
  "precompiledReferences": [],
  "autoReferenced": true,
  "defineConstraints": [],
  "versionDefines": [],
  "noEngineReferences": false
}
```

---

## Module Map

**File**: `Plugins/iOS/Source/UnityFramework.modulemap`

Copy this exactly from `hello-world` plugin:

```
framework module UnityFramework {
    umbrella header "UnityFramework.h"

    export *
    module * { export * }

    explicit module Unity {
        header "Classes/Unity/UnityInterface.h"
        header "Classes/Unity/UnityForwardDecls.h"
        header "Classes/Unity/UnityRendering.h"
        header "Classes/Unity/UnitySharedDecls.h"
        export *
    }
}
```

---

## Testing Checklist

### In Unity Editor

- ✅ C# compiles without errors
- ✅ Assembly definition is valid
- ✅ Stub methods work (don't crash)
- ✅ Events can be subscribed to

### iOS Build

- ✅ Build completes without errors
- ✅ Xcode project opens successfully
- ✅ `UnityFramework` has `DEFINES_MODULE = YES`
- ✅ `MODULEMAP_FILE` points to `UnityFramework.modulemap`
- ✅ Unity headers are marked as "Public"
- ✅ Swift files compile in Xcode
- ✅ App runs on device/simulator
- ✅ Native methods return expected values
- ✅ Callbacks reach Unity successfully

---

## Common Issues & Solutions

### "Undefined symbol" errors

- **Cause**: Missing `@objc` in Swift or modulemap not found
- **Fix**: Add `@objc` to all classes/methods; verify modulemap exists

### "UnityInterface.h not found"

- **Cause**: Headers not marked as Public
- **Fix**: Check PostProcessor runs and marks headers correctly

### Bridge can't find Swift class

- **Cause**: Missing `DEFINES_MODULE` or wrong import
- **Fix**: Ensure `#import <UnityFramework/UnityFramework-Swift.h>`

### Memory leaks

- **Cause**: Not freeing strings returned from native
- **Fix**: Always call `freeString` in `finally` block

### PostProcessor not running

- **Cause**: Missing `#if UNITY_IOS` or wrong callback attribute
- **Fix**: Add `[PostProcessBuild]` and wrap in `#if UNITY_IOS`

---

## Pro Tips

### Keep it simple

- One plugin = one responsibility
- Prefer simple data types over complex objects
- Use events for async operations

### Memory management

```csharp
// ✅ Good: Always free in finally
try {
    string result = Marshal.PtrToStringAnsi(ptr);
    return result;
}
finally {
    FreeString(ptr);
}

// ❌ Bad: Memory leak if exception occurs
string result = Marshal.PtrToStringAnsi(ptr);
FreeString(ptr);
return result;
```

### Threading

```swift
// Always callback to Unity on main thread
DispatchQueue.main.async {
    UnitySendMessage("GameObject", "Method", "data")
}
```

### Error handling

```csharp
// Provide meaningful fallbacks
public static string GetData()
{
#if UNITY_IOS && !UNITY_EDITOR
    try {
        return NativeCall();
    }
    catch (Exception e) {
        Debug.LogError($"Native call failed: {e.Message}");
        return string.Empty;
    }
#else
    return "Editor stub";
#endif
}
```

### Debugging

- Use Xcode breakpoints in Swift code
- Add `Debug.Log` in C# wrapper
- Check Xcode console for native logs
- Test on real device for permissions/hardware

---

## Quick Reference

| Component        | Language      | Purpose                    |
| ---------------- | ------------- | -------------------------- |
| `*.swift`        | Swift         | Native iOS implementation  |
| `*.mm`           | Objective-C++ | Bridge between Swift and C |
| `*.cs` (Runtime) | C#            | Unity-facing API           |
| `*.cs` (Editor)  | C#            | Xcode build automation     |
| `*.modulemap`    | Text          | Module symbol resolution   |
| `*.asmdef`       | JSON          | Unity assembly definition  |

---

## Next Steps

1. **Copy** `unity-ios-library-hello-world` to a new folder
2. **Rename** files and classes to match your feature
3. **Implement** Swift logic for your use case
4. **Expose** methods via bridge
5. **Wrap** with C# API
6. **Test** in Unity Editor (stubs)
7. **Build** to iOS and test on device
8. **Document** usage in README.md

**Estimated time**: 30 mins for basic plugin, 2-3 hours for complex features

---

## Examples in This Repo

| Plugin          | Complexity      | Learn About                            |
| --------------- | --------------- | -------------------------------------- |
| `hello-world`   | ⭐ Basic        | Foundation pattern, messaging          |
| `camera`        | ⭐⭐ Medium     | Permissions, UIKit, file handling      |
| `audio-session` | ⭐⭐ Medium     | Observers, notifications, enums        |
| `custom-view`   | ⭐⭐⭐ Advanced | SwiftUI, keyboard handling, overlays   |
| `speech`        | ⭐⭐⭐ Advanced | Permissions, streaming data, languages |

---

**Happy plugin building!** 🚀

For questions or improvements to this guide, open an issue or PR.
