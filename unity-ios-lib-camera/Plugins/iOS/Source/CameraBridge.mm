#import <Foundation/Foundation.h>
#import <UnityFramework/UnityFramework-Swift.h>
#import "UnityInterface.h"
#include <stdlib.h>
#include <string.h>

extern "C"
{
	static char* cStringCopy(const char* string)
	{
		if (string == NULL) return NULL;
		size_t length = strlen(string) + 1;
		char* res = (char*)malloc(length);
		if (res != NULL) memcpy(res, string, length);
		return res;
	}
	
	typedef void (*PermissionCallback)(bool);
	static PermissionCallback g_permissionCallback = NULL;
	
	void cameraSetLoggingEnabled(bool enabled)
	{
		[CameraManager setLoggingEnabled:enabled];
	}
	
	void cameraSetPermissionCallback(PermissionCallback callback)
	{
		g_permissionCallback = callback;
	}
	
	void cameraRequestPermission()
	{
		[[CameraManager shared] requestPermissionWithCompletion:^(BOOL granted) {
			if (g_permissionCallback != NULL) {
				g_permissionCallback(granted);
			}
		}];
	}
	
	int cameraCheckPermission()
	{
		return (int)[[CameraManager shared] checkPermission];
	}
	
	bool cameraIsAvailable()
	{
		return [[CameraManager shared] isCameraAvailable];
	}
	
	bool cameraOpen(int maxSize, int quality)
	{
		return [[CameraManager shared] openCameraWithMaxSize:maxSize quality:quality];
	}
	
	bool cameraOpenPhotoLibrary(int maxSize, int quality)
	{
		return [[CameraManager shared] openPhotoLibraryWithMaxSize:maxSize quality:quality];
	}
	
	void cameraDismiss()
	{
		[[CameraManager shared] dismissPicker];
	}
}


