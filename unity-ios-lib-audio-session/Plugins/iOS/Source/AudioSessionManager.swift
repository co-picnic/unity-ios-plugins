import Foundation
import AVFoundation

@objc public class AudioSessionManager: NSObject {
	
	@objc public static let shared = AudioSessionManager()
	
	private var routeChangeObserver: NSObjectProtocol?
	private var interruptionObserver: NSObjectProtocol?
	private var silenceHintObserver: NSObjectProtocol?
	
	private static var loggingEnabled = true
	
	private override init() {
		super.init()
	}
	
	@objc public static func setLoggingEnabled(_ enabled: Bool) {
		loggingEnabled = enabled
	}
	
	private func log(_ message: String) {
		guard AudioSessionManager.loggingEnabled else { return }
		NSLog("[CoPicnic.iOS.Audio] \(message)")
	}
	
	// MARK: - Session Configuration
	
	@objc public func setupSession(
		category: String,
		mode: String,
		options: UInt
	) -> Bool {
		let session = AVAudioSession.sharedInstance()
		
		do {
			let sessionCategory = mapCategory(category)
			let sessionMode = mapMode(mode)
			let sessionOptions = AVAudioSession.CategoryOptions(rawValue: options)
			
			try session.setCategory(sessionCategory, mode: sessionMode, options: sessionOptions)
			
			log("Configured: category=\(category), mode=\(mode), options=\(options)")
			return true
		} catch {
			log("Failed to configure session: \(error)")
			return false
		}
	}
	
	@objc public func activateSession() -> Bool {
		do {
			try AVAudioSession.sharedInstance().setActive(true)
			log("Activated")
			return true
		} catch {
			log("Failed to activate: \(error)")
			return false
		}
	}
	
	@objc public func deactivateSession() -> Bool {
		do {
			try AVAudioSession.sharedInstance().setActive(false, options: .notifyOthersOnDeactivation)
			log("Deactivated")
			return true
		} catch {
			log("Failed to deactivate: \(error)")
			return false
		}
	}
	
	// MARK: - State Queries
	
	@objc public func getOutputVolume() -> Float {
		return AVAudioSession.sharedInstance().outputVolume
	}
	
	@objc public func isOtherAudioPlaying() -> Bool {
		return AVAudioSession.sharedInstance().isOtherAudioPlaying
	}
	
	@objc public func isMutedApprox() -> Bool {
		let session = AVAudioSession.sharedInstance()
		let volumeIsZero = session.outputVolume == 0.0
		let shouldBeSilenced = session.secondaryAudioShouldBeSilencedHint
		return volumeIsZero || shouldBeSilenced
	}
	
	@objc public func getCurrentRouteJSON() -> String {
		let session = AVAudioSession.sharedInstance()
		let route = session.currentRoute
		
		var routeInfo: [String: Any] = [:]
		
		var inputs: [[String: String]] = []
		for input in route.inputs {
			inputs.append([
				"name": input.portName,
				"type": portTypeString(input.portType),
				"uid": input.uid
			])
		}
		
		var outputs: [[String: String]] = []
		for output in route.outputs {
			outputs.append([
				"name": output.portName,
				"type": portTypeString(output.portType),
				"uid": output.uid
			])
		}
		
		routeInfo["inputs"] = inputs
		routeInfo["outputs"] = outputs
		
		if let jsonData = try? JSONSerialization.data(withJSONObject: routeInfo, options: []),
		   let jsonString = String(data: jsonData, encoding: .utf8) {
			return jsonString
		}
		
		return "{}"
	}
	
	// MARK: - Notifications
	
	@objc public func startObservingRouteChanges() {
		stopObservingRouteChanges()
		
		routeChangeObserver = NotificationCenter.default.addObserver(
			forName: AVAudioSession.routeChangeNotification,
			object: nil,
			queue: .main
		) { [weak self] notification in
			self?.handleRouteChange(notification)
		}
		
		log("Started observing route changes")
	}
	
	@objc public func stopObservingRouteChanges() {
		if let observer = routeChangeObserver {
			NotificationCenter.default.removeObserver(observer)
			routeChangeObserver = nil
		}
	}
	
	@objc public func startObservingInterruptions() {
		stopObservingInterruptions()
		
		interruptionObserver = NotificationCenter.default.addObserver(
			forName: AVAudioSession.interruptionNotification,
			object: nil,
			queue: .main
		) { [weak self] notification in
			self?.handleInterruption(notification)
		}
		
		log("Started observing interruptions")
	}
	
	@objc public func stopObservingInterruptions() {
		if let observer = interruptionObserver {
			NotificationCenter.default.removeObserver(observer)
			interruptionObserver = nil
		}
	}
	
	@objc public func startObservingSilenceHint() {
		stopObservingSilenceHint()
		
		silenceHintObserver = NotificationCenter.default.addObserver(
			forName: AVAudioSession.silenceSecondaryAudioHintNotification,
			object: nil,
			queue: .main
		) { [weak self] notification in
			self?.handleSilenceHint(notification)
		}
		
		log("Started observing silence hint")
	}
	
