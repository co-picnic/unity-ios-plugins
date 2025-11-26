#if UNITY_IOS
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEngine;
using UnityEditor.PackageManager;

namespace Ursula.IOS.HelloWorld.Editor
{
	public static class HelloWorldPostProcess
	{
		[PostProcessBuild]
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
					Debug.LogWarning("HelloWorldPostProcess: Could not locate UnityFramework.modulemap in project assets. Swift interop may fail.");
				}
			}
			if (File.Exists(dstModuleFile))
			{
				project.SetBuildProperty(unityFrameworkGuid, "MODULEMAP_FILE", "$(SRCROOT)/UnityFramework/UnityFramework.modulemap");
			}
			else
			{
				Debug.LogWarning("HelloWorldPostProcess: UnityFramework.modulemap not found; MODULEMAP_FILE will not be set.");
			}

			// Make Unity headers public for Swift
			MakeHeaderPublicIfExists(project, unityFrameworkGuid, "Classes/Unity/UnityInterface.h");
			MakeHeaderPublicIfExists(project, unityFrameworkGuid, "Classes/Unity/UnityForwardDecls.h");
			MakeHeaderPublicIfExists(project, unityFrameworkGuid, "Classes/Unity/UnityRendering.h");
			MakeHeaderPublicIfExists(project, unityFrameworkGuid, "Classes/Unity/UnitySharedDecls.h");

			// Embed Swift standard libraries
			project.SetBuildProperty(unityMainTargetGuid, "ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES", "YES");

			project.WriteToFile(projectPath);
			Debug.Log("HelloWorldPostProcess: iOS Xcode project configured.");
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
			// Try to find the modulemap under Assets (works when plugin resides in Assets/)
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

			// Try resolve via PackageManager (handles file: and external package paths)
			try
			{
				var pkgInfo = UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/com.ursula.unity-ios-library-hello-world");
				if (pkgInfo != null && !string.IsNullOrEmpty(pkgInfo.resolvedPath) && Directory.Exists(pkgInfo.resolvedPath))
				{
					string candidate = Path.Combine(pkgInfo.resolvedPath, "Plugins/iOS/Source/UnityFramework.modulemap");
					if (File.Exists(candidate))
					{
						return candidate;
					}
					string[] files = Directory.GetFiles(pkgInfo.resolvedPath, "UnityFramework.modulemap", SearchOption.AllDirectories);
					if (files != null && files.Length > 0)
					{
						return files[0];
					}
				}
			}
			catch
			{
				// ignored
			}

			// Finally scan Library/PackageCache (git/registry cache)
			string packageCache = Path.Combine(Directory.GetCurrentDirectory(), "Library/PackageCache");
			if (Directory.Exists(packageCache))
			{
				string[] files = Directory.GetFiles(packageCache, "UnityFramework.modulemap", SearchOption.AllDirectories);
				if (files != null && files.Length > 0)
				{
					return files[0];
				}
			}

			return null;
		}
	}
}
#endif


