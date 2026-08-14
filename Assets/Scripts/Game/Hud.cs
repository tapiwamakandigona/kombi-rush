using KombiRush.Sim;
using UnityEngine;
using UnityEngine.UI;

namespace KombiRush.Game
{
    /// <summary>In-run HUD: fares, distance, fuel, hull, riders, combo, plus short-lived toasts.</summary>
    public sealed class Hud
    {
        private const int ToastCount = 5;

        private readonly RectTransform _root;
        private readonly Text _cash;
        private readonly Text _distance;
        private readonly Text _riders;
        private readonly Text _combo;
        private readonly Text _hint;
        private readonly Image _fuelFill;
        private readonly Image[] _hullPips = new Image[8];
        private readonly Text[] _toasts = new Text[ToastCount];
        private readonly float[] _toastLife = new float[ToastCount];
        private int _nextToast;
        private float _hintTimer = 4f;

        public Hud(Transform canvas)
        {
            _root = UiKit.Group(canvas, "Hud");

            Image cashChip = UiKit.Panel(_root, "CashChip", new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(330f, 96f), new Vector2(26f, -26f), Palette.PanelDim);
            _cash = UiKit.Label(cashChip.transform, "Cash", "$0.00", 52, Palette.Accent, TextAnchor.MiddleLeft,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(300f, 80f), new Vector2(26f, 0f));

            Image distChip = UiKit.Panel(_root, "DistChip", new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(300f, 96f), new Vector2(-26f, -26f), Palette.PanelDim);
            _distance = UiKit.Label(distChip.transform, "Distance", "0 m", 48, Palette.Paper, TextAnchor.MiddleRight,
                new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(270f, 80f), new Vector2(-26f, 0f));

            // fuel gauge
            Image fuelBack = UiKit.Panel(_root, "FuelBack", new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(330f, 40f), new Vector2(26f, -136f), new Color(0f, 0f, 0f, 0.55f));
            _fuelFill = UiKit.Panel(fuelBack.transform, "FuelFill", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(318f, 28f), new Vector2(6f, 0f), Palette.Good);
            UiKit.Label(fuelBack.transform, "FuelLabel", "FUEL", 24, new Color(1f, 1f, 1f, 0.85f), TextAnchor.MiddleLeft,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(120f, 30f), new Vector2(14f, 0f));

            // hull pips
            for (int i = 0; i < _hullPips.Length; i++)
            {
                Image pip = UiKit.Panel(_root, "Hull" + i, new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(34f, 34f), new Vector2(30f + i * 44f, -190f), Palette.Bad);
                _hullPips[i] = pip;
                pip.gameObject.SetActive(false);
            }

            Image ridersChip = UiKit.Panel(_root, "RidersChip", new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(240f, 64f), new Vector2(-26f, -136f), Palette.PanelDim);
            _riders = UiKit.Label(ridersChip.transform, "Riders", "0/8", 40, Palette.Paper, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(230f, 60f), Vector2.zero);

            _combo = UiKit.Label(_root, "Combo", "", 60, Palette.Accent, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(400f, 80f), new Vector2(0f, -150f));

            _hint = UiKit.Label(_root, "Hint", "Tap a side or slide to steer", 38, new Color(1f, 1f, 1f, 0.85f),
                TextAnchor.MiddleCenter, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(900f, 60f),
                new Vector2(0f, 190f), FontStyle.Normal);

            for (int i = 0; i < ToastCount; i++)
            {
                _toasts[i] = UiKit.Label(_root, "Toast" + i, "", 56, Palette.Accent, TextAnchor.MiddleCenter,
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(800f, 70f), Vector2.zero);
                _toasts[i].gameObject.SetActive(false);
            }
        }

        public void SetVisible(bool visible) => _root.gameObject.SetActive(visible);

        public void ResetForRun()
        {
            _hintTimer = 4f;
            _hint.gameObject.SetActive(true);
            for (int i = 0; i < ToastCount; i++)
            {
                _toastLife[i] = 0f;
                _toasts[i].gameObject.SetActive(false);
            }
        }

        public void Toast(string text, Color color)
        {
            Text t = _toasts[_nextToast];
            _toastLife[_nextToast] = 1.1f;
            _nextToast = (_nextToast + 1) % ToastCount;
            t.text = text;
            t.color = color;
            t.gameObject.SetActive(true);
            t.rectTransform.anchoredPosition = new Vector2(Random.Range(-90f, 90f), Random.Range(-40f, 60f));
        }

        public void Sync(RoadSim sim, float deltaTime)
        {
            _cash.text = Economy.FormatCents(sim.CashCents + sim.BankedFareCents);
            _distance.text = UiKit.Distance(sim.DistanceMetres);
            _riders.text = sim.Riders + "/" + sim.SeatCapacity;

            float fuelFraction = Mathf.Clamp01(sim.Fuel / Mathf.Max(1f, sim.FuelCapacity));
            _fuelFill.rectTransform.sizeDelta = new Vector2(318f * fuelFraction, 28f);
            _fuelFill.color = fuelFraction < 0.2f ? Palette.Bad : fuelFraction < 0.45f ? Palette.Accent : Palette.Good;

            for (int i = 0; i < _hullPips.Length; i++)
            {
                bool used = i < sim.HullMax;
                _hullPips[i].gameObject.SetActive(used);
                if (used) _hullPips[i].color = i < sim.Hull ? Palette.Bad : new Color(1f, 1f, 1f, 0.18f);
            }

            _combo.text = sim.Combo > 1.01f ? "x" + sim.Combo.ToString("0.00") : "";

            if (_hintTimer > 0f)
            {
                _hintTimer -= deltaTime;
                if (_hintTimer <= 0f) _hint.gameObject.SetActive(false);
            }

            for (int i = 0; i < ToastCount; i++)
            {
                if (_toastLife[i] <= 0f) continue;
                _toastLife[i] -= deltaTime;
                Text t = _toasts[i];
                Vector2 pos = t.rectTransform.anchoredPosition;
                pos.y += 90f * deltaTime;
                t.rectTransform.anchoredPosition = pos;
                Color c = t.color;
                c.a = Mathf.Clamp01(_toastLife[i] / 0.6f);
                t.color = c;
                if (_toastLife[i] <= 0f) t.gameObject.SetActive(false);
            }
        }
    }
}
