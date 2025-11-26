import SwiftUI

struct CustomView: View {
	
	@State private var inputText: String = ""
	@FocusState private var isTextFieldFocused: Bool
	
	var onTextSubmitted: (String) -> Void
	var onButtonTapped: (String) -> Void
	var onDismiss: () -> Void
	
	var body: some View {
		ZStack {
			// Semi-transparent background (allows seeing Unity beneath)
			Color.black.opacity(0.5)
				.ignoresSafeArea()
				.onTapGesture {
					// Dismiss keyboard on background tap
					isTextFieldFocused = false
				}
			
			VStack(spacing: 20) {
				Spacer()
				
				// Main content card
				VStack(spacing: 16) {
					Text("Enter Text")
						.font(.headline)
						.foregroundColor(.primary)
					
					// Text input
					TextField("Type something...", text: $inputText)
						.textFieldStyle(.roundedBorder)
						.focused($isTextFieldFocused)
						.submitLabel(.done)
						.onSubmit {
							handleSubmit()
						}
					
					HStack(spacing: 12) {
						// Submit button
						Button(action: handleSubmit) {
							Text("Submit")
								.font(.body.weight(.semibold))
								.foregroundColor(.white)
								.frame(maxWidth: .infinity)
								.padding(.vertical, 12)
								.background(Color.blue)
								.cornerRadius(10)
						}
						
						// Cancel button
						Button(action: handleCancel) {
							Text("Cancel")
								.font(.body.weight(.semibold))
								.foregroundColor(.white)
								.frame(maxWidth: .infinity)
								.padding(.vertical, 12)
								.background(Color.gray)
								.cornerRadius(10)
						}
					}
				}
				.padding(24)
				.background(
					RoundedRectangle(cornerRadius: 16)
						.fill(Color(UIColor.systemBackground))
						.shadow(color: .black.opacity(0.2), radius: 10, x: 0, y: 4)
				)
				.padding(.horizontal, 32)
				
				Spacer()
			}
		}
		.onAppear {
			// Auto-focus text field when view appears
			DispatchQueue.main.asyncAfter(deadline: .now() + 0.5) {
				isTextFieldFocused = true
			}
		}
	}
	
	private func handleSubmit() {
		guard !inputText.isEmpty else { return }
		
		isTextFieldFocused = false
		onTextSubmitted(inputText)
		
		// Dismiss after short delay
		DispatchQueue.main.asyncAfter(deadline: .now() + 0.3) {
			onDismiss()
		}
	}
	
	private func handleCancel() {
		isTextFieldFocused = false
		onButtonTapped("cancel")
		onDismiss()
	}
}
 

