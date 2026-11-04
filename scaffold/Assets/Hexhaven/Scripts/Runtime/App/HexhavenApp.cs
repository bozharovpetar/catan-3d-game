// SCAFFOLD — composition root skeleton. Fill in per docs/02-ARCHITECTURE.md §6 (milestones M5–M8).
using UnityEngine;

namespace Hexhaven
{
    /// <summary>Composition root. The only component in Main.unity; creates everything else at runtime.</summary>
    public sealed class HexhavenApp : MonoBehaviour
    {
        public static HexhavenApp Instance { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            // TODO(M5–M8): settings, camera rig, lights, world views, audio, UI, post FX, title screen (02 §6).
        }

        /// <summary>Lets "Play" work in any empty scene.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void EnsureApp()
        {
            if (Object.FindAnyObjectByType<HexhavenApp>() == null)
                new GameObject("HexhavenApp").AddComponent<HexhavenApp>();
        }
    }
}
