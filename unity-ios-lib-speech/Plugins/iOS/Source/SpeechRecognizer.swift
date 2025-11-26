import Foundation
import Speech
import AVFoundation

@objc public class SpeechRecognizer: NSObject {
	
	@objc public static let shared = SpeechRecognizer()
	
	private var speechRecognizer: SFSpeechRecognizer?
	private var audioEngine = AVAudioEngine()
	private var recognitionRequest: SFSpeechAudioBufferRecognitionRequest?
	private var recognitionTask: SFSpeechRecognitionTask?
	
	private var isRecognizing = false
	private var currentLocale: Locale?
	private var continuousMode = false
	
	private static var loggingEnabled = true
	
	private override init() {
		super.init()
	}
	
	@objc public static func setLoggingEnabled(_ enabled: Bool) {
		loggingEnabled = enabled
	}
	
	@objc public func setContinuousMode(_ enabled: Bool) {
		continuousMode = enabled
		log("Continuous mode set to \(enabled)")
	}
	
	private func log(_ message: String) {
		guard SpeechRecognizer.loggingEnabled else { return }
		NSLog("[CoPicnic.iOS.Speech] \(message)")
	}
	
	// MARK: - Permission Management
	
	@objc public func requestPermission(completion: @escaping (Bool) -> Void) {
		SFSpeechRecognizer.requestAuthorization { authStatus in
			let speechGranted = authStatus == .authorized
			
			AVAudioSession.sharedInstance().requestRecordPermission { micGranted in
				let isAuthorized = speechGranted && micGranted
				
				DispatchQueue.main.async {
					self.log("Permission result: speech=\(speechGranted), mic=\(micGranted)")
					completion(isAuthorized)
				}
			}
		}
	}
	
	@objc public func hasPermission() -> Bool {
		let speechStatus = SFSpeechRecognizer.authorizationStatus()
		let audioStatus = AVAudioSession.sharedInstance().recordPermission
		return speechStatus == .authorized && audioStatus == .granted
	}
	
	@objc public func checkPermissionStatus() -> Int {
		let speechStatus = SFSpeechRecognizer.authorizationStatus()
		
		switch speechStatus {
		case .authorized:
			let audioStatus = AVAudioSession.sharedInstance().recordPermission
			if audioStatus == .granted {
				return 1 // Granted
			} else {
				return -1 // Speech OK but mic denied
			}
		case .denied, .restricted:
			return -1 // Denied
		case .notDetermined:
			return 0 // Not requested
		@unknown default:
			return -1
		}
	}
	
	// MARK: - Recognition Control
	
	@objc public func startRecognition(localeCode: String) -> Bool {
		if isRecognizing {
			log("Already recognizing")
			return false
		}
		
		guard hasPermission() else {
			log("Permission not granted")
			speechError("Permission not granted")
			return false
		}
		
		let locale = Locale(identifier: localeCode)
		currentLocale = locale
		
		speechRecognizer = SFSpeechRecognizer(locale: locale)
		
		guard let speechRecognizer = speechRecognizer, speechRecognizer.isAvailable else {
			log("Speech recognizer not available for locale: \(localeCode)")
			speechError("Speech recognizer not available for selected language")
			return false
		}
		
		recognitionRequest = SFSpeechAudioBufferRecognitionRequest()
		
		guard let recognitionRequest = recognitionRequest else {
			log("Could not create recognition request")
			speechError("Could not create recognition request")
			return false
		}
		
		recognitionRequest.shouldReportPartialResults = true
		
		recognitionTask = speechRecognizer.recognitionTask(with: recognitionRequest) { [weak self] result, error in
			guard let self = self else { return }
			
			if let error = error {
				self.log("Recognition error: \(error.localizedDescription)")
				speechError(error.localizedDescription)
				self.stopRecognition()
				return
			}
			
			if let result = result {
				let transcription = result.bestTranscription.formattedString
				let isFinal = result.isFinal
				
				if isFinal {
					self.log("Final: \(transcription)")
					transcription.withCString { ptr in
						speechFinalResult(ptr)
					}
				} else {
					self.log("Partial: \(transcription)")
					transcription.withCString { ptr in
						speechPartialResult(ptr)
					}
				}
				
				if isFinal && !self.continuousMode {
					self.stopRecognition()
				}
			}
		}
		
		let inputNode = audioEngine.inputNode
		let recordingFormat = inputNode.outputFormat(forBus: 0)
		
		inputNode.installTap(onBus: 0, bufferSize: 1024, format: recordingFormat) { buffer, _ in
			self.recognitionRequest?.append(buffer)
		}
		
		audioEngine.prepare()
		
		do {
			try audioEngine.start()
			isRecognizing = true
			
			log("Recognition started with locale: \(localeCode)")
			speechReady()
			
			return true
		} catch {
			log("Audio engine failed to start: \(error)")
			let msg = "Audio engine failed to start: \(error.localizedDescription)"
			msg.withCString { ptr in
				speechError(ptr)
			}
			return false
		}
	}
	
	@objc public func stopRecognition() {
		guard isRecognizing else { return }
		
		log("Stopping recognition")
		
		if audioEngine.isRunning {
			audioEngine.stop()
			audioEngine.inputNode.removeTap(onBus: 0)
		}
		
		recognitionRequest?.endAudio()
		recognitionTask?.cancel()
		
		recognitionRequest = nil
		recognitionTask = nil
		
		isRecognizing = false
		
		log("Recognition stopped")
	}
	
	@objc public func cancelRecognition() {
		guard isRecognizing else { return }
		
		log("Cancelling recognition")
		
		if audioEngine.isRunning {
			audioEngine.stop()
			audioEngine.inputNode.removeTap(onBus: 0)
		}
		
		recognitionTask?.cancel()
		recognitionRequest = nil
		recognitionTask = nil
		
		isRecognizing = false
		
		speechCancelled()
		
		log("Recognition cancelled")
	}
	
	@objc public func isRecognitionActive() -> Bool {
		return isRecognizing
	}
	
	// MARK: - Locale Support
	
	@objc public func getSupportedLocales() -> [String] {
		let locales = SFSpeechRecognizer.supportedLocales()
		return locales.map { $0.identifier }.sorted()
	}
	
	@objc public func isLocaleSupported(_ localeCode: String) -> Bool {
		let locale = Locale(identifier: localeCode)
		let recognizer = SFSpeechRecognizer(locale: locale)
		return recognizer?.isAvailable ?? false
	}
	
	// MARK: - Route/Interruption Handling
	
	@objc public func handleRouteChange() {
		if isRecognizing {
			log("Audio route changed, restarting engine")
			
			let wasRecognizing = isRecognizing
			let locale = currentLocale?.identifier ?? "en-US"
			
			stopRecognition()
			
			if wasRecognizing {
				DispatchQueue.main.asyncAfter(deadline: .now() + 0.5) { [weak self] in
					_ = self?.startRecognition(localeCode: locale)
				}
			}
		}
	}
	
	@objc public func handleInterruption(began: Bool) {
		if began {
			log("Audio interrupted, stopping recognition")
			if isRecognizing {
				stopRecognition()
			}
		}
	}
}

// MARK: - Unity Callbacks (declared in bridge)

@_silgen_name("speechReady")
func speechReady()

@_silgen_name("speechPartialResult")
func speechPartialResult(_ text: UnsafePointer<CChar>)

@_silgen_name("speechFinalResult")
func speechFinalResult(_ text: UnsafePointer<CChar>)

@_silgen_name("speechError")
func speechError(_ message: UnsafePointer<CChar>)

@_silgen_name("speechCancelled")
func speechCancelled()


