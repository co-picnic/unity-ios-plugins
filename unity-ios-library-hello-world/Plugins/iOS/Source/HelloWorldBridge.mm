#import <Foundation/Foundation.h>
#import <UnityFramework/UnityFramework-Swift.h>
#import "UnityInterface.h"
#include <stdlib.h>
#include <string.h>

extern "C"
{
	static char* cStringCopy(const char* string)
	{
		if (string == NULL)
		{
			return NULL;
		}
		size_t length = strlen(string) + 1;
		char* res = (char*)malloc(length);
		if (res != NULL)
		{
			memcpy(res, string, length);
		}
		return res;
	}

	char* helloWorldGetMessage()
	{
		NSString* returnString = [[HelloWorld shared] sayHello];
		return cStringCopy([returnString UTF8String]);
	}

	void helloWorldFreeString(char* str)
	{
		if (str != NULL)
		{
			free(str);
		}
	}

	void helloWorldSendToUnity(const char* gameObjectName, const char* methodName, const char* message)
	{
		UnitySendMessage(gameObjectName, methodName, message ? message : "");
	}
}


