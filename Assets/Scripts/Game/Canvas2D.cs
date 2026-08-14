using UnityEngine;

namespace KombiRush.Game
{
    /// <summary>
    /// A tiny software drawing surface used to bake sprites at runtime. Shapes are drawn with
    /// distance-based coverage so edges come out smooth instead of jagged, which is what makes
    /// generated art look deliberate rather than programmer-art.
    /// </summary>
    public sealed class Canvas2D
    {
        public readonly int Width;
        public readonly int Height;
        private readonly Color[] _pixels;

        public Canvas2D(int width, int height)
        {
            Width = width;
            Height = height;
            _pixels = new Color[width * height];
        }

        public void Clear(Color color)
        {
            for (int i = 0; i < _pixels.Length; i++) _pixels[i] = color;
        }

        public void Blend(int x, int y, Color color, float alpha)
        {
            if (alpha <= 0f || x < 0 || y < 0 || x >= Width || y >= Height) return;
            if (alpha > 1f) alpha = 1f;
            int i = y * Width + x;
            Color dst = _pixels[i];
            float a = color.a * alpha;
            float outA = a + dst.a * (1f - a);
            if (outA <= 0.0001f) { _pixels[i] = new Color(0, 0, 0, 0); return; }
            _pixels[i] = new Color(
                (color.r * a + dst.r * dst.a * (1f - a)) / outA,
                (color.g * a + dst.g * dst.a * (1f - a)) / outA,
                (color.b * a + dst.b * dst.a * (1f - a)) / outA,
                outA);
        }

        public void Rect(float x0, float y0, float x1, float y1, Color color)
        {
            int ix0 = Mathf.FloorToInt(Mathf.Min(x0, x1));
            int ix1 = Mathf.CeilToInt(Mathf.Max(x0, x1));
            int iy0 = Mathf.FloorToInt(Mathf.Min(y0, y1));
            int iy1 = Mathf.CeilToInt(Mathf.Max(y0, y1));
            for (int y = iy0; y <= iy1; y++)
            for (int x = ix0; x <= ix1; x++)
            {
                float cov = Coverage(x, y, px => InsideRect(px.x, px.y, x0, y0, x1, y1));
                Blend(x, y, color, cov);
            }
        }

        /// <summary>Rounded rectangle by centre and half extents.</summary>
        public void RoundRect(float cx, float cy, float halfW, float halfH, float radius, Color color)
        {
            int ix0 = Mathf.FloorToInt(cx - halfW - 1f);
            int ix1 = Mathf.CeilToInt(cx + halfW + 1f);
            int iy0 = Mathf.FloorToInt(cy - halfH - 1f);
            int iy1 = Mathf.CeilToInt(cy + halfH + 1f);
            if (radius > halfW) radius = halfW;
            if (radius > halfH) radius = halfH;

            for (int y = iy0; y <= iy1; y++)
            for (int x = ix0; x <= ix1; x++)
            {
                float d = RoundRectDistance(x + 0.5f - cx, y + 0.5f - cy, halfW, halfH, radius);
                Blend(x, y, color, Mathf.Clamp01(0.5f - d));
            }
        }

        public void Ellipse(float cx, float cy, float radiusX, float radiusY, Color color)
        {
            int ix0 = Mathf.FloorToInt(cx - radiusX - 1f);
            int ix1 = Mathf.CeilToInt(cx + radiusX + 1f);
            int iy0 = Mathf.FloorToInt(cy - radiusY - 1f);
            int iy1 = Mathf.CeilToInt(cy + radiusY + 1f);
            for (int y = iy0; y <= iy1; y++)
            for (int x = ix0; x <= ix1; x++)
            {
                float nx = (x + 0.5f - cx) / Mathf.Max(0.0001f, radiusX);
                float ny = (y + 0.5f - cy) / Mathf.Max(0.0001f, radiusY);
                float d = (Mathf.Sqrt(nx * nx + ny * ny) - 1f) * Mathf.Min(radiusX, radiusY);
                Blend(x, y, color, Mathf.Clamp01(0.5f - d));
            }
        }

