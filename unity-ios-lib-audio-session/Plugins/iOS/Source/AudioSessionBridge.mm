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
	
	typedef void (*RouteChangedCallback)(const char* reason, const char* routeJSON);
	typedef void (*InterruptionBeganCallback)();
	typedef void (*InterruptionEndedCallback)(bool shouldResume);
	typedef void (*SilenceHintCallback)(bool shouldSilence);
	
	static RouteChangedCallback g_routeChangedCallback = NULL;
	static InterruptionBeganCallback g_interruptionBeganCallback = NULL;
	static InterruptionEndedCallback g_interruptionEndedCallback = NULL;
	static SilenceHintCallback g_silenceHintCallback = NULL;
	
	// Logging toggle
	void audioSetLoggingEnabled(bool enabled)
	{
		[AudioSessionManager setLoggingEnabled:enabled];
	}
	
	// Session Configuration
	bool audioSessionSetup(const char* category, const char* mode, unsigned int options)
	{
		NSString* categoryStr = category ? [NSString stringWithUTF8String:category] : @"playback";
		NSString* modeStr = mode ? [NSString stringWithUTF8String:mode] : @"default";
		
		return [[AudioSessionManager shared] setupSessionWithCategory:categoryStr
																 mode:modeStr
															  options:options];
	}
	
	bool audioSessionActivate()
	{
		return [[AudioSessionManager shared] activateSession];
	}
	
	bool audioSessionDeactivate()
	{
		return [[AudioSessionManager shared] deactivateSession];
	}
	
	// State Queries
	float audioSessionGetOutputVolume()
	{
		return [[AudioSessionManager shared] getOutputVolume];
	}
	
	bool audioSessionIsOtherAudioPlaying()
	{
		return [[AudioSessionManager shared] isOtherAudioPlaying];
	}
	
	bool audioSessionIsMutedApprox()
	{
		return [[AudioSessionManager shared] isMutedApprox];
	}
	
	char* audioSessionGetCurrentRouteJSON()
	{
		NSString* routeJSON = [[AudioSessionManager shared] getCurrentRouteJSON];
		return cStringCopy([routeJSON UTF8String]);
	}
	
	// Notifications
	void audioSessionStartObservingRouteChanges()
	{
		[[AudioSessionManager shared] startObservingRouteChanges];
	}
	
	void audioSessionStopObservingRouteChanges()
	{
		[[AudioSessionManager shared] stopObservingRouteChanges];
	}
	
	void audioSessionStartObservingInterruptions()
	{
		[[AudioSessionManager shared] startObservingInterruptions];
	}
	
	void audioSessionStopObservingInterruptions()
	{
		[[AudioSessionManager shared] stopObservingInterruptions];
	}
	
	void audioSessionStartObservingSilenceHint()
	{
		[[AudioSessionManager shared] startObservingSilenceHint];
	}
	
	void audioSessionStopObservingSilenceHint()
	{
		[[AudioSessionManager shared] stopObservingSilenceHint];
	}
	
	// Callback Registration
	void audioSessionSetRouteChangedCallback(RouteChangedCallback callback)
	{
		g_routeChangedCallback = callback;
	}
	
	void audioSessionSetInterruptionBeganCallback(InterruptionBeganCallback callback)
	{
		g_interruptionBeganCallback = callback;
	}
	
	void audioSessionSetInterruptionEndedCallback(InterruptionEndedCallback callback)
	{
		g_interruptionEndedCallback = callback;
	}
	
	void audioSessionSetSilenceHintCallback(SilenceHintCallback callback)
	{
		g_silenceHintCallback = callback;
	}
}

// Unity Callback Implementations (called from Swift)
extern "C" {
	void audioSessionRouteChanged(const char* reason, const char* routeJSON)
	{
		if (g_routeChangedCallback != NULL) {
			g_routeChangedCallback(reason, routeJSON);
		}
	}
	
	void audioSessionInterruptionBegan()
	{
		if (g_interruptionBeganCallback != NULL) {
			g_interruptionBeganCallback();
		}
	}
	
	void audioSessionInterruptionEnded(bool shouldResume)
	{
		if (g_interruptionEndedCallback != NULL) {
			g_interruptionEndedCallback(shouldResume);
		}
	}
	
	void audioSessionSilenceHintChanged(bool shouldSilence)
	{
		if (g_silenceHintCallback != NULL) {
			g_silenceHintCallback(shouldSilence);
		}
	}
}


