using System.Runtime.InteropServices;
using SlimPenHotkeys.Interop;
using static SlimPenHotkeys.Interop.NativeMethods;

namespace SlimPenHotkeys.Core;

/// <summary>Thin wrapper over SendInput for synthesizing key up/down events.</summary>
internal static class InputSender
{
    public static void SendKeys(IReadOnlyList<(int vk, bool isDown)> keys)
    {
        if (keys.Count == 0) return;

        var inputs = new INPUT[keys.Count];
        for (int i = 0; i < keys.Count; i++)
        {
            var (vk, isDown) = keys[i];
            uint flags = 0;
            if (!isDown) flags |= KEYEVENTF_KEYUP;
            if (KeyMap.IsExtended(vk)) flags |= KEYEVENTF_EXTENDEDKEY;

            inputs[i] = new INPUT
            {
                type = INPUT_KEYBOARD,
                U = new InputUnion
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = (ushort)vk,
                        wScan = 0,
                        dwFlags = flags,
                        time = 0,
                        dwExtraInfo = InjectSignature,
                    },
                },
            };
        }

        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
    }

    public static void SendKey(int vk, bool isDown)
        => SendKeys(new[] { (vk, isDown) });
}
