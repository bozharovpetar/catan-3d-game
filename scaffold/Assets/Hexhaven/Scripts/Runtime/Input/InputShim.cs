// SCAFFOLD — complete. The ONLY file allowed to touch input APIs (docs/02-ARCHITECTURE.md §3).
using UnityEngine;
#if HEXHAVEN_INPUTSYSTEM
using UnityEngine.InputSystem;
#endif

namespace Hexhaven
{
    /// <summary>Logical keys used by the game (mapped to physical keys here only).</summary>
    public enum GameKey
    {
        Space, Enter, Escape, Tab, Path1, Outpost2, Keep3, Charter4, Charters, Trade, Costs, Log, Home,
        Up, Down, Left, Right, RotateLeft, RotateRight, Help, Debug, Screenshot
    }

    /// <summary>Unified input over the Input System package (preferred) or the legacy Input Manager.</summary>
    public static class InputShim
    {
        public static Vector2 MousePosition
        {
            get
            {
#if HEXHAVEN_INPUTSYSTEM
                var m = Mouse.current; return m != null ? m.position.ReadValue() : Vector2.zero;
#else
                return Input.mousePosition;
#endif
            }
        }

        public static bool LeftDown { get { return Button(0, true); } }
        public static bool LeftHeld { get { return Button(0, false); } }
        public static bool RightDown { get { return Button(1, true); } }
        public static bool RightHeld { get { return Button(1, false); } }
        public static bool MiddleHeld { get { return Button(2, false); } }
        public static bool RightUp
        {
            get
            {
#if HEXHAVEN_INPUTSYSTEM
                var m = Mouse.current; return m != null && m.rightButton.wasReleasedThisFrame;
#else
                return Input.GetMouseButtonUp(1);
#endif
            }
        }

        /// <summary>Scroll in "notches" (+ = away from user / zoom in).</summary>
        public static float Scroll
        {
            get
            {
#if HEXHAVEN_INPUTSYSTEM
                var m = Mouse.current; return m != null ? Mathf.Clamp(m.scroll.ReadValue().y / 120f, -3f, 3f) : 0f;
#else
                return Input.mouseScrollDelta.y;
#endif
            }
        }

        static bool Button(int index, bool down)
        {
#if HEXHAVEN_INPUTSYSTEM
            var m = Mouse.current;
            if (m == null) return false;
            var b = index == 0 ? m.leftButton : index == 1 ? m.rightButton : m.middleButton;
            return down ? b.wasPressedThisFrame : b.isPressed;
#else
            return down ? Input.GetMouseButtonDown(index) : Input.GetMouseButton(index);
#endif
        }

        public static bool KeyDown(GameKey k) { return Key(k, true); }
        public static bool KeyHeld(GameKey k) { return Key(k, false); }

        static bool Key(GameKey k, bool down)
        {
#if HEXHAVEN_INPUTSYSTEM
            var kb = Keyboard.current;
            if (kb == null) return false;
            foreach (var key in Map(k))
            {
                var c = kb[key];
                if (down ? c.wasPressedThisFrame : c.isPressed) return true;
            }
            return false;
#else
            foreach (var key in MapLegacy(k)) if (down ? Input.GetKeyDown(key) : Input.GetKey(key)) return true;
            return false;
#endif
        }

#if HEXHAVEN_INPUTSYSTEM
        static Key[] Map(GameKey k)
        {
            switch (k)
            {
                case GameKey.Space: return new[] { UnityEngine.InputSystem.Key.Space };
                case GameKey.Enter: return new[] { UnityEngine.InputSystem.Key.Enter };
                case GameKey.Escape: return new[] { UnityEngine.InputSystem.Key.Escape };
                case GameKey.Tab: return new[] { UnityEngine.InputSystem.Key.Tab };
                case GameKey.Path1: return new[] { UnityEngine.InputSystem.Key.Digit1 };
                case GameKey.Outpost2: return new[] { UnityEngine.InputSystem.Key.Digit2 };
                case GameKey.Keep3: return new[] { UnityEngine.InputSystem.Key.Digit3 };
                case GameKey.Charter4: return new[] { UnityEngine.InputSystem.Key.Digit4 };
                case GameKey.Charters: return new[] { UnityEngine.InputSystem.Key.C };
                case GameKey.Trade: return new[] { UnityEngine.InputSystem.Key.T };
                case GameKey.Costs: return new[] { UnityEngine.InputSystem.Key.K };
                case GameKey.Log: return new[] { UnityEngine.InputSystem.Key.L };
                case GameKey.Home: return new[] { UnityEngine.InputSystem.Key.H };
                case GameKey.Up: return new[] { UnityEngine.InputSystem.Key.W, UnityEngine.InputSystem.Key.UpArrow };
                case GameKey.Down: return new[] { UnityEngine.InputSystem.Key.S, UnityEngine.InputSystem.Key.DownArrow };
                case GameKey.Left: return new[] { UnityEngine.InputSystem.Key.A, UnityEngine.InputSystem.Key.LeftArrow };
                case GameKey.Right: return new[] { UnityEngine.InputSystem.Key.D, UnityEngine.InputSystem.Key.RightArrow };
                case GameKey.RotateLeft: return new[] { UnityEngine.InputSystem.Key.Q };
                case GameKey.RotateRight: return new[] { UnityEngine.InputSystem.Key.E };
                case GameKey.Help: return new[] { UnityEngine.InputSystem.Key.F1, UnityEngine.InputSystem.Key.Slash };
                case GameKey.Debug: return new[] { UnityEngine.InputSystem.Key.F3 };
                case GameKey.Screenshot: return new[] { UnityEngine.InputSystem.Key.F12 };
                default: return new Key[0];
            }
        }
#else
        static KeyCode[] MapLegacy(GameKey k)
        {
            switch (k)
            {
                case GameKey.Space: return new[] { KeyCode.Space };
                case GameKey.Enter: return new[] { KeyCode.Return, KeyCode.KeypadEnter };
                case GameKey.Escape: return new[] { KeyCode.Escape };
                case GameKey.Tab: return new[] { KeyCode.Tab };
                case GameKey.Path1: return new[] { KeyCode.Alpha1 };
                case GameKey.Outpost2: return new[] { KeyCode.Alpha2 };
                case GameKey.Keep3: return new[] { KeyCode.Alpha3 };
                case GameKey.Charter4: return new[] { KeyCode.Alpha4 };
                case GameKey.Charters: return new[] { KeyCode.C };
                case GameKey.Trade: return new[] { KeyCode.T };
                case GameKey.Costs: return new[] { KeyCode.K };
                case GameKey.Log: return new[] { KeyCode.L };
                case GameKey.Home: return new[] { KeyCode.H };
                case GameKey.Up: return new[] { KeyCode.W, KeyCode.UpArrow };
                case GameKey.Down: return new[] { KeyCode.S, KeyCode.DownArrow };
                case GameKey.Left: return new[] { KeyCode.A, KeyCode.LeftArrow };
                case GameKey.Right: return new[] { KeyCode.D, KeyCode.RightArrow };
                case GameKey.RotateLeft: return new[] { KeyCode.Q };
                case GameKey.RotateRight: return new[] { KeyCode.E };
                case GameKey.Help: return new[] { KeyCode.F1, KeyCode.Slash };
                case GameKey.Debug: return new[] { KeyCode.F3 };
                case GameKey.Screenshot: return new[] { KeyCode.F12 };
                default: return new KeyCode[0];
            }
        }
#endif
    }
}
