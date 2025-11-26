import Foundation
import UIKit
import Combine

class KeyboardResponder: ObservableObject {
	
	private var cancellables = Set<AnyCancellable>()
	
	@Published var isKeyboardVisible: Bool = false
	@Published var keyboardHeight: CGFloat = 0
	
	init() {
		// Observe keyboard show
		NotificationCenter.default.publisher(for: UIResponder.keyboardWillShowNotification)
			.compactMap { notification -> CGFloat? in
				guard let keyboardSize = (notification.userInfo?[UIResponder.keyboardFrameEndUserInfoKey] as? NSValue)?.cgRectValue else {
					return nil
				}
				return keyboardSize.height
			}
			.sink(receiveValue: { [weak self] keyboardHeight in
				self?.keyboardHeight = keyboardHeight
				self?.isKeyboardVisible = true
			})
			.store(in: &cancellables)
		
		// Observe keyboard hide
		NotificationCenter.default.publisher(for: UIResponder.keyboardWillHideNotification)
			.map { _ in 0 }
			.sink(receiveValue: { [weak self] _ in
				self?.keyboardHeight = 0
				self?.isKeyboardVisible = false
			})
			.store(in: &cancellables)
	}
	
	func hideKeyboard() {
		DispatchQueue.main.async {
			UIApplication.shared.sendAction(
				#selector(UIResponder.resignFirstResponder),
				to: nil,
				from: nil,
				for: nil
			)
		}
	}
}
 


