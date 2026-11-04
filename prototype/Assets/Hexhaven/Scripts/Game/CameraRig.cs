using UnityEngine;

namespace Hexhaven
{
    /// <summary>Orbit camera: right/middle-drag to orbit, scroll to zoom, WASD/arrows to pan, Q/E to rotate.</summary>
    public class CameraRig : MonoBehaviour
    {
        public Vector3 Target = Vector3.zero;
        public float Yaw = 0f, Pitch = 58f, Distance = 11.5f;
        public float MinDistance = 4f, MaxDistance = 22f, MinPitch = 20f, MaxPitch = 85f;
        public float PanLimit = 5f;

        float yaw, pitch, distance;
        Vector3 target;
        Vector2 lastMouse;
        bool dragging;

        /// <summary>Set by the HUD so dragging/scrolling over UI panels doesn't move the camera.</summary>
        public bool PointerOverUI;

        void Start()
        {
            yaw = Yaw; pitch = Pitch; distance = Distance; target = Target;
            Apply();
        }

        void LateUpdate()
        {
            Vector2 mouse = InputShim.MousePosition;
            if (InputShim.OrbitHeld)
            {
                if (dragging)
                {
                    Vector2 d = mouse - lastMouse;
                    Yaw += d.x * 0.25f;
                    Pitch = Mathf.Clamp(Pitch - d.y * 0.2f, MinPitch, MaxPitch);
                }
                dragging = true;
            }
            else dragging = false;
            lastMouse = mouse;

            if (!PointerOverUI)
            {
                float scroll = InputShim.Scroll;
                if (Mathf.Abs(scroll) > 0.01f) Distance = Mathf.Clamp(Distance * Mathf.Pow(0.88f, scroll), MinDistance, MaxDistance);
            }

            Yaw += InputShim.RotateAxis * 90f * Time.unscaledDeltaTime;
            Vector2 pan = InputShim.PanAxis;
            if (pan.sqrMagnitude > 0)
            {
                Quaternion flat = Quaternion.Euler(0, Yaw, 0);
                Vector3 move = flat * new Vector3(pan.x, 0, pan.y) * (Distance * 0.6f) * Time.unscaledDeltaTime;
                Target = Vector3.ClampMagnitude(Target + move, PanLimit);
            }

            float k = 1f - Mathf.Exp(-12f * Time.unscaledDeltaTime);
            yaw = Mathf.LerpAngle(yaw, Yaw, k);
            pitch = Mathf.Lerp(pitch, Pitch, k);
            distance = Mathf.Lerp(distance, Distance, k);
            target = Vector3.Lerp(target, Target, k);
            Apply();
        }

        void Apply()
        {
            Quaternion rot = Quaternion.Euler(pitch, yaw, 0);
            transform.position = target - rot * Vector3.forward * distance;
            transform.rotation = rot;
        }
    }
}
