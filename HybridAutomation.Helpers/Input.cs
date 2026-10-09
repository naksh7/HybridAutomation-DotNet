using System.Runtime.InteropServices;

namespace HybridAutomation.Helpers
{

    /// <summary>
    /// Provides comprehensive Windows input automation capabilities using Win32 API and SendInput for keyboard and mouse operations.
    /// </summary>
    public class Input
    {
        #region Machine Key Codes
        // --- Win32 API
        [DllImport("user32.dll")]
        private static extern short VkKeyScan(char ch);

        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int X, int Y);

        [DllImport("user32.dll")]
        private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        // SendInput structures
        [StructLayout(LayoutKind.Sequential)]
        struct INPUT
        {
            public uint Type;
            public INPUTUNION Data;
        }

        [StructLayout(LayoutKind.Explicit)]
        struct INPUTUNION
        {
            [FieldOffset(0)]
            public MOUSEINPUT Mouse;
            [FieldOffset(0)]
            public KEYBDINPUT Keyboard;
            [FieldOffset(0)]
            public HARDWAREINPUT Hardware;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct HARDWAREINPUT
        {
            public uint uMsg;
            public ushort wParamL;
            public ushort wParamH;
        }

        // Input types
        private const int INPUT_MOUSE = 0;
        private const int INPUT_KEYBOARD = 1;
        private const int INPUT_HARDWARE = 2;

        // Key event flags
        private const uint KEYEVENTF_EXTENDEDKEY = 0x0001;
        private const uint KEYEVENTF_KEYUP = 0x0002;
        private const uint KEYEVENTF_UNICODE = 0x0004;
        private const uint KEYEVENTF_SCANCODE = 0x0008;

        // Mouse event constants
        private const int MOUSEEVENTF_MOVE = 0x0001;
        private const int MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const int MOUSEEVENTF_LEFTUP = 0x0004;
        private const int MOUSEEVENTF_RIGHTDOWN = 0x0008;
        private const int MOUSEEVENTF_RIGHTUP = 0x0010;
        private const int MOUSEEVENTF_ABSOLUTE = 0x8000;
        private const int MOUSEEVENTF_MIDDLEDOWN = 0x0020;
        private const int MOUSEEVENTF_MIDDLEUP = 0x0040;
        private const int MOUSEEVENTF_WHEEL = 0x0800;
        private const int WHEEL_DELTA = 120;

        // Key event constants
        private const int KEYEVENTF_KEYDOWN = 0x0000;
        #endregion

        /// <summary>
        /// Moves the mouse cursor to the specified screen coordinates.
        /// </summary>
        /// <param name="x">Horizontal screen coordinate</param>
        /// <param name="y">Vertical screen coordinate</param>
        public void InputMoveMouseTo(int x, int y)
        {
            SetCursorPos(x, y);
        }

        /// <summary>
        /// Performs a single left mouse click at the current cursor position.
        /// </summary>
        public void InputLeftClick()
        {
            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
        }

