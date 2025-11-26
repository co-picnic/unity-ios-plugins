#import <Foundation/Foundation.h>
#import <UnityFramework/UnityFramework-Swift.h>
#import "UnityInterface.h"

extern "C"
{
	// View Management
	
	bool customViewShow()
	{
		return [[ViewManager shared] showCustomView];
	}
	
	void customViewDismiss()
	{
		[[ViewManager shared] dismissCustomView];
	}
	
	bool customViewIsVisible()
	{
		return [[ViewManager shared] isViewVisible];
	}
	
	void customViewSetText(const char* text)
	{
		NSString* nsText = [NSString stringWithUTF8String:text ?: ""];
		[[ViewManager shared] setText:nsText];
	}
}



