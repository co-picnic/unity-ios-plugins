import Foundation
import UIKit
import AVFoundation

@objc public class CameraManager: NSObject {
	
	@objc public static let shared = CameraManager()
	
	private var pickerController: UIImagePickerController?
	private weak var presentingViewController: UIViewController?
	
	private static var loggingEnabled = true
	
	private override init() {
		super.init()
	}
	
	@objc public static func setLoggingEnabled(_ enabled: Bool) {
		loggingEnabled = enabled
	}
	
	private func log(_ message: String) {
		guard CameraManager.loggingEnabled else { return }
		NSLog("[CoPicnic.iOS.Camera] \(message)")
	}
	
	// MARK: - Permission Management
	
	@objc public func checkPermission() -> Int {
		let status = AVCaptureDevice.authorizationStatus(for: .video)
		switch status {
		case .authorized:
			return 1 // Granted
		case .denied, .restricted:
			return -1 // Denied
		case .notDetermined:
			return 0 // Not requested
		@unknown default:
			return -1
		}
	}
	
	@objc public func requestPermission(completion: @escaping (Bool) -> Void) {
		AVCaptureDevice.requestAccess(for: .video) { granted in
			DispatchQueue.main.async {
				self.log("Permission result: \(granted)")
				completion(granted)
			}
		}
	}
	
	@objc public func isCameraAvailable() -> Bool {
		return UIImagePickerController.isSourceTypeAvailable(.camera)
	}
	
	// MARK: - Camera Capture
	
	@objc public func openCamera(maxSize: Int, quality: Int) -> Bool {
		guard isCameraAvailable() else {
			sendError("Camera not available on this device")
			return false
		}
		
		guard checkPermission() == 1 else {
			sendError("Camera permission not granted")
			return false
		}
		
		guard let rootVC = getRootViewController() else {
			sendError("Could not find root view controller")
			return false
		}
		
		DispatchQueue.main.async { [weak self] in
			guard let self = self else { return }
			
			let picker = UIImagePickerController()
			picker.sourceType = .camera
			picker.cameraDevice = .front
			picker.allowsEditing = true
			picker.delegate = self
			
			picker.view.tag = maxSize
			picker.view.accessibilityHint = "\(quality)"
			
			self.pickerController = picker
			self.presentingViewController = rootVC
			
			self.log("Presenting camera")
			rootVC.present(picker, animated: true)
		}
		
		return true
	}
	
	@objc public func openPhotoLibrary(maxSize: Int, quality: Int) -> Bool {
		guard UIImagePickerController.isSourceTypeAvailable(.photoLibrary) else {
			sendError("Photo library not available")
			return false
		}
		
		guard let rootVC = getRootViewController() else {
			sendError("Could not find root view controller")
			return false
		}
		
		DispatchQueue.main.async { [weak self] in
			guard let self = self else { return }
			
			let picker = UIImagePickerController()
			picker.sourceType = .photoLibrary
			picker.allowsEditing = true
			picker.delegate = self
			
			picker.view.tag = maxSize
			picker.view.accessibilityHint = "\(quality)"
			
			self.pickerController = picker
			self.presentingViewController = rootVC
			
			self.log("Presenting photo library")
			rootVC.present(picker, animated: true)
		}
		
		return true
	}
	
	@objc public func dismissPicker() {
		DispatchQueue.main.async { [weak self] in
			self?.presentingViewController?.dismiss(animated: true)
			self?.pickerController = nil
		}
	}
	
	// MARK: - Private Helpers
	
	private func getRootViewController() -> UIViewController? {
		// Base candidate: Unity VC or key window root
		var baseVC: UIViewController?
		if let windowScene = UIApplication.shared.connectedScenes.first as? UIWindowScene,
		   let window = windowScene.windows.first(where: { $0.isKeyWindow }) {
			baseVC = window.rootViewController
		} else if let window = UIApplication.shared.windows.first(where: { $0.isKeyWindow }) {
			baseVC = window.rootViewController
		}
		// Walk to the top-most presented VC to ensure we present from the visible controller
		var topVC = baseVC
		while let presented = topVC?.presentedViewController {
			topVC = presented
		}
		return topVC ?? baseVC
	}
	
	private func processAndSaveImage(_ image: UIImage, maxSize: Int, quality: Int) -> String? {
		var processedImage = image
		if maxSize > 0 {
			processedImage = resizeImage(image, maxSize: CGFloat(maxSize))
		}
		let compressionQuality = CGFloat(quality) / 100.0
		guard let imageData = processedImage.jpegData(compressionQuality: compressionQuality) else {
			return nil
		}
		let tempDir = NSTemporaryDirectory()
		let fileName = "camera_\(UUID().uuidString).jpg"
		let filePath = (tempDir as NSString).appendingPathComponent(fileName)
		do {
			try imageData.write(to: URL(fileURLWithPath: filePath))
			return filePath
		} catch {
			print("CoPicnic.iOS.Camera: Failed to save image: \(error)")
			return nil
		}
	}
	
	private func resizeImage(_ image: UIImage, maxSize: CGFloat) -> UIImage {
		let size = image.size
		let maxDimension = max(size.width, size.height)
		if maxDimension <= maxSize {
			return image
		}
		let scale = maxSize / maxDimension
		let newSize = CGSize(width: size.width * scale, height: size.height * scale)
		UIGraphicsBeginImageContextWithOptions(newSize, false, 1.0)
		image.draw(in: CGRect(origin: .zero, size: newSize))
		let resizedImage = UIGraphicsGetImageFromCurrentImageContext()
		UIGraphicsEndImageContext()
		return resizedImage ?? image
	}
	
	// MARK: - Unity Callbacks
	
	private func sendImageCaptured(_ filePath: String) {
		DispatchQueue.main.async {
			UnitySendMessage("CameraCallbackObject", "OnImageCaptured", filePath)
		}
	}
	
	private func sendCancelled() {
		DispatchQueue.main.async {
			UnitySendMessage("CameraCallbackObject", "OnCaptureCancelled", "")
		}
	}
	
	private func sendError(_ message: String) {
		DispatchQueue.main.async {
			UnitySendMessage("CameraCallbackObject", "OnCaptureError", message)
		}
	}
}

extension CameraManager: UIImagePickerControllerDelegate, UINavigationControllerDelegate {
	
	public func imagePickerController(_ picker: UIImagePickerController,
	                                 didFinishPickingMediaWithInfo info: [UIImagePickerController.InfoKey : Any]) {
		let image = (info[.editedImage] as? UIImage) ?? (info[.originalImage] as? UIImage)
		guard let finalImage = image else {
			sendError("Failed to get image from picker")
			dismissPicker()
			return
		}
		let maxSize = picker.view.tag
		let quality = Int(picker.view.accessibilityHint ?? "85") ?? 85
		if let filePath = processAndSaveImage(finalImage, maxSize: maxSize, quality: quality) {
			sendImageCaptured(filePath)
		} else {
			sendError("Failed to save image")
		}
		dismissPicker()
	}
	
	public func imagePickerControllerDidCancel(_ picker: UIImagePickerController) {
		sendCancelled()
		dismissPicker()
	}
}


