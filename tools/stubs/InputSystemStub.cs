// Minimal Input System API surface for tools/compile-check.sh. NOT shipped in the game.
// Runtime code may only use the members declared here (InputShim is the only file that touches the Input System).
using UnityEngine;

namespace UnityEngine.InputSystem.Controls
{
    public class ButtonControl
    {
        public bool isPressed { get { return false; } }
        public bool wasPressedThisFrame { get { return false; } }
        public bool wasReleasedThisFrame { get { return false; } }
    }
    public class KeyControl : ButtonControl { }
    public class Vector2Control { public Vector2 ReadValue() { return Vector2.zero; } }
}

namespace UnityEngine.InputSystem
{
    using UnityEngine.InputSystem.Controls;

    public enum Key
    {
        None, Space, Enter, Tab, Escape, LeftShift, RightShift, LeftCtrl, RightCtrl,
        A, B, C, D, E, F, G, H, I, J, K, L, M, N, O, P, Q, R, S, T, U, V, W, X, Y, Z,
        Digit1, Digit2, Digit3, Digit4, Digit5, Digit6, Digit7, Digit8, Digit9, Digit0,
        LeftArrow, RightArrow, UpArrow, DownArrow, F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12, Slash
    }

    public class Mouse
    {
        public static Mouse current { get { return null; } }
        public Vector2Control position = new Vector2Control();
        public Vector2Control delta = new Vector2Control();
        public Vector2Control scroll = new Vector2Control();
        public ButtonControl leftButton = new ButtonControl(), rightButton = new ButtonControl(), middleButton = new ButtonControl();
    }

    public class Keyboard
    {
        public static Keyboard current { get { return null; } }
        public KeyControl this[Key key] { get { return new KeyControl(); } }
        public KeyControl anyKey { get { return new KeyControl(); } }
    }
}
