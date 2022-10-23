using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static WindowsConsole;

namespace Internal
{
    internal static class NativeMethod
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern IntPtr GetStdHandle(int nStdHandle);
        internal enum StandardHandle : int
        {
            STD_INPUT_HANDLE = -10,
            STD_OUTPUT_HANDLE = -11,
            STD_ERROR_HANDLE = -12,
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);
        [Flags]
        internal enum ConsoleMode : uint
        {
            ENABLE_PROCESSED_INPUT = 0x0001,
            ENABLE_LINE_INPUT = 0x0002,
            ENABLE_ECHO_INPUT = 0x0004,
            ENABLE_WINDOW_INPUT = 0x0008,
            ENABLE_MOUSE_INPUT = 0x0010,
            ENABLE_INSERT_MODE = 0x0020,
            ENABLE_QUICK_EDIT_MODE = 0x0040,
            ENABLE_EXTENDED_FLAGS = 0x0080,
            ENABLE_AUTO_POSITION = 0x0100,

            ENABLE_PROCESSED_OUTPUT = 0x0001,
            ENABLE_WRAP_AT_EOL_OUTPUT = 0x0002,
            ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004,
            DISABLE_NEWLINE_AUTO_RETURN = 0x0008,
            ENABLE_LVB_GRID_WORLDWIDE = 0x0010
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool PeekConsoleInput(
            IntPtr hConsoleInput,
            [Out] INPUT_RECORD[] lpBuffer,
            uint nLength,
            out uint lpNumberOfEventsRead);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "ReadConsoleInputW")]
        internal static extern bool ReadConsoleInput(
            IntPtr hConsoleInput,
            [Out] INPUT_RECORD[] lpBuffer,
            uint nLength,
            out uint lpNumberOfEventsRead);

        [StructLayout(LayoutKind.Explicit)]
        internal struct INPUT_RECORD
        {
            [FieldOffset(0)]
            internal ushort EventType;
            [FieldOffset(4)]
            internal KEY_EVENT_RECORD KeyEvent;
            [FieldOffset(4)]
            internal MOUSE_EVENT_RECORD MouseEvent;
            [FieldOffset(4)]
            internal WINDOW_BUFFER_SIZE_RECORD WindowBufferSizeEvent;
            [FieldOffset(4)]
            internal MENU_EVENT_RECORD MenuEvent;
            [FieldOffset(4)]
            internal FOCUS_EVENT_RECORD FocusEvent;
        };

        [StructLayout(LayoutKind.Explicit, CharSet = CharSet.Unicode)]
        internal struct KEY_EVENT_RECORD
        {
            [FieldOffset(0), MarshalAs(UnmanagedType.Bool)]
            internal bool bKeyDown;
            [FieldOffset(4), MarshalAs(UnmanagedType.U2)]
            internal ushort wRepeatCount;
            [FieldOffset(6), MarshalAs(UnmanagedType.U2)]
            internal WindowsConsole.VirutalKeyType wVirtualKeyCode;
            [FieldOffset(8), MarshalAs(UnmanagedType.U2)]
            internal ushort wVirtualScanCode;
            [FieldOffset(10)]
            internal char UnicodeChar;
            [FieldOffset(12), MarshalAs(UnmanagedType.U4)]
            internal ControlKeyState dwControlKeyState;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct MOUSE_EVENT_RECORD
        {
            internal COORD dwMousePosition;
            internal MouseButtonState dwButtonState;
            internal ControlKeyState dwControlKeyState;
            internal MouseEventFlags dwEventFlags;
        }

        [Flags]
        internal enum MouseButtonState
        {
            FROM_LEFT_1ST_BUTTON_PRESSED = 0x1,
            RIGHTMOST_BUTTON_PRESSED = 0x2,
            FROM_LEFT_2ND_BUTTON_PRESSED = 0x4,
            FROM_LEFT_3RD_BUTTON_PRESSED = 0x8,
            FROM_LEFT_4TH_BUTTON_PRESSED = 0x10
        }

        [Flags]
        internal enum MouseEventFlags
        {
            MOUSE_MOVED = 0x1,
            DOUBLE_CLICK = 0x2,
            MOUSE_WHEELED = 0x4,
            MOUSE_HWHEELED = 0x8
        }

        internal struct WINDOW_BUFFER_SIZE_RECORD
        {
            internal COORD dwSize;

            internal WINDOW_BUFFER_SIZE_RECORD(short x, short y)
            {
                this.dwSize = new COORD(x, y);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct COORD
        {
            internal short X;
            internal short Y;

            internal COORD(short X, short Y)
            {
                this.X = X;
                this.Y = Y;
            }
        };

        [StructLayout(LayoutKind.Sequential)]
        internal struct MENU_EVENT_RECORD
        {
            internal uint dwCommandId;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct FOCUS_EVENT_RECORD
        {
            internal uint bSetFocus;
        }
    }
}
