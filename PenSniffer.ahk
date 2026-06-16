#Requires AutoHotkey v2.0
#SingleInstance Force
InstallKeybdHook()
InstallMouseHook()
LogFile := A_ScriptDir "\penlog.txt"
try FileDelete(LogFile)
FileAppend("Listening. Press your Surface Pen buttons now…`n", LogFile)

; Hook every standard + media + F-key + mouse key
keys := [
  "Volume_Up","Volume_Down","Volume_Mute",
  "Media_Play_Pause","Media_Next","Media_Prev","Media_Stop",
  "Browser_Back","Browser_Forward","Browser_Refresh","Browser_Home",
  "Browser_Search","Browser_Favorites",
  "Launch_Mail","Launch_Media","Launch_App1","Launch_App2",
  "Enter","Space","Escape","Tab","Backspace","Delete","Insert",
  "Up","Down","Left","Right","PgUp","PgDn","Home","End",
  "LButton","RButton","MButton","XButton1","XButton2",
  "AppsKey","PrintScreen","Pause","ScrollLock","NumLock","CapsLock",
  "LWin","RWin","LControl","RControl","LAlt","RAlt","LShift","RShift",
  "F1","F2","F3","F4","F5","F6","F7","F8","F9","F10","F11","F12",
  "F13","F14","F15","F16","F17","F18","F19","F20",
  "F21","F22","F23","F24"
]
for k in keys {
    try Hotkey("*" k, LogKey.Bind(k))
}
chars := "abcdefghijklmnopqrstuvwxyz0123456789"
Loop Parse, chars {
    ch := A_LoopField
    try Hotkey("*" ch, LogKey.Bind(ch))
}

SetTimer(WatchUnknown, 50)
TrayTip("Pen sniffer running", "Press the Surface Pen button.`nLog: " LogFile, 1)
return

LogKey(name, *) {
    global LogFile
    ts := FormatTime(, "HH:mm:ss.")
    FileAppend(ts " " name " (sc=" Format("0x{:03X}", GetKeySC(name)) " vk=" Format("0x{:02X}", GetKeyVK(name)) ")`n", LogFile)
}

WatchUnknown() {
    static last := ""
    cur := A_PriorKey
    if (cur != "" && cur != last) {
        last := cur
        global LogFile
        ts := FormatTime(, "HH:mm:ss.")
        FileAppend(ts " A_PriorKey=" cur "`n", LogFile)
    }
}
