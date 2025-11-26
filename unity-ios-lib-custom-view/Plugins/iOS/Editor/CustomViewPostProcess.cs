#if UNITY_IOS
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEngine;

namespace Ursula.IOS.CustomView.Editor
{
	public static class CustomViewPostProcess
	{
		[PostProcessBuild(104)]
		public static void OnPostProcessBuild(BuildTarget buildTarget, string buildPath)
		{
			if (buildTarget != BuildTarget.iOS)
			{
				return;
			}

			string projectPath = PBXProject.GetPBXProjectPath(buildPath);
			var project = new PBXProject();
			project.ReadFromFile(projectPath);

			string unityFrameworkGuid = project.GetUnityFrameworkTargetGuid();
			string unityMainTargetGuid = project.GetUnityMainTargetGuid();

			// Ensure Swift module is defined
			project.AddBuildProperty(unityFrameworkGuid, "DEFINES_MODULE", "YES");

			// iOS 13+ for SwiftUI
			project.SetBuildProperty(unityMainTargetGuid, "IPHONEOS_DEPLOYMENT_TARGET", "13.0");
			project.SetBuildProperty(unityFrameworkGuid, "IPHONEOS_DEPLOYMENT_TARGET", "13.0");

			// Copy modulemap if missing and set path
			string dstModuleDir = Path.Combine(buildPath, "UnityFramework");
			Directory.CreateDirectory(dstModuleDir);
			string dstModuleFile = Path.Combine(dstModuleDir, "UnityFramework.modulemap");
			if (!File.Exists(dstModuleFile))
			{
				string srcModuleFile = FindModuleMapSource();
				if (!string.IsNullOrEmpty(srcModuleFile) && File.Exists(srcModuleFile))
				{
					FileUtil.CopyFileOrDirectory(srcModuleFile, dstModuleFile);
					if (project.FindFileGuidByRealPath(dstModuleFile) == null)
					{
						project.AddFile(dstModuleFile, "UnityFramework/UnityFramework.modulemap");
					}
				}
				else
				{
					Debug.LogWarning("CustomViewPostProcess: Could not locate UnityFramework.modulemap in project assets/packages.");
				}
			}
			if (File.Exists(dstModuleFile))
			{
				project.SetBuildProperty(unityFrameworkGuid, "MODULEMAP_FILE", "$(SRCROOT)/UnityFramework/UnityFramework.modulemap");
			}
			else
			{
				Debug.LogWarning("CustomViewPostProcess: UnityFramework.modulemap not found; MODULEMAP_FILE will not be set.");
			}

			// Make Unity headers public for Swift
			MakeHeaderPublicIfExists(project, unityFrameworkGuid, "Classes/Unity/UnityInterface.h");
			MakeHeaderPublicIfExists(project, unityFrameworkGuid, "Classes/Unity/UnityForwardDecls.h");
			MakeHeaderPublicIfExists(project, unityFrameworkGuid, "Classes/Unity/UnityRendering.h");
			MakeHeaderPublicIfExists(project, unityFrameworkGuid, "Classes/Unity/UnitySharedDecls.h");

			// Add frameworks
			project.AddFrameworkToProject(unityFrameworkGuid, "SwiftUI.framework", false);
			project.AddFrameworkToProject(unityFrameworkGuid, "UIKit.framework", false);

			// Embed Swift standard libraries
			project.SetBuildProperty(unityMainTargetGuid, "ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES", "YES");

			project.WriteToFile(projectPath);
			Debug.Log("CustomViewPostProcess: iOS Xcode project configured.");
		}

		private static void MakeHeaderPublicIfExists(PBXProject project, string targetGuid, string projectRelativePath)
		{
			string guid = project.FindFileGuidByProjectPath(projectRelativePath);
			if (!string.IsNullOrEmpty(guid))
			{
				project.AddPublicHeaderToBuild(targetGuid, guid);
			}
		}

		private static string FindModuleMapSource()
		{
			// Try to find the modulemap under Assets
			string assetsPath = Application.dataPath;
			if (Directory.Exists(assetsPath))
			{
				string[] files = Directory.GetFiles(assetsPath, "UnityFramework.modulemap", SearchOption.AllDirectories);
				if (files != null && files.Length > 0)
				{
					// Prefer the one under Plugins/iOS
					string preferred = files.FirstOrDefault(f => f.Replace("\\", "/").Contains("/Plugins/iOS/"));
					return preferred ?? files[0];
				}
			}

			// Also try Packages (if imported as UPM package)
			string packagesPath = Path.GetFullPath("Packages");
			if (Directory.Exists(packagesPath))
			{
				string[] files = Directory.GetFiles(packagesPath, "UnityFramework.modulemap", SearchOption.AllDirectories);
				if (files != null && files.Length > 0)
				{
					return files[0];
				}
			}

			// Try Library/PackageCache
			string packageCache = Path.Combine(Directory.GetCurrentDirectory(), "Library/PackageCache");
			if (Directory.Exists(packageCache))
			{
				string[] files = Directory.GetFiles(packageCache, "UnityFramework.modulemap", SearchOption.AllDirectories);
				if (files != null && files.Length > 0)
				{
					return files[0];
				}
			}

			// Fallback: this package path
			string local = Path.Combine(Directory.GetCurrentDirectory(), "Packages/com.ursula.unity-ios-lib-custom-view/Plugins/iOS/Source/UnityFramework.modulemap");
			if (File.Exists(local))
			{
				return local;
			}

			return null;
		}
	}
}
#endif
 