        public void Line(float x0, float y0, float x1, float y1, float thickness, Color color)
        {
            float half = thickness * 0.5f;
            int ix0 = Mathf.FloorToInt(Mathf.Min(x0, x1) - half - 1f);
            int ix1 = Mathf.CeilToInt(Mathf.Max(x0, x1) + half + 1f);
            int iy0 = Mathf.FloorToInt(Mathf.Min(y0, y1) - half - 1f);
            int iy1 = Mathf.CeilToInt(Mathf.Max(y0, y1) + half + 1f);
            for (int y = iy0; y <= iy1; y++)
            for (int x = ix0; x <= ix1; x++)
            {
                float d = SegmentDistance(x + 0.5f, y + 0.5f, x0, y0, x1, y1) - half;
                Blend(x, y, color, Mathf.Clamp01(0.5f - d));
            }
        }

        /// <summary>Vertical linear shade, used for cheap "sunlight" on bodywork.</summary>
        public void ShadeVertical(float x0, float y0, float x1, float y1, Color top, Color bottom)
        {
            int ix0 = Mathf.FloorToInt(Mathf.Min(x0, x1));
            int ix1 = Mathf.CeilToInt(Mathf.Max(x0, x1));
            int iy0 = Mathf.FloorToInt(Mathf.Min(y0, y1));
            int iy1 = Mathf.CeilToInt(Mathf.Max(y0, y1));
            for (int y = iy0; y <= iy1; y++)
            {
                float t = Mathf.InverseLerp(iy0, iy1, y);
                Color c = Color.Lerp(bottom, top, t);
                for (int x = ix0; x <= ix1; x++)
                {
                    float cov = Coverage(x, y, px => InsideRect(px.x, px.y, x0, y0, x1, y1));
                    Blend(x, y, c, cov);
                }
            }
        }

        public Texture2D ToTexture(FilterMode filterMode = FilterMode.Bilinear)
        {
            var tex = new Texture2D(Width, Height, TextureFormat.RGBA32, false)
            {
                filterMode = filterMode,
                wrapMode = TextureWrapMode.Clamp
            };
            tex.SetPixels(_pixels);
            tex.Apply(false, true);
            return tex;
        }

        public Sprite ToSprite(float pixelsPerUnit)
        {
            Texture2D tex = ToTexture();
            return Sprite.Create(tex, new Rect(0, 0, Width, Height), new Vector2(0.5f, 0.5f), pixelsPerUnit,
                0, SpriteMeshType.FullRect);
        }

        // --- helpers ----------------------------------------------------------
        private static float Coverage(int x, int y, System.Func<Vector2, bool> inside)
        {
            int hits = 0;
            for (int sy = 0; sy < 2; sy++)
            for (int sx = 0; sx < 2; sx++)
                if (inside(new Vector2(x + 0.25f + sx * 0.5f, y + 0.25f + sy * 0.5f))) hits++;
            return hits * 0.25f;
        }

        private static bool InsideRect(float px, float py, float x0, float y0, float x1, float y1)
        {
            float minX = Mathf.Min(x0, x1), maxX = Mathf.Max(x0, x1);
            float minY = Mathf.Min(y0, y1), maxY = Mathf.Max(y0, y1);
            return px >= minX && px <= maxX && py >= minY && py <= maxY;
        }

        private static float RoundRectDistance(float px, float py, float halfW, float halfH, float radius)
        {
            float qx = Mathf.Abs(px) - (halfW - radius);
            float qy = Mathf.Abs(py) - (halfH - radius);
            float outside = Mathf.Sqrt(Mathf.Max(qx, 0f) * Mathf.Max(qx, 0f) + Mathf.Max(qy, 0f) * Mathf.Max(qy, 0f));
            float inside = Mathf.Min(Mathf.Max(qx, qy), 0f);
            return outside + inside - radius;
        }

        private static float SegmentDistance(float px, float py, float x0, float y0, float x1, float y1)
        {
            float dx = x1 - x0, dy = y1 - y0;
            float lenSq = dx * dx + dy * dy;
            float t = lenSq <= 0.0001f ? 0f : Mathf.Clamp01(((px - x0) * dx + (py - y0) * dy) / lenSq);
            float cx = x0 + t * dx, cy = y0 + t * dy;
            return Mathf.Sqrt((px - cx) * (px - cx) + (py - cy) * (py - cy));
        }
    }
}
