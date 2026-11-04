using System.Collections.Generic;
using Hexhaven.Core;
using UnityEngine;

namespace Hexhaven
{
    public enum BuildMode { None, Path, Outpost, Keep, Storm }

    /// <summary>
    /// Scene entry point: owns the rules engine, the board view, the camera and input.
    /// The HUD lives in HexhavenGame.Hud.cs. Drop this on an empty GameObject (or let the
    /// bootstrap create it) and press Play.
    /// </summary>
    public partial class HexhavenGame : MonoBehaviour
    {
        [Header("Seats")]
        public int SeatCount = 4;
        public string[] SeatNames = { "You", "Bryn", "Corra", "Dain" };
        public bool[] SeatIsBot = { false, true, true, true };

        [Header("Pacing")]
        [Range(0.05f, 2f)] public float BotDelay = 0.6f;

        Game game;
        BoardView view;
        CameraRig rig;
        Camera cam;

        bool running;          // false while the start menu is open
        string seedText;
        int seed;

        BuildMode mode;
        PickKind candidateKind;
        readonly List<int> candidates = new List<int>();
        int hovered = -1, hoverTile = -1;
        float botTimer;
        int viewer;            // human seat whose hand is displayed

        // marker refresh tracking
        int shownVersion = -1, shownHover = -2;
        BuildMode shownMode = (BuildMode)(-1);

        public Game State { get { return game; } }

        void Awake()
        {
            // guard against arrays shortened in the Inspector
            if (SeatNames == null || SeatNames.Length < 4) System.Array.Resize(ref SeatNames, 4);
            if (SeatIsBot == null || SeatIsBot.Length < 4) System.Array.Resize(ref SeatIsBot, 4);
            EnsureCameraAndLight();
            view = new GameObject("BoardView").AddComponent<BoardView>();
            view.transform.SetParent(transform, false);
            seed = Random.Range(1, 100000);
            seedText = seed.ToString();
            CreateGame(true);
        }

        void EnsureCameraAndLight()
        {
            cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera") { tag = "MainCamera" };
                cam = go.AddComponent<Camera>();
                go.AddComponent<AudioListener>();
            }
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Palette.Sky;
            cam.fieldOfView = 45f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 200f;
            rig = cam.GetComponent<CameraRig>();
            if (rig == null) rig = cam.gameObject.AddComponent<CameraRig>();

