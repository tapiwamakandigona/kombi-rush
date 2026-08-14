using System.Collections.Generic;
using UnityEngine;

namespace KombiRush.Game
{
    /// <summary>
    /// Bakes every sprite the game needs at startup. Keeping the art procedural means the APK
    /// carries no texture payload (it matters when players install over metered data) and the
    /// look is tuned in code rather than in an art pipeline.
    /// </summary>
    public static class SpriteFactory
    {
        public const float PixelsPerUnit = 64f;
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        public static void Clear() => Cache.Clear();

        private static Sprite Get(string key, System.Func<Sprite> build)
        {
            if (Cache.TryGetValue(key, out Sprite cached) && cached != null) return cached;
            Sprite made = build();
            Cache[key] = made;
            return made;
        }

        /// <summary>1x1 white sprite, tinted and stretched for road surfaces and UI blocks.</summary>
        public static Sprite Solid => Get("solid", () =>
        {
            var c = new Canvas2D(4, 4);
            c.Clear(Color.white);
            return c.ToSprite(4f);
        });

        public static Sprite SoftShadow => Get("shadow", () =>
        {
            var c = new Canvas2D(96, 48);
            c.Clear(new Color(0, 0, 0, 0));
            for (int i = 6; i >= 1; i--)
                c.Ellipse(48f, 24f, 40f * i / 6f, 18f * i / 6f, new Color(0f, 0f, 0f, 0.06f));
            return c.ToSprite(PixelsPerUnit);
        });

        public static Sprite Kombi => Get("kombi", () =>
        {
            const int W = 80, H = 144;
            var c = new Canvas2D(W, H);
            c.Clear(new Color(0, 0, 0, 0));
            float cx = W * 0.5f;

            // tyres poking out of the body
            c.RoundRect(cx - 33f, H * 0.28f, 5f, 13f, 3f, Palette.Tyre);
            c.RoundRect(cx + 33f, H * 0.28f, 5f, 13f, 3f, Palette.Tyre);
            c.RoundRect(cx - 33f, H * 0.74f, 5f, 13f, 3f, Palette.Tyre);
            c.RoundRect(cx + 33f, H * 0.74f, 5f, 13f, 3f, Palette.Tyre);

            // body with a dark outline for contrast against tarmac
            c.RoundRect(cx, H * 0.5f, 31f, 66f, 13f, Palette.Ink);
            c.RoundRect(cx, H * 0.5f, 29f, 64f, 12f, Palette.KombiBody);
            c.ShadeVertical(cx - 27f, H * 0.5f - 60f, cx + 27f, H * 0.5f + 60f,
                new Color(1f, 1f, 1f, 0.18f), new Color(0f, 0f, 0f, 0.10f));

            // windscreen and rear window
            c.RoundRect(cx, H - 26f, 22f, 12f, 6f, Palette.Glass);
            c.RoundRect(cx, 24f, 20f, 9f, 5f, new Color(Palette.Glass.r, Palette.Glass.g, Palette.Glass.b, 0.85f));

            // side windows
            for (int i = 0; i < 3; i++)
            {
                float y = H * 0.42f + i * 20f;
                c.RoundRect(cx - 21f, y, 6f, 8f, 3f, Palette.Glass);
                c.RoundRect(cx + 21f, y, 6f, 8f, 3f, Palette.Glass);
            }

            // Zimbabwe flag band along the flank
            c.Rect(cx - 29f, H * 0.30f, cx + 29f, H * 0.30f + 4f, Palette.KombiStripeGreen);
            c.Rect(cx - 29f, H * 0.30f + 4f, cx + 29f, H * 0.30f + 7f, Palette.KombiStripeGold);
            c.Rect(cx - 29f, H * 0.30f + 7f, cx + 29f, H * 0.30f + 10f, Palette.KombiStripeRed);

            // destination board and headlights
            c.RoundRect(cx, H - 12f, 16f, 5f, 2f, Palette.Ink);
            c.RoundRect(cx, H - 12f, 14f, 3f, 1f, Palette.KombiStripeGold);
            c.RoundRect(cx - 22f, H - 8f, 5f, 3f, 2f, Palette.Accent);
            c.RoundRect(cx + 22f, H - 8f, 5f, 3f, 2f, Palette.Accent);
            return c.ToSprite(PixelsPerUnit);
        });

