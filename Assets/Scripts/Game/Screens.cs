using System;
using System.Collections.Generic;
using KombiRush.Sim;
using UnityEngine;
using UnityEngine.UI;

namespace KombiRush.Game
{
    /// <summary>Menu, garage and end-of-shift screens.</summary>
    public sealed class Screens
    {
        private readonly Profile _profile;
        private readonly RectTransform _menu;
        private readonly RectTransform _garage;
        private readonly RectTransform _over;

        private readonly Text _menuBest;
        private readonly Text _menuWallet;
        private readonly Text _menuNews;
        private readonly Text _soundCaption;
        private readonly Text _garageWallet;
        private readonly Text _overTitle;
        private readonly Text _overReason;
        private readonly Text _overStats;
        private readonly Text _overBest;

        private readonly List<UpgradeRow> _rows = new List<UpgradeRow>();

        private sealed class UpgradeRow
        {
            public UpgradeId Id;
            public Text Name;
            public Text Blurb;
            public Text Cost;
            public Button Buy;
            public Image[] Pips;
        }

        public Screens(Transform canvas, Profile profile, Action onDrive, Action onGarage, Action onMenu, Action onToggleSound)
        {
            _profile = profile;

            // ---------------- menu ----------------
            _menu = UiKit.Group(canvas, "Menu");
            UiKit.Panel(_menu, "Backdrop", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(1200f, 2100f), Vector2.zero, new Color(0f, 0f, 0f, 0.45f));
            UiKit.Label(_menu, "Title", "KOMBI RUSH", 132, Palette.Accent, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(1000f, 160f), new Vector2(0f, -300f));
            UiKit.Label(_menu, "Tagline", "Mbare to town. Mind the potholes.", 44, Palette.Paper,
                TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(1000f, 60f),
                new Vector2(0f, -410f), FontStyle.Normal);

            _menuBest = UiKit.Label(_menu, "Best", "", 46, Palette.Paper, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(900f, 60f), new Vector2(0f, 190f));
            _menuWallet = UiKit.Label(_menu, "Wallet", "", 52, Palette.Accent, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(900f, 60f), new Vector2(0f, 110f));
            _menuNews = UiKit.Label(_menu, "News", "", 40, Palette.Good, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(900f, 60f), new Vector2(0f, 40f),
                FontStyle.Normal);

            UiKit.TextButton(_menu, "Drive", "DRIVE", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(620f, 170f), new Vector2(0f, -140f), Palette.Accent, Palette.Ink, 72, onDrive);
            UiKit.TextButton(_menu, "Garage", "GARAGE", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(620f, 130f), new Vector2(0f, -330f), Palette.PanelDim, Palette.Paper, 56, onGarage);
            Button sound = UiKit.TextButton(_menu, "Sound", "SOUND: ON", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(420f, 96f), new Vector2(0f, 150f), new Color(0f, 0f, 0f, 0.4f), Palette.Paper, 40,
                onToggleSound);
            _soundCaption = sound.GetComponentInChildren<Text>();

            // ---------------- garage ----------------
            _garage = UiKit.Group(canvas, "Garage");
            UiKit.Panel(_garage, "Backdrop", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(1200f, 2100f), Vector2.zero, new Color(0f, 0f, 0f, 0.55f));
            UiKit.Label(_garage, "Title", "GARAGE", 96, Palette.Accent, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(900f, 120f), new Vector2(0f, -150f));
            _garageWallet = UiKit.Label(_garage, "Wallet", "", 52, Palette.Paper, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(900f, 70f), new Vector2(0f, -250f));

            IReadOnlyList<UpgradeId> order = Profile.UpgradeOrder;
            for (int i = 0; i < order.Count; i++) _rows.Add(BuildUpgradeRow(order[i], -360f - i * 240f));

            UiKit.TextButton(_garage, "Back", "BACK", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(520f, 130f), new Vector2(0f, 120f), Palette.Accent, Palette.Ink, 56, onMenu);

            // ---------------- end of shift ----------------
            _over = UiKit.Group(canvas, "GameOver");
            UiKit.Panel(_over, "Backdrop", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(1200f, 2100f), Vector2.zero, new Color(0f, 0f, 0f, 0.55f));
            _overTitle = UiKit.Label(_over, "Title", "SHIFT OVER", 108, Palette.Accent, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(1000f, 130f), new Vector2(0f, -280f));
            _overReason = UiKit.Label(_over, "Reason", "", 46, Palette.Paper, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(1000f, 60f), new Vector2(0f, -390f),
                FontStyle.Normal);
            _overStats = UiKit.Label(_over, "Stats", "", 50, Palette.Paper, TextAnchor.UpperCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(1000f, 460f), new Vector2(0f, 180f),
                FontStyle.Normal);
            _overBest = UiKit.Label(_over, "Best", "", 46, Palette.Good, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(1000f, 60f), new Vector2(0f, -120f));

            UiKit.TextButton(_over, "Again", "DRIVE AGAIN", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(640f, 160f), new Vector2(0f, 480f), Palette.Accent, Palette.Ink, 62, onDrive);
            UiKit.TextButton(_over, "Garage", "GARAGE", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(640f, 130f), new Vector2(0f, 310f), Palette.PanelDim, Palette.Paper, 52, onGarage);
            UiKit.TextButton(_over, "Menu", "MENU", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(640f, 120f), new Vector2(0f, 160f), new Color(0f, 0f, 0f, 0.4f), Palette.Paper, 46, onMenu);

            ShowNone();
        }

