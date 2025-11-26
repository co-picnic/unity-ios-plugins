#if UNITY_IOS
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEngine;

namespace CoPicnic.IOS.Camera.Editor
{
	public static class CameraPostProcess
	{
		[PostProcessBuild(101)]
		public static void OnPostProcessBuild(BuildTarget buildTarget, string buildPath)
		{
			if (buildTarget != BuildTarget.iOS) return;

			Debug.Log("CameraPostProcess: Configuring Xcode project for Camera...");

			ConfigureXcodeProject(buildPath);
			ConfigureInfoPlist(buildPath);

			Debug.Log("CameraPostProcess: Configuration complete.");
		}

		private static void ConfigureXcodeProject(string buildPath)
		{
			string projectPath = PBXProject.GetPBXProjectPath(buildPath);
			var project = new PBXProject();
			project.ReadFromFile(projectPath);

			string frameworkGuid = project.GetUnityFrameworkTargetGuid();
			string mainGuid = project.GetUnityMainTargetGuid();

			project.AddFrameworkToProject(frameworkGuid, "AVFoundation.framework", false);
			project.AddFrameworkToProject(frameworkGuid, "Photos.framework", false);

			project.AddBuildProperty(frameworkGuid, "DEFINES_MODULE", "YES");
			project.SetBuildProperty(mainGuid, "ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES", "YES");

			// MODULEMAP_FILE if our modulemap exists (idempotent)
			string dstModuleDir = Path.Combine(buildPath, "UnityFramework");
			Directory.CreateDirectory(dstModuleDir);
			string dstModuleFile = Path.Combine(dstModuleDir, "UnityFramework.modulemap");
			if (!File.Exists(dstModuleFile))
			{
				string pluginModule = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "Plugins", "iOS", "Source", "UnityFramework.modulemap");
				if (File.Exists(pluginModule))
				{
					FileUtil.CopyFileOrDirectory(pluginModule, dstModuleFile);
					Debug.Log("CameraPostProcess: Copied modulemap");
				}
			}
			string currentModulemapPath = project.GetBuildPropertyForConfig(frameworkGuid, "MODULEMAP_FILE");
			if (string.IsNullOrEmpty(currentModulemapPath))
			{
				project.SetBuildProperty(frameworkGuid, "MODULEMAP_FILE", "$(SRCROOT)/UnityFramework/UnityFramework.modulemap");
			}

			project.WriteToFile(projectPath);
		}

		private static void ConfigureInfoPlist(string buildPath)
		{
			string plistPath = Path.Combine(buildPath, "Info.plist");
			var plist = new PlistDocument();
			plist.ReadFromFile(plistPath);

			plist.root.SetString("NSCameraUsageDescription",
				"This app needs access to your camera to take photos.");
			
			plist.root.SetString("NSPhotoLibraryUsageDescription",
				"This app needs access to your photo library to select photos.");

			plist.WriteToFile(plistPath);
		}
	}
}
#endif