        public static Sprite Traffic(int variant) => Get("traffic" + variant, () =>
        {
            const int W = 72, H = 120;
            Color body = variant % 3 == 0 ? Palette.TrafficA : variant % 3 == 1 ? Palette.TrafficB : Palette.TrafficC;
            var c = new Canvas2D(W, H);
            c.Clear(new Color(0, 0, 0, 0));
            float cx = W * 0.5f;

            c.RoundRect(cx - 29f, H * 0.30f, 4f, 11f, 2f, Palette.Tyre);
            c.RoundRect(cx + 29f, H * 0.30f, 4f, 11f, 2f, Palette.Tyre);
            c.RoundRect(cx - 29f, H * 0.72f, 4f, 11f, 2f, Palette.Tyre);
            c.RoundRect(cx + 29f, H * 0.72f, 4f, 11f, 2f, Palette.Tyre);

            c.RoundRect(cx, H * 0.5f, 27f, 54f, 14f, Palette.Ink);
            c.RoundRect(cx, H * 0.5f, 25f, 52f, 13f, body);
            c.ShadeVertical(cx - 24f, 8f, cx + 24f, H - 8f, new Color(0f, 0f, 0f, 0.14f), new Color(1f, 1f, 1f, 0.14f));

            // cabin glass, plus red tail lights at the bottom because it is coming at you
            c.RoundRect(cx, H * 0.60f, 19f, 14f, 6f, Palette.Glass);
            c.RoundRect(cx, H * 0.34f, 16f, 8f, 4f, new Color(Palette.Glass.r, Palette.Glass.g, Palette.Glass.b, 0.8f));
            c.RoundRect(cx - 17f, 12f, 5f, 3f, 2f, Palette.Bad);
            c.RoundRect(cx + 17f, 12f, 5f, 3f, 2f, Palette.Bad);
            return c.ToSprite(PixelsPerUnit);
        });

        public static Sprite Pothole => Get("pothole", () =>
        {
            const int W = 112, H = 72;
            var c = new Canvas2D(W, H);
            c.Clear(new Color(0, 0, 0, 0));
            c.Ellipse(W * 0.5f, H * 0.5f, 52f, 31f, Palette.PotholeRim);
            c.Ellipse(W * 0.5f, H * 0.5f + 2f, 46f, 26f, Palette.Pothole);
            // a little standing water so it reads at a glance on a small screen
            c.Ellipse(W * 0.5f - 10f, H * 0.5f + 6f, 16f, 7f, new Color(0.42f, 0.52f, 0.56f, 0.5f));
            return c.ToSprite(PixelsPerUnit);
        });

        public static Sprite Roadblock => Get("roadblock", () =>
        {
            const int W = 208, H = 64;
            var c = new Canvas2D(W, H);
            c.Clear(new Color(0, 0, 0, 0));
            // trestle legs
            c.RoundRect(24f, H * 0.34f, 5f, 14f, 2f, Palette.Ink);
            c.RoundRect(W - 24f, H * 0.34f, 5f, 14f, 2f, Palette.Ink);
            // striped plank
            c.RoundRect(W * 0.5f, H * 0.62f, W * 0.5f - 6f, 13f, 4f, Palette.BarrierWhite);
            for (int i = -8; i < 9; i++)
            {
                float x = W * 0.5f + i * 24f;
                c.Line(x - 10f, H * 0.62f - 12f, x + 10f, H * 0.62f + 12f, 11f, Palette.BarrierOrange);
            }
            c.RoundRect(W * 0.5f, H * 0.62f, W * 0.5f - 6f, 13f, 4f, new Color(0f, 0f, 0f, 0f));
            c.Line(8f, H * 0.62f + 13f, W - 8f, H * 0.62f + 13f, 2f, Palette.Ink);
            c.Line(8f, H * 0.62f - 13f, W - 8f, H * 0.62f - 13f, 2f, Palette.Ink);
            return c.ToSprite(PixelsPerUnit);
        });

        public static Sprite Passenger => Get("passenger", () =>
        {
            const int W = 60, H = 84;
            var c = new Canvas2D(W, H);
            c.Clear(new Color(0, 0, 0, 0));
            float cx = W * 0.5f;
            // raised arm - they are hailing the kombi
            c.Line(cx + 10f, H * 0.52f, cx + 20f, H * 0.80f, 7f, Palette.Passenger);
            c.RoundRect(cx, H * 0.40f, 15f, 22f, 7f, Palette.PassengerShirt);   // torso
            c.Ellipse(cx, H * 0.72f, 13f, 13f, Palette.Passenger);              // head
            c.Ellipse(cx, H * 0.78f, 13f, 8f, new Color(0.16f, 0.13f, 0.12f, 1f)); // hair
            c.RoundRect(cx - 7f, H * 0.14f, 5f, 10f, 3f, Palette.Ink);          // legs
            c.RoundRect(cx + 7f, H * 0.14f, 5f, 10f, 3f, Palette.Ink);
            return c.ToSprite(PixelsPerUnit);
        });

