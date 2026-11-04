using System.Collections.Generic;
using Hexhaven.Core;
using UnityEngine;

namespace Hexhaven
{
    /// <summary>IMGUI heads-up display: start menu, scoreboard, hand, build/trade actions, log.</summary>
    public partial class HexhavenGame
    {
        const float VirtualHeight = 900f;

        bool tradeOpen;
        int tradeGive = -1, tradeGet = -1;

        GUIStyle panel, title, label, small, bigNumber, button, centered;
        Texture2D panelTex, whiteTex;
        float uiScale = 1f;
        readonly List<Rect> uiRects = new List<Rect>();
        readonly List<Rect> uiRectsBuilding = new List<Rect>();

        bool PointerOverUI()
        {
            Vector2 m = InputShim.MousePosition;
            Vector2 v = new Vector2(m.x / uiScale, (Screen.height - m.y) / uiScale);
            foreach (var r in uiRects) if (r.Contains(v)) return true;
            return false;
        }

        void InitStyles()
        {
            if (panel != null) return;
            panelTex = new Texture2D(1, 1);
            panelTex.SetPixel(0, 0, new Color(0.08f, 0.10f, 0.14f, 0.82f));
            panelTex.Apply();
            whiteTex = Texture2D.whiteTexture;

            panel = new GUIStyle(GUI.skin.box) { padding = new RectOffset(12, 12, 10, 10) };
            panel.normal.background = panelTex;
            title = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold };
            title.normal.textColor = Color.white;
            label = new GUIStyle(GUI.skin.label) { fontSize = 15, richText = true };
            label.normal.textColor = new Color(0.92f, 0.93f, 0.95f);
            small = new GUIStyle(label) { fontSize = 12 };
            centered = new GUIStyle(label) { alignment = TextAnchor.MiddleCenter };
            bigNumber = new GUIStyle(label) { fontSize = 28, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            button = new GUIStyle(GUI.skin.button) { fontSize = 14, richText = true, wordWrap = true };
        }

        Rect Panel(Rect r)
        {
            uiRectsBuilding.Add(r);
            GUI.Box(r, GUIContent.none, panel);
            return r;
        }

        void Swatch(Rect r, Color c)
        {
            var old = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, whiteTex);
            GUI.color = old;
        }

        static string Hex(Color c) { return "#" + ColorUtility.ToHtmlStringRGB(c); }

        void OnGUI()
        {
            if (game == null) return;
            InitStyles();
            uiScale = Screen.height / VirtualHeight;
            GUI.matrix = Matrix4x4.Scale(new Vector3(uiScale, uiScale, 1));
            float W = Screen.width / uiScale, H = VirtualHeight;
            if (Event.current.type == EventType.Layout) uiRectsBuilding.Clear();

            if (!running) DrawMenu(W, H);
            else
            {
                DrawScoreboard();
                DrawTurnPanel(W);
                DrawLog(W, H);
                DrawActionBar(W, H);
                DrawTooltip();
                if (game.Phase == Phase.GameOver) DrawGameOver(W, H);
            }

            if (Event.current.type == EventType.Repaint)
            {
                uiRects.Clear();
                uiRects.AddRange(uiRectsBuilding);
            }
        }

        // ------------------------------------------------------------------ start menu

        void DrawMenu(float W, float H)
        {
            var r = Panel(new Rect(W / 2 - 270, H / 2 - 250, 540, 500));
            GUILayout.BeginArea(new Rect(r.x + 24, r.y + 18, r.width - 48, r.height - 36));
            GUILayout.Label("HEXHAVEN", new GUIStyle(title) { fontSize = 40, alignment = TextAnchor.MiddleCenter });
            GUILayout.Label("Settle the isles. Trade, build, and be first to " + Game.PointsToWin + " points.", centered);
            GUILayout.Space(16);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Players", label, GUILayout.Width(110));
            if (GUILayout.Toggle(SeatCount == 3, " 3", button, GUILayout.Width(60))) SeatCount = 3;
            if (GUILayout.Toggle(SeatCount == 4, " 4", button, GUILayout.Width(60))) SeatCount = 4;
            GUILayout.EndHorizontal();
            GUILayout.Space(8);

            for (int i = 0; i < SeatCount; i++)
            {
                GUILayout.BeginHorizontal();
                Rect sw = GUILayoutUtility.GetRect(22, 22, GUILayout.Width(22));
                Swatch(new Rect(sw.x, sw.y + 4, 18, 18), Palette.PlayerColors[i]);
                GUILayout.Label(Palette.PlayerColorNames[i], label, GUILayout.Width(80));
                SeatNames[i] = GUILayout.TextField(SeatNames[i] ?? "", 16, GUILayout.Width(170));
                if (GUILayout.Button(SeatIsBot[i] ? "Computer" : "Human", button, GUILayout.Width(110))) SeatIsBot[i] = !SeatIsBot[i];
                GUILayout.EndHorizontal();
                GUILayout.Space(4);
            }

            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Island seed", label, GUILayout.Width(110));
            string newSeed = GUILayout.TextField(seedText, 9, GUILayout.Width(120));
            if (newSeed != seedText) { seedText = newSeed; PreviewSeed(); }
            if (GUILayout.Button("Shuffle", button, GUILayout.Width(90))) { seedText = Random.Range(1, 100000).ToString(); PreviewSeed(); }
            GUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();
            GUILayout.Label("Right-drag to orbit · scroll to zoom · WASD to pan · Q/E rotate · Space rolls · Esc cancels", new GUIStyle(small) { alignment = TextAnchor.MiddleCenter, wordWrap = true });
            GUILayout.Space(8);
            if (GUILayout.Button("<size=20><b>Start game</b></size>", button, GUILayout.Height(48))) StartGame();
            GUILayout.EndArea();
        }

