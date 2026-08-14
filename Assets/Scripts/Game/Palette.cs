using UnityEngine;

namespace KombiRush.Game
{
    /// <summary>
    /// Colours for the whole game. Warm dusty road, high-contrast UI so it stays readable in
    /// sunlight on a cheap LCD panel, with Zimbabwe flag accents on the kombi and the HUD.
    /// </summary>
    public static class Palette
    {
        public static readonly Color Tarmac = new Color32(58, 58, 62, 255);
        public static readonly Color TarmacDark = new Color32(48, 48, 52, 255);
        public static readonly Color LaneLine = new Color32(226, 226, 214, 255);
        public static readonly Color Kerb = new Color32(196, 190, 176, 255);
        public static readonly Color Dust = new Color32(158, 126, 82, 255);
        public static readonly Color DustDark = new Color32(132, 103, 64, 255);
        public static readonly Color Grass = new Color32(94, 118, 62, 255);

        public static readonly Color KombiBody = new Color32(244, 244, 238, 255);
        public static readonly Color KombiStripeGreen = new Color32(0, 138, 61, 255);
        public static readonly Color KombiStripeGold = new Color32(252, 209, 22, 255);
        public static readonly Color KombiStripeRed = new Color32(206, 17, 38, 255);
        public static readonly Color Glass = new Color32(96, 152, 176, 255);
        public static readonly Color Tyre = new Color32(28, 28, 30, 255);

        public static readonly Color TrafficA = new Color32(198, 66, 52, 255);
        public static readonly Color TrafficB = new Color32(64, 96, 168, 255);
        public static readonly Color TrafficC = new Color32(212, 168, 60, 255);
        public static readonly Color Pothole = new Color32(24, 22, 24, 255);
        public static readonly Color PotholeRim = new Color32(84, 78, 72, 255);
        public static readonly Color BarrierOrange = new Color32(232, 118, 32, 255);
        public static readonly Color BarrierWhite = new Color32(240, 238, 232, 255);

        public static readonly Color Passenger = new Color32(232, 196, 148, 255);
        public static readonly Color PassengerShirt = new Color32(72, 132, 196, 255);
        public static readonly Color Coin = new Color32(250, 206, 62, 255);
        public static readonly Color CoinEdge = new Color32(196, 148, 24, 255);
        public static readonly Color Fuel = new Color32(228, 74, 62, 255);
        public static readonly Color StopSign = new Color32(0, 138, 61, 255);

        public static readonly Color Ink = new Color32(24, 24, 28, 255);
        public static readonly Color Paper = new Color32(248, 246, 240, 255);
        public static readonly Color PanelDim = new Color32(18, 20, 24, 220);
        public static readonly Color Accent = new Color32(252, 209, 22, 255);
        public static readonly Color Good = new Color32(0, 168, 74, 255);
        public static readonly Color Bad = new Color32(214, 58, 44, 255);
    }
}
