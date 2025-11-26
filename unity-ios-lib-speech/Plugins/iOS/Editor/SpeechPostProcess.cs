#if UNITY_IOS
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEngine;

namespace CoPicnic.IOS.Speech.Editor
{
	public static class SpeechPostProcess
	{
		[PostProcessBuild(102)]
		public static void OnPostProcessBuild(BuildTarget buildTarget, string buildPath)
		{
			if (buildTarget != BuildTarget.iOS) return;

			Debug.Log("SpeechPostProcess: Configuring Xcode project for Speech...");

			ConfigureXcodeProject(buildPath);
			ConfigureInfoPlist(buildPath);

			Debug.Log("SpeechPostProcess: Configuration complete.");
		}

		private static void ConfigureXcodeProject(string buildPath)
		{
			string projectPath = PBXProject.GetPBXProjectPath(buildPath);
			var project = new PBXProject();
			project.ReadFromFile(projectPath);

			string frameworkGuid = project.GetUnityFrameworkTargetGuid();
			string mainGuid = project.GetUnityMainTargetGuid();

			// Add frameworks
			project.AddFrameworkToProject(frameworkGuid, "Speech.framework", false);
			project.AddFrameworkToProject(frameworkGuid, "AVFoundation.framework", false);

			// Swift support
			project.AddBuildProperty(frameworkGuid, "DEFINES_MODULE", "YES");
			project.SetBuildProperty(mainGuid, "ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES", "YES");

			// Modulemap (idempotent)
			string dstModuleDir = Path.Combine(buildPath, "UnityFramework");
			Directory.CreateDirectory(dstModuleDir);
			string dstModuleFile = Path.Combine(dstModuleDir, "UnityFramework.modulemap");
			
			if (!File.Exists(dstModuleFile))
			{
				string pluginModule = Path.Combine(
					Directory.GetCurrentDirectory(),
					"Assets",
					"Plugins",
					"iOS",
					"Source",
					"UnityFramework.modulemap"
				);
				
				if (File.Exists(pluginModule))
				{
					FileUtil.CopyFileOrDirectory(pluginModule, dstModuleFile);
					Debug.Log("SpeechPostProcess: Copied modulemap");
				}
			}
			
			// Set modulemap path (idempotent)
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

			plist.root.SetString("NSSpeechRecognitionUsageDescription",
				"This app needs access to speech recognition to transcribe your voice.");
			
			plist.root.SetString("NSMicrophoneUsageDescription",
				"This app needs access to your microphone to recognize your speech.");

			plist.WriteToFile(plistPath);
		}
	}
}
#endif