        /// <summary>
        /// Performs a single right mouse click at the current cursor position.
        /// </summary>
        public void InputRightClick()
        {
            mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, UIntPtr.Zero);
            mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, UIntPtr.Zero);
        }

        /// <summary>
        /// Performs a double left mouse click with a brief delay between clicks.
        /// </summary>
        public void InputDoubleClick()
        {
            InputLeftClick();
            Thread.Sleep(10); // Small delay between clicks
            InputLeftClick();
        }

        /// <summary>
        /// Sends a virtual key code using the modern SendInput API for reliable special key input.
        /// </summary>
        /// <param name="keyCode">Virtual key code to send</param>
        private void SendSpecialKey(byte keyCode)
        {
            INPUT[] inputs = new INPUT[2];

            // Key down
            inputs[0] = new INPUT
            {
                Type = INPUT_KEYBOARD,
                Data = new INPUTUNION
                {
                    Keyboard = new KEYBDINPUT
                    {
                        wVk = keyCode,
                        wScan = 0,
                        dwFlags = 0,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };

            // Key up
            inputs[1] = new INPUT
            {
                Type = INPUT_KEYBOARD,
                Data = new INPUTUNION
                {
                    Keyboard = new KEYBDINPUT
                    {
                        wVk = keyCode,
                        wScan = 0,
                        dwFlags = KEYEVENTF_KEYUP,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };

            SendInput(2, inputs, Marshal.SizeOf(typeof(INPUT)));
        }

        /// <summary>
        /// Determines if a character is a special character that should use VkKeyScan.
        /// </summary>
        /// <param name="c">Character to check</param>
        /// <returns>True if the character is a special character</returns>
        private bool IsSpecialCharacter(char c)
        {
            // Explicitly exclude space character - it should be handled separately
            if (c == ' ') return false;

            // Common special characters that need VkKeyScan handling
            return c switch
            {
                '-' or '+' or '=' or '[' or ']' or ';' or '\'' or ',' or '.' or '/' or
                '\\' or '`' or '~' or '!' or '@' or '#' or '$' or '%' or '^' or '&' or
                '*' or '(' or ')' or '_' or '{' or '}' or ':' or '"' or '<' or '>' or
                '?' or '|' => true,
                _ => char.IsPunctuation(c) || char.IsSymbol(c)
            };
        }

        /// <summary>
        /// Sends a key with the Shift modifier pressed.
        /// </summary>
        /// <param name="keyCode">Virtual key code to send with Shift</param>
        private void SendKeyWithShift(byte keyCode)
        {
            INPUT[] inputs = new INPUT[4];

            // Shift down
            inputs[0] = new INPUT
            {
                Type = INPUT_KEYBOARD,
                Data = new INPUTUNION
                {
                    Keyboard = new KEYBDINPUT
                    {
                        wVk = 0x10, // VK_SHIFT
                        wScan = 0,
                        dwFlags = 0,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };

            // Key down
            inputs[1] = new INPUT
            {
                Type = INPUT_KEYBOARD,
                Data = new INPUTUNION
                {
                    Keyboard = new KEYBDINPUT
                    {
                        wVk = keyCode,
                        wScan = 0,
                        dwFlags = 0,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };

            // Key up
            inputs[2] = new INPUT
            {
                Type = INPUT_KEYBOARD,
                Data = new INPUTUNION
                {
                    Keyboard = new KEYBDINPUT
                    {
                        wVk = keyCode,
                        wScan = 0,
                        dwFlags = KEYEVENTF_KEYUP,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };

            // Shift up
            inputs[3] = new INPUT
            {
                Type = INPUT_KEYBOARD,
                Data = new INPUTUNION
                {
                    Keyboard = new KEYBDINPUT
                    {
                        wVk = 0x10, // VK_SHIFT
                        wScan = 0,
                        dwFlags = KEYEVENTF_KEYUP,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };

            SendInput(4, inputs, Marshal.SizeOf(typeof(INPUT)));
        }

        /// <summary>
        /// Sends a Unicode character using the SendInput API for proper handling of international characters and symbols.
        /// </summary>
        /// <param name="character">Unicode character to send</param>
        private void SendUnicodeChar(char character)
        {
            INPUT[] inputs = new INPUT[2];

            // Key down
            inputs[0] = new INPUT
            {
                Type = INPUT_KEYBOARD,
                Data = new INPUTUNION
                {
                    Keyboard = new KEYBDINPUT
                    {
                        wVk = 0,
                        wScan = character,
                        dwFlags = KEYEVENTF_UNICODE,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };

            // Key up
            inputs[1] = new INPUT
            {
                Type = INPUT_KEYBOARD,
                Data = new INPUTUNION
                {
                    Keyboard = new KEYBDINPUT
                    {
                        wVk = 0,
                        wScan = character,
                        dwFlags = KEYEVENTF_UNICODE | KEYEVENTF_KEYUP,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };
            SendInput(2, inputs, Marshal.SizeOf(typeof(INPUT)));
        }

        /// <summary>
        /// Inputs text using Unicode characters for letters/symbols while maintaining compatibility across applications.
        /// Inputs text using the most appropriate method for each character type:
        /// - Digits: Virtual key codes for maximum compatibility
        /// - Space: Virtual key code (VK_SPACE) for reliable space input
        /// - Special characters: VkKeyScan for proper keyboard layout handling
        /// - Other characters: Unicode for international character support
        /// </summary>
        /// <param name="text">Text string to input</param>
        /// <param name="timeOut">wait for the time provided</param>

        public void InputText(string text, int timeOut = 0)
        {
            foreach (char c in text)
            {
                if (char.IsDigit(c))
                {
                    // Use virtual key codes for digits (VK_0 to VK_9)
                    byte digitKey = (byte)(0x30 + (c - '0')); // 0x30 is VK_0, 0x31 is VK_1, etc.
                    SendSpecialKey(digitKey);
                    Utilities.WinApp.WaitForAppIdle(timeOut);
                }
                else if (c == ' ')
                {
                    // Handle space character explicitly with VK_SPACE for maximum compatibility
                    SendSpecialKey(0x20); // VK_SPACE
                    Utilities.WinApp.WaitForAppIdle(timeOut);
                }
                else if (IsSpecialCharacter(c))
                {
                    // Use VkKeyScan for special characters to respect keyboard layout
                    short scanResult = VkKeyScan(c);
                    if (scanResult != -1)
                    {
                        byte vkCode = (byte)(scanResult & 0xFF);
                        byte modifiers = (byte)((scanResult >> 8) & 0xFF);
                        // Handle shift key if needed
                        if ((modifiers & 1) != 0) // Shift key required
                        {
                            SendKeyWithShift(vkCode);
                            Utilities.WinApp.WaitForAppIdle(timeOut);
                        }
                        else
                        {
                            SendSpecialKey(vkCode);
                            Utilities.WinApp.WaitForAppIdle(timeOut);
                        }
                    }
                    else
                    {
                        // Fallback to Unicode if VkKeyScan fails
                        SendUnicodeChar(c);
                        Utilities.WinApp.WaitForAppIdle(timeOut);
                    }
                }
                else
                {
                    // Use Unicode for letters and other characters
                    SendUnicodeChar(c);
                    Utilities.WinApp.WaitForAppIdle(timeOut);
                }
            }
        }

        /// <summary>
        /// Sends the Tab key (0x09) for navigating between form elements.
        /// </summary>
        public void InputTab()
        {
            SendSpecialKey(0x09);
        }

        /// <summary>
        /// Sends the Enter key (0x0D) for confirming input or submitting forms.
        /// </summary>
        public void InputEnter()
        {
            SendSpecialKey(0x0D);
        }

        /// <summary>
        /// Sends the Up Arrow key (0x26) for upward navigation in lists or menus.
        /// </summary>
        public void InputArrowUp()
        {
            SendSpecialKey(0x26);
        }

        /// <summary>
        /// Sends the Down Arrow key (0x28) for downward navigation in lists or menus.
        /// </summary>
        public void InputArrowDown()
        {
            SendSpecialKey(0x28);
        }

        /// <summary>
        /// Sends the Left Arrow key (0x25) for leftward navigation or cursor movement.
        /// </summary>
        public void InputArrowLeft()
        {
            SendSpecialKey(0x25);
        }

        /// <summary>
        /// Sends the Right Arrow key (0x27) for rightward navigation or cursor movement.
        /// </summary>
        public void InputArrowRight()
        {
            SendSpecialKey(0x27);
        }

        /// <summary>
        /// Sends the Space key (0x20) for adding spaces in text input.
        /// </summary>
        public void InputSpace()
        {
            SendSpecialKey(0x20);
        }

        /// <summary>
        /// Sends the PageDown key (0x22) for page down.
        /// </summary>
        public void InputPageDown()
        {
            SendSpecialKey(0x22);
        }

        /// <summary>
        /// Sends the PageUp key (0x21) for page up.
        /// </summary>
        public void InputPageUp()
        {
            SendSpecialKey(0x21);
        }

        /// <summary>
        /// Sends the Delete key (0x2E) for deleting characters or selected content.
        /// </summary>
        public void InputDelete()
        {
            SendSpecialKey(0x2E);
        }

        /// <summary>
        /// Clears the current field by selecting all (Ctrl+A) and deleting (Backspace).
        /// This sends: Ctrl down, 'A' down/up, Backspace down/up, Ctrl up using SendInput.
        /// </summary>
        public void ClearField()
        {
            INPUT[] inputs = new INPUT[6];

            // Ctrl down
            inputs[0] = new INPUT
            {
                Type = INPUT_KEYBOARD,
                Data = new INPUTUNION
                {
                    Keyboard = new KEYBDINPUT
                    {
                        wVk = 0x11, // VK_CONTROL
                        wScan = 0,
                        dwFlags = 0,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };

            // 'A' down
            inputs[1] = new INPUT
            {
                Type = INPUT_KEYBOARD,
                Data = new INPUTUNION
                {
                    Keyboard = new KEYBDINPUT
                    {
                        wVk = 0x41, // 'A'
                        wScan = 0,
                        dwFlags = 0,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };

            // 'A' up
            inputs[2] = new INPUT
            {
                Type = INPUT_KEYBOARD,
                Data = new INPUTUNION
                {
                    Keyboard = new KEYBDINPUT
                    {
                        wVk = 0x41, // 'A'
                        wScan = 0,
                        dwFlags = KEYEVENTF_KEYUP,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };

            // Backspace down
            inputs[3] = new INPUT
            {
                Type = INPUT_KEYBOARD,
                Data = new INPUTUNION
                {
                    Keyboard = new KEYBDINPUT
                    {
                        wVk = 0x08, // VK_BACK
                        wScan = 0,
                        dwFlags = 0,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };

            // Backspace up
            inputs[4] = new INPUT
            {
                Type = INPUT_KEYBOARD,
                Data = new INPUTUNION
                {
                    Keyboard = new KEYBDINPUT
                    {
                        wVk = 0x08, // VK_BACK
                        wScan = 0,
                        dwFlags = KEYEVENTF_KEYUP,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };

            // Ctrl up
            inputs[5] = new INPUT
            {
                Type = INPUT_KEYBOARD,
                Data = new INPUTUNION
                {
                    Keyboard = new KEYBDINPUT
                    {
                        wVk = 0x11, // VK_CONTROL
                        wScan = 0,
                        dwFlags = KEYEVENTF_KEYUP,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };

            SendInput(6, inputs, Marshal.SizeOf(typeof(INPUT)));
        }

        /// <summary>
        /// Scrolls down using the mouse wheel at the current cursor position.
        /// </summary>        
        public void ScrollDown()
        {
            INPUT[] inputs = new INPUT[1];

            inputs[0] = new INPUT
            {
                Type = INPUT_MOUSE,
                Data = new INPUTUNION
                {
                    Mouse = new MOUSEINPUT
                    {
                        dx = 0,
                        dy = 0,
                        mouseData = unchecked((uint)(-WHEEL_DELTA)), // Negative for scroll down
                        dwFlags = MOUSEEVENTF_WHEEL,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };
            SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT)));
        }

        /// <summary>
        /// Scrolls up using the mouse wheel at the current cursor position.
        /// </summary>        
        public void ScrollUp()
        {
            INPUT[] inputs = new INPUT[1];

            inputs[0] = new INPUT
            {
                Type = INPUT_MOUSE,
                Data = new INPUTUNION
                {
                    Mouse = new MOUSEINPUT
                    {
                        dx = 0,
                        dy = 0,
                        mouseData = WHEEL_DELTA, // Positive for scroll up
                        dwFlags = MOUSEEVENTF_WHEEL,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };
            SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT)));
        }
    }
}
