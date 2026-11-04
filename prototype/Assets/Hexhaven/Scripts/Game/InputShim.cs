using UnityEngine;
#if !ENABLE_LEGACY_INPUT_MANAGER && ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Hexhaven
{
    /// <summary>
    /// Works with either input backend: the legacy Input Manager (Built-in/“Both” projects)
    /// or the Input System package (Unity 6 default for new projects).
    /// </summary>
    public static class InputShim
    {
#if ENABLE_LEGACY_INPUT_MANAGER
        public static Vector2 MousePosition { get { return Input.mousePosition; } }
        public static bool LeftClick { get { return Input.GetMouseButtonDown(0); } }
        public static bool OrbitHeld { get { return Input.GetMouseButton(1) || Input.GetMouseButton(2); } }
        public static float Scroll { get { return Input.mouseScrollDelta.y; } }
        public static bool Cancel { get { return Input.GetKeyDown(KeyCode.Escape); } }
        public static bool RollKey { get { return Input.GetKeyDown(KeyCode.Space); } }
        public static Vector2 PanAxis
        {
            get
            {
                float x = (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1 : 0) - (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? 1 : 0);
                float y = (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) ? 1 : 0) - (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) ? 1 : 0);
                return new Vector2(x, y);
            }
        }
        public static float RotateAxis { get { return (Input.GetKey(KeyCode.E) ? 1 : 0) - (Input.GetKey(KeyCode.Q) ? 1 : 0); } }
#elif ENABLE_INPUT_SYSTEM
        static Mouse M { get { return Mouse.current; } }
        static Keyboard K { get { return Keyboard.current; } }
        public static Vector2 MousePosition { get { return M != null ? M.position.ReadValue() : Vector2.zero; } }
        public static bool LeftClick { get { return M != null && M.leftButton.wasPressedThisFrame; } }
        public static bool OrbitHeld { get { return M != null && (M.rightButton.isPressed || M.middleButton.isPressed); } }
        public static float Scroll { get { return M != null ? Mathf.Clamp(M.scroll.ReadValue().y / 120f, -3f, 3f) : 0f; } }
        public static bool Cancel { get { return K != null && K.escapeKey.wasPressedThisFrame; } }
        public static bool RollKey { get { return K != null && K.spaceKey.wasPressedThisFrame; } }
        public static Vector2 PanAxis
        {
            get
            {
                if (K == null) return Vector2.zero;
                float x = (K.dKey.isPressed || K.rightArrowKey.isPressed ? 1 : 0) - (K.aKey.isPressed || K.leftArrowKey.isPressed ? 1 : 0);
                float y = (K.wKey.isPressed || K.upArrowKey.isPressed ? 1 : 0) - (K.sKey.isPressed || K.downArrowKey.isPressed ? 1 : 0);
                return new Vector2(x, y);
            }
        }
        public static float RotateAxis { get { return K == null ? 0 : (K.eKey.isPressed ? 1 : 0) - (K.qKey.isPressed ? 1 : 0); } }
#else
        public static Vector2 MousePosition { get { return Vector2.zero; } }
        public static bool LeftClick { get { return false; } }
        public static bool OrbitHeld { get { return false; } }
        public static float Scroll { get { return 0; } }
        public static bool Cancel { get { return false; } }
        public static bool RollKey { get { return false; } }
        public static Vector2 PanAxis { get { return Vector2.zero; } }
        public static float RotateAxis { get { return 0; } }
#endif
    }
}
