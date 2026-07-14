using System.Collections.Generic;
using UnityEngine;

namespace Chris.PachiRogue.Physics
{
    /// <summary>
    /// Procedural placeholder sprites (filled circle / square) so the graybox
    /// game needs zero texture assets. Replaced by real art per ASSET_GUIDE.md;
    /// results are cached per size.
    /// </summary>
    public static class GrayboxSprites
    {
        private static readonly Dictionary<int, Sprite> CircleCache = new Dictionary<int, Sprite>();
        private static Sprite _square;

        public static Sprite Circle(int diameter = 64)
        {
            if (CircleCache.TryGetValue(diameter, out Sprite cached))
            {
                return cached;
            }

            var texture = new Texture2D(diameter, diameter, TextureFormat.RGBA32, false);
            float radius = diameter * 0.5f;
            float radiusSq = (radius - 1f) * (radius - 1f);
            var pixels = new Color32[diameter * diameter];

            for (int y = 0; y < diameter; y++)
            {
                for (int x = 0; x < diameter; x++)
                {
                    float dx = x - radius + 0.5f;
                    float dy = y - radius + 0.5f;
                    bool inside = dx * dx + dy * dy <= radiusSq;
                    pixels[y * diameter + x] = inside
                        ? new Color32(255, 255, 255, 255)
                        : new Color32(0, 0, 0, 0);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();

            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, diameter, diameter),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit: diameter); // 1 world unit across

            CircleCache[diameter] = sprite;
            return sprite;
        }

        public static Sprite Square()
        {
            if (_square != null)
            {
                return _square;
            }

            var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            var pixels = new Color32[16];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color32(255, 255, 255, 255);
            }

            texture.SetPixels32(pixels);
            texture.Apply();

            _square = Sprite.Create(texture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 4f);
            return _square;
        }
    }
}
