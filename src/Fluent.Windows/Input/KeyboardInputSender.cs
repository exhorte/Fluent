using System.Runtime.InteropServices;

namespace Fluent.Windows.Input;

public sealed class KeyboardInputSender : IKeyboardInputSender
{
    private const ushort VirtualKeyControl = 0x11;
    private const ushort VirtualKeyV = 0x56;
    private const uint InputKeyboard = 1;
    private const uint KeyEventKeyUp = 0x0002;

    public KeyboardInputSendResult SendCtrlV()
    {
        Input[] inputs =
        [
            Input.KeyDown(VirtualKeyControl),
            Input.KeyDown(VirtualKeyV),
            Input.KeyUp(VirtualKeyV),
            Input.KeyUp(VirtualKeyControl)
        ];

        uint sent = NativeMethods.SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<Input>());
        if (sent == inputs.Length)
        {
            return new KeyboardInputSendResult(
                Succeeded: true,
                SentInputCount: sent,
                RequestedInputCount: (uint)inputs.Length,
                ErrorCode: 0);
        }

        return new KeyboardInputSendResult(
            Succeeded: false,
            SentInputCount: sent,
            RequestedInputCount: (uint)inputs.Length,
            ErrorCode: Marshal.GetLastPInvokeError());
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Input
    {
        public uint Type;
        public InputUnion Data;

        public static Input KeyDown(ushort virtualKey) => new()
        {
            Type = InputKeyboard,
            Data = new InputUnion
            {
                Keyboard = new KeyboardInput { VirtualKey = virtualKey }
            }
        };

        public static Input KeyUp(ushort virtualKey) => new()
        {
            Type = InputKeyboard,
            Data = new InputUnion
            {
                Keyboard = new KeyboardInput { VirtualKey = virtualKey, Flags = KeyEventKeyUp }
            }
        };
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)]
        public MouseInput Mouse;

        [FieldOffset(0)]
        public KeyboardInput Keyboard;

        [FieldOffset(0)]
        public HardwareInput Hardware;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MouseInput
    {
        public int X;
        public int Y;
        public uint MouseData;
        public uint Flags;
        public uint Time;
        public nint ExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KeyboardInput
    {
        public ushort VirtualKey;
        public ushort Scan;
        public uint Flags;
        public uint Time;
        public nint ExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct HardwareInput
    {
        public uint Message;
        public ushort ParameterLow;
        public ushort ParameterHigh;
    }

    private static class NativeMethods
    {
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern uint SendInput(uint inputCount, Input[] inputs, int inputSize);
    }
}
