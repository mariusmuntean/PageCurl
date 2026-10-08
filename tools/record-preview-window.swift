// Record only the supplied process's window, without cursor or audio.
import Foundation
import AppKit
import ScreenCaptureKit
import CoreMedia

final class RecordingSignals: NSObject, SCRecordingOutputDelegate {
    let folder: URL
    init(_ folder: URL) { self.folder = folder }
    func recordingOutputDidStartRecording(_ recordingOutput: SCRecordingOutput) {
        write("start", "start")
    }
    func recordingOutputDidFinishRecording(_ recordingOutput: SCRecordingOutput) {
        write("recorded", "recorded")
    }
    func recordingOutput(_ recordingOutput: SCRecordingOutput, didFailWithError error: Error) {
        write("error", error.localizedDescription)
    }
    private func write(_ name: String, _ text: String) {
        do { try text.write(to: folder.appendingPathComponent(name), atomically: true, encoding: .utf8) }
        catch { fputs("Preview signal failed: \(error)\n", stderr); exit(1) }
    }
}

@main
struct PreviewRecorder {
    static func main() async {
        do { try await record() }
        catch { fputs("Preview recording failed: \(error)\n", stderr); exit(1) }
    }

    @MainActor private static func record() async throws {
        // A CLI needs a WindowServer connection before creating the filter.
        _ = NSApplication.shared
        guard CommandLine.arguments.count == 4, let pid = Int32(CommandLine.arguments[1]) else {
            throw NSError(domain: "Preview", code: 1, userInfo: [NSLocalizedDescriptionKey: "Expected process ID, signal directory and output file"])
        }
        let folder = URL(fileURLWithPath: CommandLine.arguments[2])
        let available = try await SCShareableContent.excludingDesktopWindows(true, onScreenWindowsOnly: true)
        guard let window = available.windows.filter({ $0.owningApplication?.processID == pid && $0.frame.width > 400 })
            .max(by: { $0.frame.width * $0.frame.height < $1.frame.width * $1.frame.height }) else {
            throw NSError(domain: "Preview", code: 2, userInfo: [NSLocalizedDescriptionKey: "The preview process has no visible window"])
        }
        let config = SCStreamConfiguration()
        config.width = Int(window.frame.width) * 2
        config.height = Int(window.frame.height) * 2
        config.minimumFrameInterval = CMTime(value: 1, timescale: 60)
        config.showsCursor = false
        config.capturesAudio = false
        let stream = SCStream(filter: SCContentFilter(desktopIndependentWindow: window), configuration: config, delegate: nil)
        let recordingConfig = SCRecordingOutputConfiguration()
        recordingConfig.outputURL = URL(fileURLWithPath: CommandLine.arguments[3])
        let signals = RecordingSignals(folder)
        let output = SCRecordingOutput(configuration: recordingConfig, delegate: signals)
        try stream.addRecordingOutput(output)
        try await stream.startCapture()
        try await waitFor("done", in: folder)
        try await stream.stopCapture()
        try await waitFor("recorded", in: folder)
        try "finish".write(to: folder.appendingPathComponent("finish"), atomically: true, encoding: .utf8)
    }

    private static func waitFor(_ name: String, in folder: URL) async throws {
        let deadline = Date().addingTimeInterval(45)
        while Date() < deadline {
            let error = folder.appendingPathComponent("error")
            if FileManager.default.fileExists(atPath: error.path) {
                throw NSError(domain: "Preview", code: 3, userInfo: [NSLocalizedDescriptionKey: try String(contentsOf: error, encoding: .utf8)])
            }
            if FileManager.default.fileExists(atPath: folder.appendingPathComponent(name).path) { return }
            try await Task.sleep(for: .milliseconds(50))
        }
        throw NSError(domain: "Preview", code: 4, userInfo: [NSLocalizedDescriptionKey: "Timed out waiting for \(name)"])
    }
}
