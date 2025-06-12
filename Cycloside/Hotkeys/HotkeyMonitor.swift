import Cocoa

private var monitor: Any?
private var callback: ((UInt16, UInt64) -> Void)?

@_cdecl("RegisterGlobalHotkeyMonitor")
public func RegisterGlobalHotkeyMonitor(_ cb: @escaping (UInt16, UInt64) -> Void) -> UnsafeMutableRawPointer? {
    callback = cb
    monitor = NSEvent.addGlobalMonitorForEvents(matching: .keyDown) { event in
        callback?(event.keyCode, event.modifierFlags.rawValue)
    }
    if let m = monitor {
        return Unmanaged.passRetained(m as AnyObject).toOpaque()
    }
    return nil
}

@_cdecl("UnregisterGlobalHotkeyMonitor")
public func UnregisterGlobalHotkeyMonitor(_ token: UnsafeMutableRawPointer?) {
    if let token = token {
        let obj = Unmanaged<AnyObject>.fromOpaque(token).takeRetainedValue()
        NSEvent.removeMonitor(obj)
    }
    monitor = nil
    callback = nil
}
