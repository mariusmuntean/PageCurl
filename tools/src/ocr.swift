import Vision
import AppKit

guard CommandLine.arguments.count >= 2 else {
    FileHandle.standardError.write("usage: ocr <image-path> [x y w h] (optional rect in image pixels)\n".data(using: .utf8)!)
    exit(2)
}

let path = CommandLine.arguments[1]
guard let image = NSImage(contentsOfFile: path),
      let tiff = image.tiffRepresentation,
      let cg = NSBitmapImageRep(data: tiff)?.cgImage else {
    FileHandle.standardError.write("cannot load image\n".data(using: .utf8)!)
    exit(1)
}

// Optional crop rect in image pixels
var target = cg
if CommandLine.arguments.count >= 6 {
    let x = Double(CommandLine.arguments[2])!, y = Double(CommandLine.arguments[3])!
    let w = Double(CommandLine.arguments[4])!, h = Double(CommandLine.arguments[5])!
    target = cg.cropping(to: CGRect(x: x, y: y, width: w, height: h)) ?? cg
}

let request = VNRecognizeTextRequest()
request.recognitionLevel = .accurate
request.usesLanguageCorrection = false

let handler = VNImageRequestHandler(cgImage: target, options: [:])
try handler.perform([request])

let results = (request.results ?? []).compactMap { $0.topCandidates(1).first?.string }
print(results.joined(separator: "\n"))