        void PreviewSeed()
        {
            int parsed;
            seed = int.TryParse(seedText, out parsed) ? parsed : seedText.GetHashCode();
            CreateGame(true);
        }

        // ------------------------------------------------------------------ scoreboard

        void DrawScoreboard()
        {
            int n = game.Players.Count;
            var r = Panel(new Rect(12, 12, 320, 46 + n * 46));
            GUI.Label(new Rect(r.x + 12, r.y + 8, 300, 26), "Round " + game.TurnNumber + (game.InSetup ? " · Setup" : ""), title);
            for (int i = 0; i < n; i++)
            {
                var p = game.Players[i];
                float y = r.y + 42 + i * 46;
                if (i == game.Current) Swatch(new Rect(r.x + 4, y - 2, r.width - 8, 42), new Color(1, 1, 1, 0.08f));
                Swatch(new Rect(r.x + 12, y + 6, 26, 26), Palette.PlayerColors[i]);
                string tag = p.IsBot ? " <color=#9aa3ad>(cpu)</color>" : "";
                string badge = game.LongestPathHolder == i ? "  <color=#ffd34d>★ Longest Path</color>" : "";
                GUI.Label(new Rect(r.x + 46, y, 200, 22), "<b>" + p.Name + "</b>" + tag, label);
                GUI.Label(new Rect(r.x + 46, y + 19, 230, 20), p.Hand.Total + " cards · path " + p.LongestPath + badge, small);
                GUI.Label(new Rect(r.x + 250, y + 2, 60, 34), game.VictoryPoints(i).ToString(), bigNumber);
            }
        }

        // ------------------------------------------------------------------ turn / dice panel

        void DrawTurnPanel(float W)
        {
            var r = Panel(new Rect(W - 252, 12, 240, 168));
            var cp = game.CurrentPlayer;
            GUI.Label(new Rect(r.x + 12, r.y + 8, 220, 24), "<color=" + Hex(Palette.PlayerColors[cp.Id]) + ">■</color> " + cp.Name + "'s turn", label);
            string phase;
            switch (game.Phase)
            {
                case Phase.Setup: phase = game.SetupNeedsPath ? "Place a path" : "Place an outpost"; break;
                case Phase.Roll: phase = "Roll the dice"; break;
                case Phase.MoveStorm: phase = "Move the storm"; break;
                case Phase.Main: phase = "Build & trade"; break;
                default: phase = "Game over"; break;
            }
            GUI.Label(new Rect(r.x + 12, r.y + 30, 220, 20), phase, small);

            if (game.Die1 > 0)
            {
                DrawDie(new Rect(r.x + 14, r.y + 56, 48, 48), game.Die1);
                DrawDie(new Rect(r.x + 70, r.y + 56, 48, 48), game.Die2);
                GUI.Label(new Rect(r.x + 128, r.y + 56, 90, 48), "= " + game.DiceTotal, bigNumber);
            }
            GUI.Label(new Rect(r.x + 12, r.y + 112, 120, 20), "CPU speed", small);
            BotDelay = GUI.HorizontalSlider(new Rect(r.x + 90, r.y + 117, 136, 16), BotDelay, 1.5f, 0.05f);
            if (GUI.Button(new Rect(r.x + 12, r.y + 136, 216, 24), "New game / menu", button)) OpenMenu();
        }