	@objc public func stopObservingSilenceHint() {
		if let observer = silenceHintObserver {
			NotificationCenter.default.removeObserver(observer)
			silenceHintObserver = nil
		}
	}
	
	// MARK: - Private Helpers
	
	private func handleRouteChange(_ notification: Notification) {
		guard let userInfo = notification.userInfo,
			  let reasonValue = userInfo[AVAudioSessionRouteChangeReasonKey] as? UInt,
			  let reason = AVAudioSession.RouteChangeReason(rawValue: reasonValue) else {
			return
		}
		
		let reasonString = routeChangeReasonString(reason)
		let routeJSON = getCurrentRouteJSON()
		
		log("Route changed: \(reasonString)")
		
		reasonString.withCString { reasonPtr in
			routeJSON.withCString { jsonPtr in
				audioSessionRouteChanged(reasonPtr, jsonPtr)
			}
		}
	}
	
	private func handleInterruption(_ notification: Notification) {
		guard let userInfo = notification.userInfo,
			  let typeValue = userInfo[AVAudioSessionInterruptionTypeKey] as? UInt,
			  let type = AVAudioSession.InterruptionType(rawValue: typeValue) else {
			return
		}
		
		switch type {
		case .began:
			log("Interruption began")
			audioSessionInterruptionBegan()
			
		case .ended:
			let shouldResume = userInfo[AVAudioSessionInterruptionOptionKey] as? UInt == AVAudioSession.InterruptionOptions.shouldResume.rawValue
			log("Interruption ended, shouldResume: \(shouldResume)")
			audioSessionInterruptionEnded(shouldResume)
			
		@unknown default:
			break
		}
	}
	
	private func handleSilenceHint(_ notification: Notification) {
		guard let userInfo = notification.userInfo,
			  let typeValue = userInfo[AVAudioSessionSilenceSecondaryAudioHintTypeKey] as? UInt,
			  let hintType = AVAudioSession.SilenceSecondaryAudioHintType(rawValue: typeValue) else {
			return
		}
		
		let shouldSilence = hintType == .begin
		log("Silence hint: \(shouldSilence)")
		audioSessionSilenceHintChanged(shouldSilence)
	}
	
	private func mapCategory(_ category: String) -> AVAudioSession.Category {
		switch category.lowercased() {
		case "ambient": return .ambient
		case "soloambient": return .soloAmbient
		case "playback": return .playback
		case "record": return .record
		case "playandrecord": return .playAndRecord
		case "multiroute": return .multiRoute
		default: return .playback
		}
	}
	
	private func mapMode(_ mode: String) -> AVAudioSession.Mode {
		switch mode.lowercased() {
		case "default": return .default
		case "voicechat": return .voiceChat
		case "videochat": return .videoChat
		case "gamechat": return .gameChat
		case "videorecording": return .videoRecording
		case "measurement": return .measurement
		case "movieplayback": return .moviePlayback
		case "spokenaudio": return .spokenAudio
		default: return .default
		}
	}
	
	private func portTypeString(_ portType: AVAudioSession.Port) -> String {
		switch portType {
		case .builtInMic: return "BuiltInMic"
		case .builtInSpeaker: return "BuiltInSpeaker"
		case .builtInReceiver: return "BuiltInReceiver"
		case .headphones: return "Headphones"
		case .bluetoothA2DP: return "BluetoothA2DP"
		case .bluetoothLE: return "BluetoothLE"
		case .bluetoothHFP: return "BluetoothHFP"
		case .airPlay: return "AirPlay"
		case .lineIn: return "LineIn"
		case .lineOut: return "LineOut"
		case .usbAudio: return "USBAudio"
		case .carAudio: return "CarAudio"
		default: return "Unknown"
		}
	}
	
	private func routeChangeReasonString(_ reason: AVAudioSession.RouteChangeReason) -> String {
		switch reason {
		case .newDeviceAvailable: return "NewDeviceAvailable"
		case .oldDeviceUnavailable: return "OldDeviceUnavailable"
		case .categoryChange: return "CategoryChange"
		case .override: return "Override"
		case .wakeFromSleep: return "WakeFromSleep"
		case .noSuitableRouteForCategory: return "NoSuitableRoute"
		case .routeConfigurationChange: return "ConfigurationChange"
		default: return "Unknown"
		}
	}
}

// MARK: - Unity Callbacks (declared in bridge)

@_silgen_name("audioSessionRouteChanged")
func audioSessionRouteChanged(_ reason: UnsafePointer<CChar>, _ routeJSON: UnsafePointer<CChar>)

@_silgen_name("audioSessionInterruptionBegan")
func audioSessionInterruptionBegan()

@_silgen_name("audioSessionInterruptionEnded")
func audioSessionInterruptionEnded(_ shouldResume: Bool)

@_silgen_name("audioSessionSilenceHintChanged")
func audioSessionSilenceHintChanged(_ shouldSilence: Bool)


