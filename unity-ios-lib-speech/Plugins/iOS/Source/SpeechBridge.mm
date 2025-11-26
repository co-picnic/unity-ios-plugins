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
	
	typedef void (*PermissionCallback)(bool granted);
	typedef void (*ReadyCallback)();
	typedef void (*PartialResultCallback)(const char* text);
	typedef void (*FinalResultCallback)(const char* text);
	typedef void (*ErrorCallback)(const char* message);
	typedef void (*CancelledCallback)();
	
	static PermissionCallback g_permissionCallback = NULL;
	static ReadyCallback g_readyCallback = NULL;
	static PartialResultCallback g_partialResultCallback = NULL;
	static FinalResultCallback g_finalResultCallback = NULL;
	static ErrorCallback g_errorCallback = NULL;
	static CancelledCallback g_cancelledCallback = NULL;
	
	// Logging toggle
	void speechSetLoggingEnabled(bool enabled)
	{
		[SpeechRecognizer setLoggingEnabled:enabled];
	}
	
	// Continuous mode
	void speechSetContinuousMode(bool enabled)
	{
		[[SpeechRecognizer shared] setContinuousMode:enabled];
	}
	
	// Permission Management
	void speechRequestPermission(PermissionCallback callback)
	{
		g_permissionCallback = callback;
		
		[[SpeechRecognizer shared] requestPermissionWithCompletion:^(BOOL granted) {
			if (g_permissionCallback != NULL) {
				g_permissionCallback(granted);
			}
		}];
	}
	
	bool speechHasPermission()
	{
		return [[SpeechRecognizer shared] hasPermission];
	}
	
	int speechCheckPermissionStatus()
	{
		return [[SpeechRecognizer shared] checkPermissionStatus];
	}
	
	// Recognition Control
	bool speechStartRecognition(const char* localeCode)
	{
		NSString* locale = localeCode ? [NSString stringWithUTF8String:localeCode] : @"en-US";
		return [[SpeechRecognizer shared] startRecognitionWithLocaleCode:locale];
	}
	
	void speechStopRecognition()
	{
		[[SpeechRecognizer shared] stopRecognition];
	}
	
	void speechCancelRecognition()
	{
		[[SpeechRecognizer shared] cancelRecognition];
	}
	
	bool speechIsRecognitionActive()
	{
		return [[SpeechRecognizer shared] isRecognitionActive];
	}
	
	// Locale Support
	char* speechGetSupportedLocales()
	{
		NSArray<NSString*>* locales = [[SpeechRecognizer shared] getSupportedLocales];
		NSString* joinedLocales = [locales componentsJoinedByString:@","];
		return cStringCopy([joinedLocales UTF8String]);
	}
	
	bool speechIsLocaleSupported(const char* localeCode)
	{
		NSString* locale = localeCode ? [NSString stringWithUTF8String:localeCode] : @"en-US";
		return [[SpeechRecognizer shared] isLocaleSupported:locale];
	}
	
	// Route/Interruption
	void speechHandleRouteChange()
	{
		[[SpeechRecognizer shared] handleRouteChange];
	}
	
	void speechHandleInterruption(bool began)
	{
		[[SpeechRecognizer shared] handleInterruptionWithBegan:began];
	}
	
	// Callback Registration
	void speechSetReadyCallback(ReadyCallback callback)
	{
		g_readyCallback = callback;
	}
	
	void speechSetPartialResultCallback(PartialResultCallback callback)
	{
		g_partialResultCallback = callback;
	}
	
	void speechSetFinalResultCallback(FinalResultCallback callback)
	{
		g_finalResultCallback = callback;
	}
	
	void speechSetErrorCallback(ErrorCallback callback)
	{
		g_errorCallback = callback;
	}
	
	void speechSetCancelledCallback(CancelledCallback callback)
	{
		g_cancelledCallback = callback;
	}
}

// Unity Callback Implementations (called from Swift)
extern "C" {
void speechReady()
{
	if (g_readyCallback != NULL) {
		g_readyCallback();
	}
}

void speechPartialResult(const char* text)
{
	if (g_partialResultCallback != NULL) {
		g_partialResultCallback(text);
	}
}

void speechFinalResult(const char* text)
{
	if (g_finalResultCallback != NULL) {
		g_finalResultCallback(text);
	}
}

void speechError(const char* message)
{
	if (g_errorCallback != NULL) {
		g_errorCallback(message);
	}
}

void speechCancelled()
{
	if (g_cancelledCallback != NULL) {
		g_cancelledCallback();
		}
	}
}