            Light sun = null;
            foreach (var l in FindObjectsByTypeCompat<Light>()) if (l.type == LightType.Directional) { sun = l; break; }
            if (sun == null)
            {
                sun = new GameObject("Sun").AddComponent<Light>();
                sun.type = LightType.Directional;
            }
            sun.transform.rotation = Quaternion.Euler(52f, -35f, 0f);
            sun.intensity = 1.15f;
            sun.color = new Color(1f, 0.96f, 0.88f);
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.6f;

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.70f, 0.78f, 0.88f);
            RenderSettings.ambientEquatorColor = new Color(0.55f, 0.58f, 0.60f);
            RenderSettings.ambientGroundColor = new Color(0.35f, 0.32f, 0.30f);
        }

        static T[] FindObjectsByTypeCompat<T>() where T : Object
        {
#if UNITY_2023_1_OR_NEWER
            return FindObjectsByType<T>(FindObjectsSortMode.None);
#else
            return FindObjectsOfType<T>();
#endif
        }

        void CreateGame(bool preview)
        {
            int n = Mathf.Clamp(SeatCount, 3, 4);
            var names = new List<string>();
            var bots = new List<bool>();
            for (int i = 0; i < n; i++)
            {
                string nm = i < SeatNames.Length && !string.IsNullOrEmpty(SeatNames[i]) ? SeatNames[i] : "Player " + (i + 1);
                names.Add(nm);
                bots.Add(preview || (i < SeatIsBot.Length && SeatIsBot[i]));
            }
            game = new Game(seed, names, bots);
            view.Build(game, seed);
            running = !preview;
            mode = BuildMode.None;
            tradeOpen = false;
            botTimer = 0;
            shownVersion = -1;
            viewer = 0;
            for (int i = 0; i < n; i++) if (!bots[i]) { viewer = i; break; }
        }

        public void StartGame()
        {
            int parsed;
            seed = int.TryParse(seedText, out parsed) ? parsed : seedText.GetHashCode();
            CreateGame(false);
        }

        public void OpenMenu()
        {
            running = false;
            mode = BuildMode.None;
        }

        // ------------------------------------------------------------------ frame loop

        void Update()
        {
            if (game == null) return;
            view.Sync();
            rig.PointerOverUI = PointerOverUI();

            if (!running || game.Phase == Phase.GameOver)
            {
                SetCandidates(BuildMode.None);
                RefreshMarkers();
                return;
            }

            var cp = game.CurrentPlayer;
            if (cp.IsBot)
            {
                mode = BuildMode.None;
                tradeOpen = false;
                SetCandidates(BuildMode.None);
                botTimer += Time.deltaTime;
                float delay = game.InSetup ? BotDelay * 0.75f : BotDelay;
                if (botTimer >= delay)
                {
                    botTimer = 0;
                    Bot.Step(game);
                }
            }
            else
            {
                viewer = game.Current;
                HumanInput();
            }
            RefreshMarkers();
        }

        void HumanInput()
        {
            switch (game.Phase)
            {
                case Phase.Setup: mode = game.SetupNeedsPath ? BuildMode.Path : BuildMode.Outpost; break;
                case Phase.MoveStorm: mode = BuildMode.Storm; break;
                case Phase.Roll:
                    mode = BuildMode.None;
                    if (InputShim.RollKey) game.RollDice();
                    break;
                case Phase.Main:
                    if (InputShim.Cancel) { mode = BuildMode.None; tradeOpen = false; }
                    if (mode == BuildMode.Storm) mode = BuildMode.None;
                    break;
            }

            SetCandidates(mode);
            if (game.Phase == Phase.Main && mode != BuildMode.None && candidates.Count == 0) mode = BuildMode.None;

            Ray ray = cam.ScreenPointToRay(InputShim.MousePosition);
            bool overUI = PointerOverUI();
            hovered = overUI ? -1 : view.Pick(ray, candidateKind, candidates);
            hoverTile = overUI ? -1 : view.TileUnder(ray);

            if (InputShim.LeftClick && !overUI && hovered >= 0)
            {
                bool ok = false;
                switch (mode)
                {
                    case BuildMode.Path: ok = game.PlacePath(hovered); break;
                    case BuildMode.Outpost: ok = game.PlaceOutpost(hovered); break;
                    case BuildMode.Keep: ok = game.Upgrade(hovered); break;
                    case BuildMode.Storm: ok = game.MoveStorm(hovered, -1); break;
                }
                if (ok && game.Phase == Phase.Main) mode = BuildMode.None;
            }
        }

        void SetCandidates(BuildMode m)
        {
            candidates.Clear();
            candidateKind = PickKind.None;
            if (game == null || m == BuildMode.None) return;
            int p = game.Current;
            switch (m)
            {
                case BuildMode.Path: candidateKind = PickKind.Edge; candidates.AddRange(game.LegalPaths(p)); break;
                case BuildMode.Outpost: candidateKind = PickKind.Vertex; candidates.AddRange(game.LegalOutposts(p)); break;
                case BuildMode.Keep: candidateKind = PickKind.Vertex; candidates.AddRange(game.LegalUpgrades(p)); break;
                case BuildMode.Storm:
                    candidateKind = PickKind.Tile;
                    foreach (var t in game.Board.Tiles) if (game.CanMoveStorm(t.Id)) candidates.Add(t.Id);
                    break;
            }
        }

        void RefreshMarkers()
        {
            if (shownVersion == game.Version && shownMode == mode && shownHover == hovered) return;
            shownVersion = game.Version; shownMode = mode; shownHover = hovered;
            Color c = mode == BuildMode.Storm ? Palette.Storm : Color.Lerp(Palette.PlayerColors[game.Current], Color.white, 0.45f);
            view.ShowMarkers(candidateKind, candidates, hovered, c);
        }

        void SelectMode(BuildMode m)
        {
            mode = mode == m ? BuildMode.None : m;
            tradeOpen = false;
        }
    }

    /// <summary>Creates the game automatically when Play is pressed in a scene that doesn't have one.</summary>
    static class HexhavenBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Object.FindAnyObjectByType<HexhavenGame>() == null)
                new GameObject("Hexhaven").AddComponent<HexhavenGame>();
        }
    }
}