        public static Sprite Coin => Get("coin", () =>
        {
            const int S = 52;
            var c = new Canvas2D(S, S);
            c.Clear(new Color(0, 0, 0, 0));
            c.Ellipse(S * 0.5f, S * 0.5f, 24f, 24f, Palette.CoinEdge);
            c.Ellipse(S * 0.5f, S * 0.5f, 20f, 20f, Palette.Coin);
            c.Line(S * 0.5f, S * 0.5f - 11f, S * 0.5f, S * 0.5f + 11f, 4f, Palette.CoinEdge);
            c.Line(S * 0.5f - 7f, S * 0.5f + 6f, S * 0.5f + 7f, S * 0.5f + 6f, 4f, Palette.CoinEdge);
            c.Line(S * 0.5f - 7f, S * 0.5f - 6f, S * 0.5f + 7f, S * 0.5f - 6f, 4f, Palette.CoinEdge);
            return c.ToSprite(PixelsPerUnit);
        });

        public static Sprite FuelCan => Get("fuel", () =>
        {
            const int W = 52, H = 64;
            var c = new Canvas2D(W, H);
            c.Clear(new Color(0, 0, 0, 0));
            float cx = W * 0.5f;
            c.RoundRect(cx, H * 0.45f, 19f, 24f, 5f, Palette.Ink);
            c.RoundRect(cx, H * 0.45f, 17f, 22f, 4f, Palette.Fuel);
            c.RoundRect(cx - 3f, H * 0.86f, 9f, 5f, 2f, Palette.Ink);        // cap
            c.Line(cx + 8f, H * 0.80f, cx + 15f, H * 0.62f, 5f, Palette.Ink); // handle
            c.Rect(cx - 12f, H * 0.42f, cx + 12f, H * 0.42f + 5f, Palette.BarrierWhite);
            return c.ToSprite(PixelsPerUnit);
        });

        public static Sprite StopSign => Get("stop", () =>
        {
            const int W = 76, H = 96;
            var c = new Canvas2D(W, H);
            c.Clear(new Color(0, 0, 0, 0));
            float cx = W * 0.5f;
            c.RoundRect(cx, H * 0.28f, 3f, 26f, 1f, new Color32(120, 120, 126, 255)); // pole
            c.RoundRect(cx, H * 0.72f, 30f, 20f, 5f, Palette.Ink);
            c.RoundRect(cx, H * 0.72f, 28f, 18f, 4f, Palette.StopSign);
            c.Rect(cx - 20f, H * 0.72f + 3f, cx + 20f, H * 0.72f + 8f, Palette.Paper);
            c.Rect(cx - 20f, H * 0.72f - 8f, cx + 8f, H * 0.72f - 3f, Palette.Paper);
            return c.ToSprite(PixelsPerUnit);
        });

        public static Sprite LaneDash => Get("dash", () =>
        {
            var c = new Canvas2D(12, 56);
            c.Clear(new Color(0, 0, 0, 0));
            c.RoundRect(6f, 28f, 5f, 26f, 3f, Palette.LaneLine);
            return c.ToSprite(PixelsPerUnit);
        });

        public static Sprite RoadsideBush => Get("bush", () =>
        {
            const int S = 72;
            var c = new Canvas2D(S, S);
            c.Clear(new Color(0, 0, 0, 0));
            c.Ellipse(S * 0.42f, S * 0.42f, 20f, 16f, Palette.Grass);
            c.Ellipse(S * 0.62f, S * 0.52f, 16f, 13f, new Color32(108, 132, 70, 255));
            c.Ellipse(S * 0.50f, S * 0.62f, 13f, 11f, new Color32(126, 148, 84, 255));
            return c.ToSprite(PixelsPerUnit);
        });

        /// <summary>Rounded panel used for HUD chips and dialogs.</summary>
        public static Sprite Panel => Get("panel", () =>
        {
            const int S = 64;
            var c = new Canvas2D(S, S);
            c.Clear(new Color(0, 0, 0, 0));
            c.RoundRect(S * 0.5f, S * 0.5f, 30f, 30f, 12f, Color.white);
            Texture2D tex = c.ToTexture();
            return Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), PixelsPerUnit,
                0, SpriteMeshType.FullRect, new Vector4(14, 14, 14, 14));
        });

        public static Sprite Heart => Get("heart", () =>
        {
            const int S = 48;
            var c = new Canvas2D(S, S);
            c.Clear(new Color(0, 0, 0, 0));
            // a wrench-ish blob: this is hull condition, not lives
            c.RoundRect(S * 0.5f, S * 0.5f, 16f, 16f, 6f, Color.white);
            c.Ellipse(S * 0.5f, S * 0.66f, 7f, 7f, new Color(0, 0, 0, 0));
            return c.ToSprite(PixelsPerUnit);
        });
    }
}