        void DrawDie(Rect r, int value)
        {
            Swatch(r, new Color(0.97f, 0.96f, 0.92f));
            float s = r.width;
            var pips = new List<Vector2>();
            Vector2 c = new Vector2(0.5f, 0.5f), tl = new Vector2(0.25f, 0.25f), tr = new Vector2(0.75f, 0.25f),
                bl = new Vector2(0.25f, 0.75f), br = new Vector2(0.75f, 0.75f), ml = new Vector2(0.25f, 0.5f), mr = new Vector2(0.75f, 0.5f);
            if (value % 2 == 1) pips.Add(c);
            if (value >= 2) { pips.Add(tl); pips.Add(br); }
            if (value >= 4) { pips.Add(tr); pips.Add(bl); }
            if (value == 6) { pips.Add(ml); pips.Add(mr); }
            foreach (var p in pips) Swatch(new Rect(r.x + p.x * s - 4, r.y + p.y * s - 4, 8, 8), new Color(0.15f, 0.15f, 0.18f));
        }

        // ------------------------------------------------------------------ log

        void DrawLog(float W, float H)
        {
            var r = Panel(new Rect(W - 372, 192, 360, 232));
            GUI.Label(new Rect(r.x + 12, r.y + 6, 200, 22), "Chronicle", label);
            int lines = 10;
            int start = Mathf.Max(0, game.Log.Count - lines);
            for (int i = start; i < game.Log.Count; i++)
                GUI.Label(new Rect(r.x + 12, r.y + 28 + (i - start) * 20, r.width - 20, 20), game.Log[i], small);
        }

        // ------------------------------------------------------------------ action bar

        void DrawActionBar(float W, float H)
        {
            var me = game.Players[viewer];
            bool myTurn = game.Current == viewer && !game.CurrentPlayer.IsBot;
            float bw = 860;
            var r = Panel(new Rect(W / 2 - bw / 2, H - 162, bw, 150));

            // hand
            GUI.Label(new Rect(r.x + 14, r.y + 8, 300, 22), "<b>" + me.Name + "</b>'s hand", label);
            for (int i = 0; i < Res.Count; i++)
            {
                var cell = new Rect(r.x + 14 + i * 76, r.y + 34, 70, 66);
                Swatch(cell, Palette.ResourceColors[i] * 0.85f + new Color(0, 0, 0, 0.15f));
                GUI.Label(new Rect(cell.x, cell.y + 2, cell.width, 36), me.Hand[i].ToString(), bigNumber);
                GUI.Label(new Rect(cell.x, cell.y + 40, cell.width, 22), Res.Names[i], centered);
            }
            GUI.Label(new Rect(r.x + 14, r.y + 106, 380, 40),
                "Pieces left: " + me.PathsLeft + " paths · " + me.OutpostsLeft + " outposts · " + me.KeepsLeft + " keeps", small);

            // actions
            float ax = r.x + 410;
            GUI.enabled = myTurn;
            if (game.Phase == Phase.Roll)
            {
                if (GUI.Button(new Rect(ax, r.y + 14, 436, 122), "<size=26><b>Roll dice</b></size>\n<size=13>(Space)</size>", button)) game.RollDice();
            }
            else
            {
                bool main = myTurn && game.Phase == Phase.Main;
                ActionButton(new Rect(ax, r.y + 14, 140, 58), "Path", "1 Timber, 1 Clay", BuildMode.Path, main && game.LegalPaths(viewer).Count > 0);
                ActionButton(new Rect(ax + 148, r.y + 14, 140, 58), "Outpost", "Timber, Clay, Fiber, Grain", BuildMode.Outpost, main && game.LegalOutposts(viewer).Count > 0);
                ActionButton(new Rect(ax + 296, r.y + 14, 140, 58), "Keep", "2 Grain, 3 Iron", BuildMode.Keep, main && game.LegalUpgrades(viewer).Count > 0);

                GUI.enabled = main;
                if (GUI.Toggle(new Rect(ax, r.y + 78, 214, 58), tradeOpen, "<b>Trade with harbor</b>", button) != tradeOpen)
                {
                    tradeOpen = !tradeOpen;
                    mode = BuildMode.None;
                }
                if (GUI.Button(new Rect(ax + 222, r.y + 78, 214, 58), "<b>End turn</b>", button))
                {
                    mode = BuildMode.None;
                    tradeOpen = false;
                    game.EndTurn();
                }
                GUI.enabled = true;
            }
            GUI.enabled = true;

            // contextual hint
            string hint = Hint(myTurn);
            if (!string.IsNullOrEmpty(hint))
            {
                var hr = new Rect(W / 2 - 300, r.y - 40, 600, 32);
                Panel(hr);
                GUI.Label(hr, hint, centered);
            }

            if (tradeOpen && myTurn && game.Phase == Phase.Main) DrawTrade(new Rect(W / 2 - 300, r.y - 40 - 176, 600, 168));
            else tradeOpen = false;
        }