        private UpgradeRow BuildUpgradeRow(UpgradeId id, float y)
        {
            Image card = UiKit.Panel(_garage, "Row" + id, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(960f, 210f), new Vector2(0f, y), new Color(1f, 1f, 1f, 0.08f));

            var row = new UpgradeRow { Id = id, Pips = new Image[UpgradeSet.MaxLevel] };
            row.Name = UiKit.Label(card.transform, "Name", Economy.DisplayName(id), 52, Palette.Paper,
                TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(600f, 60f),
                new Vector2(30f, -22f));
            row.Blurb = UiKit.Label(card.transform, "Blurb", Economy.Blurb(id), 32, new Color(1f, 1f, 1f, 0.72f),
                TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(600f, 50f),
                new Vector2(30f, -84f), FontStyle.Normal);

            for (int i = 0; i < UpgradeSet.MaxLevel; i++)
            {
                row.Pips[i] = UiKit.Panel(card.transform, "Pip" + i, new Vector2(0f, 0f), new Vector2(0f, 0f),
                    new Vector2(46f, 20f), new Vector2(30f + i * 58f, 28f), new Color(1f, 1f, 1f, 0.2f));
            }

            row.Buy = UiKit.TextButton(card.transform, "Buy", "BUY", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(250f, 110f), new Vector2(-26f, -18f), Palette.Good, Palette.Paper, 46,
                () => Purchase(id));
            row.Cost = UiKit.Label(card.transform, "Cost", "", 38, Palette.Accent, TextAnchor.MiddleRight,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(300f, 50f), new Vector2(-26f, -26f));
            return row;
        }

        private void Purchase(UpgradeId id)
        {
            if (_profile.Buy(id)) SaveIO.Save(_profile);
            RefreshGarage();
        }

        public void ShowNone()
        {
            _menu.gameObject.SetActive(false);
            _garage.gameObject.SetActive(false);
            _over.gameObject.SetActive(false);
        }

        public void ShowMenu(string news)
        {
            ShowNone();
            _menu.gameObject.SetActive(true);
            _menuBest.text = _profile.BestDistanceMetres > 0f
                ? "Best shift: " + UiKit.Distance(_profile.BestDistanceMetres)
                : "No shift driven yet";
            _menuWallet.text = "Wallet: " + Economy.FormatCents(_profile.WalletCents);
            _menuNews.text = news ?? "";
            _soundCaption.text = _profile.SoundOn ? "SOUND: ON" : "SOUND: OFF";
        }

        public void ShowGarage()
        {
            ShowNone();
            _garage.gameObject.SetActive(true);
            RefreshGarage();
        }

        public void ShowGameOver(RunResult run, bool newBest)
        {
            ShowNone();
            _over.gameObject.SetActive(true);
            _overTitle.text = run.Reason == EndReason.OutOfFuel ? "OUT OF FUEL" : "KOMBI WRECKED";
            _overReason.text = run.Reason == EndReason.OutOfFuel
                ? "The tank ran dry before the next station."
                : "Too many knocks. The body is finished.";
            _overStats.text =
                "Distance      " + UiKit.Distance(run.DistanceMetres) + "\n" +
                "Fares         " + Economy.FormatCents(run.CashCents) + "\n" +
                "Passengers    " + run.PassengersServed + "\n" +
                "Stops served  " + run.StopsServed + "\n" +
                "Best combo    x" + run.BestCombo.ToString("0.00") + "\n" +
                "Shift time    " + Mathf.FloorToInt(run.DurationSeconds / 60f) + "m " +
                    (Mathf.FloorToInt(run.DurationSeconds) % 60) + "s";
            _overBest.text = newBest ? "NEW PERSONAL BEST" : "";
        }

        private void RefreshGarage()
        {
            _garageWallet.text = "Wallet: " + Economy.FormatCents(_profile.WalletCents);
            for (int i = 0; i < _rows.Count; i++)
            {
                UpgradeRow row = _rows[i];
                int level = _profile.Upgrades.Level(row.Id);
                for (int p = 0; p < row.Pips.Length; p++)
                    row.Pips[p].color = p < level ? Palette.Accent : new Color(1f, 1f, 1f, 0.2f);

                bool maxed = _profile.Upgrades.IsMaxed(row.Id);
                int cost = Economy.UpgradeCost(row.Id, level);
                row.Cost.text = maxed ? "MAXED" : Economy.FormatCents(cost);
                row.Buy.interactable = !maxed && _profile.WalletCents >= cost;
                Text caption = row.Buy.GetComponentInChildren<Text>();
                if (caption != null) caption.text = maxed ? "DONE" : "BUY";
                row.Buy.targetGraphic.color = maxed
                    ? new Color(1f, 1f, 1f, 0.15f)
                    : (row.Buy.interactable ? Palette.Good : new Color(1f, 1f, 1f, 0.15f));
            }
        }
    }
}
