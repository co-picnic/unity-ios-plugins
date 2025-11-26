import Foundation
import UIKit
import SwiftUI

@objc public class ViewManager: NSObject {
	
	// Singleton
	@objc public static let shared = ViewManager()
	
	private var overlayViewController: UIViewController?
	private var keyboardResponder: KeyboardResponder?
	
	private override init() {
		super.init()
	}
	
	// MARK: - View Presentation
	
	@objc public func showCustomView() -> Bool {
		guard let rootVC = getRootViewController() else {
			sendError("Could not find root view controller")
			return false
		}
		
		// Dismiss existing overlay if present
		dismissCustomView()
		
		DispatchQueue.main.async { [weak self] in
			guard let self = self else { return }
			
			// Create SwiftUI view
			let customView = CustomView(
				onTextSubmitted: { text in
					self.sendTextSubmitted(text)
				},
				onButtonTapped: { buttonId in
					self.sendButtonTapped(buttonId)
				},
				onDismiss: {
					self.dismissCustomView()
				}
			)
			
			// Wrap in UIHostingController
			let hostingController = TransparentHostingController(rootView: customView)
			
			// Configure presentation
			hostingController.modalPresentationStyle = .overFullScreen
			hostingController.modalTransitionStyle = .crossDissolve
			
			self.overlayViewController = hostingController
			
			// Present
			rootVC.present(hostingController, animated: true)
			
			// Setup keyboard monitoring
			self.keyboardResponder = KeyboardResponder()
		}
		
		return true
	}
	
	@objc public func dismissCustomView() {
		guard overlayViewController != nil else { return }
		
		DispatchQueue.main.async { [weak self] in
			guard let self = self else { return }
			
			self.overlayViewController?.dismiss(animated: true) {
				self.overlayViewController = nil
				self.keyboardResponder = nil
			}
		}
	}
	
	@objc public func isViewVisible() -> Bool {
		return overlayViewController != nil
	}
	
	@objc public func setText(_ text: String) {
		// TODO: Pass text to SwiftUI view via @Published property
		// For now, text must be set on view creation
	}
	
	// MARK: - Private Helpers
	
	private func getRootViewController() -> UIViewController? {
		// Try Unity's view controller
		if let unityVC = UnityGetGLViewController() {
			return unityVC
		}
		
		// Fallback to key window
		if let windowScene = UIApplication.shared.connectedScenes.first as? UIWindowScene,
		   let window = windowScene.windows.first(where: { $0.isKeyWindow }),
		   let rootVC = window.rootViewController {
			return rootVC
		}
		
		return nil
	}
	
	// MARK: - Unity Callbacks
	
	private func sendTextSubmitted(_ text: String) {
		UnitySendMessage("CustomViewCallbackObject", "OnTextSubmitted", text)
	}
	
	private func sendButtonTapped(_ buttonId: String) {
		UnitySendMessage("CustomViewCallbackObject", "OnButtonTapped", buttonId)
	}
	
	private func sendError(_ message: String) {
		UnitySendMessage("CustomViewCallbackObject", "OnViewError", message)
	}
}

// MARK: - Transparent Hosting Controller

class TransparentHostingController<Content: View>: UIHostingController<Content> {
	
	override func viewDidLoad() {
		super.viewDidLoad()
		
		// Make background transparent
		view.backgroundColor = .clear
		
		// Allow touches to pass through to Unity where appropriate
		view.isOpaque = false
	}
}
 