        void ActionButton(Rect rect, string name, string cost, BuildMode m, bool enabled)
        {
            GUI.enabled = enabled || mode == m;
            bool on = mode == m;
            string text = (on ? "<color=#ffd34d><b>" + name + "</b></color>" : "<b>" + name + "</b>") + "\n<size=11>" + cost + "</size>";
            if (GUI.Toggle(rect, on, text, button) != on) SelectMode(m);
            GUI.enabled = true;
        }

        string Hint(bool myTurn)
        {
            if (game.Phase == Phase.GameOver) return null;
            if (!myTurn) return game.CurrentPlayer.Name + " is thinking…";
            switch (mode)
            {
                case BuildMode.Outpost: return game.InSetup ? "Click a glowing corner to found an outpost" : "Click a glowing corner to build an outpost (Esc to cancel)";
                case BuildMode.Path: return game.InSetup ? "Click a glowing edge to lay a path" : "Click a glowing edge to build a path (Esc to cancel)";
                case BuildMode.Keep: return "Click one of your outposts to raise a keep (Esc to cancel)";
                case BuildMode.Storm: return "A 7! Click a tile to move the storm and plunder a neighbour";
            }
            return null;
        }

        void DrawTrade(Rect r)
        {
            Panel(r);
            var me = game.Players[viewer];
            GUI.Label(new Rect(r.x + 14, r.y + 8, 300, 22), "<b>Harbor trade</b>", label);
            GUI.Label(new Rect(r.x + 14, r.y + 36, 60, 22), "Give", label);
            GUI.Label(new Rect(r.x + 14, r.y + 82, 60, 22), "Get", label);
            for (int i = 0; i < Res.Count; i++)
            {
                int rate = game.TradeRate(viewer, i);
                GUI.enabled = me.Hand[i] >= rate;
                var gr = new Rect(r.x + 70 + i * 104, r.y + 30, 98, 38);
                if (GUI.Toggle(gr, tradeGive == i, Res.Names[i] + " <size=11>(" + rate + ":1)</size>", button)) tradeGive = i;
                else if (tradeGive == i) tradeGive = -1;
                GUI.enabled = true;
                var tr = new Rect(r.x + 70 + i * 104, r.y + 76, 98, 38);
                if (GUI.Toggle(tr, tradeGet == i, Res.Names[i], button)) tradeGet = i;
                else if (tradeGet == i) tradeGet = -1;
            }
            bool can = tradeGive >= 0 && tradeGet >= 0 && game.CanBankTrade(viewer, tradeGive, tradeGet);
            GUI.enabled = can;
            string label2 = can ? "Trade " + game.TradeRate(viewer, tradeGive) + " " + Res.Names[tradeGive] + " → 1 " + Res.Names[tradeGet] : "Pick what to give and what to get";
            if (GUI.Button(new Rect(r.x + 70, r.y + 122, 512, 36), label2, button))
            {
                game.BankTrade(tradeGive, tradeGet);
                if (tradeGive >= 0 && me.Hand[tradeGive] < game.TradeRate(viewer, tradeGive)) tradeGive = -1;
            }
            GUI.enabled = true;
        }

        // ------------------------------------------------------------------ tooltip & game over

        void DrawTooltip()
        {
            if (hoverTile < 0 || mode != BuildMode.None || PointerOverUI()) return;
            var t = game.Board.Tiles[hoverTile];
            string text = Palette.TerrainName(t.Terrain);
            if (t.Resource >= 0) text += " → " + Res.Names[t.Resource] + " on " + t.Number;
            if (t.Id == game.StormTile) text += " (storm: no harvest)";
            Vector2 m = InputShim.MousePosition;
            var r = new Rect(m.x / uiScale + 16, (Screen.height - m.y) / uiScale + 12, 300, 26);
            GUI.Box(r, GUIContent.none, panel);
            GUI.Label(new Rect(r.x + 8, r.y + 3, r.width - 12, 22), text, small);
        }

        void DrawGameOver(float W, float H)
        {
            var r = Panel(new Rect(W / 2 - 220, H / 2 - 120, 440, 200));
            var wp = game.Players[game.Winner];
            GUI.Label(new Rect(r.x, r.y + 24, r.width, 50), "<color=" + Hex(Palette.PlayerColors[wp.Id]) + ">" + wp.Name + "</color> wins!", new GUIStyle(title) { fontSize = 34, alignment = TextAnchor.MiddleCenter, richText = true });
            GUI.Label(new Rect(r.x, r.y + 78, r.width, 30), game.VictoryPoints(wp.Id) + " points in " + game.TurnNumber + " rounds", centered);
            if (GUI.Button(new Rect(r.x + 40, r.y + 126, 170, 44), "<b>Play again</b>", button)) { seedText = Random.Range(1, 100000).ToString(); StartGame(); }
            if (GUI.Button(new Rect(r.x + 230, r.y + 126, 170, 44), "Menu", button)) OpenMenu();
        }
    }
}
