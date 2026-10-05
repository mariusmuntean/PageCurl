import CoreGraphics
import Foundation

// Usage: clickevent <command> [args...]
//   click X Y            - left click at point
//   drag X1 Y1 X2 Y2 MS  - left drag from (X1,Y1) to (X2,Y2) over MS milliseconds
//   move X Y             - move mouse

func post(_ event: CGEvent, at point: CGPoint? = nil) {
    if let point {
        event.location = point
    }
    event.post(tap: .cghidEventTap)
}

func makeClick(type: CGEventType, at p: CGPoint) -> CGEvent? {
    CGEvent(mouseEventSource: nil, mouseType: type, mouseCursorPosition: p, mouseButton: .left)
}

let args = CommandLine.arguments
guard args.count >= 3 else {
    FileHandle.standardError.write("usage: clickevent click|move X Y | drag X1 Y1 X2 Y2 MS\n".data(using: .utf8)!)
    exit(2)
}

switch args[1] {
case "click":
    let x = Double(args[2])!, y = Double(args[3])!
    let p = CGPoint(x: x, y: y)
    post(CGEvent(mouseEventSource: nil, mouseType: .leftMouseDown, mouseCursorPosition: p, mouseButton: .left)!, at: p)
    post(CGEvent(mouseEventSource: nil, mouseType: .leftMouseUp, mouseCursorPosition: p, mouseButton: .left)!, at: p)
case "down":
    let x = Double(args[2])!, y = Double(args[3])!
    let p = CGPoint(x: x, y: y)
    post(CGEvent(mouseEventSource: nil, mouseType: .leftMouseDown, mouseCursorPosition: p, mouseButton: .left)!, at: p)
case "up":
    let x = Double(args[2])!, y = Double(args[3])!
    let p = CGPoint(x: x, y: y)
    post(CGEvent(mouseEventSource: nil, mouseType: .leftMouseUp, mouseCursorPosition: p, mouseButton: .left)!, at: p)
case "move":
    let x = Double(args[2])!, y = Double(args[3])!
    let p = CGPoint(x: x, y: y)
    post(CGEvent(mouseEventSource: nil, mouseType: .mouseMoved, mouseCursorPosition: p, mouseButton: .left)!, at: p)
case "drag":
    guard args.count >= 7 else {
        FileHandle.standardError.write("drag needs X1 Y1 X2 Y2 MS\n".data(using: .utf8)!)
        exit(2)
    }
    let x1 = Double(args[2])!, y1 = Double(args[3])!, x2 = Double(args[4])!, y2 = Double(args[5])!
    let totalMs = Double(args[6])!
    let steps = max(Int(totalMs / 16), 5)
    let start = CGPoint(x: x1, y: y1)
    // Move to the start first so hover/hit-testing sees the pointer there.
    for i in 0...10 {
        let t = Double(i) / 10
        let p = CGPoint(x: x1 + (x2 - x1) * t, y: y1 + (y2 - y1) * t)
        post(CGEvent(mouseEventSource: nil, mouseType: .mouseMoved, mouseCursorPosition: p, mouseButton: .left)!, at: p)
        usleep(20_000)
    }
    post(CGEvent(mouseEventSource: nil, mouseType: .leftMouseDown, mouseCursorPosition: start, mouseButton: .left)!, at: start)
    usleep(250_000)
    for i in 1...steps {
        let t = Double(i) / Double(steps)
        let p = CGPoint(x: x1 + (x2 - x1) * t, y: y1 + (y2 - y1) * t)
        post(CGEvent(mouseEventSource: nil, mouseType: .leftMouseDragged, mouseCursorPosition: p, mouseButton: .left)!, at: p)
        usleep(useconds_t(totalMs * 1000 / Double(steps)))
    }
    usleep(250_000)
    let end = CGPoint(x: x2, y: y2)
    post(CGEvent(mouseEventSource: nil, mouseType: .leftMouseUp, mouseCursorPosition: end, mouseButton: .left)!, at: end)
default:
    FileHandle.standardError.write("unknown command \(args[1])\n".data(using: .utf8)!)
    exit(2)
}
