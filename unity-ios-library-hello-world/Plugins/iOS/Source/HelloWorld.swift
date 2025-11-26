import Foundation

@objc public class HelloWorld: NSObject {
	@objc public static let shared = HelloWorld()

	@objc public func sayHello() -> String {
		return "Hello from Swift!"
	}

	@objc public func sendMessageToUnity(_ gameObject: String, method: String, message: String) {
		UnitySendMessage(gameObject, method, message)
	}
}


