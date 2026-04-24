using UnityEngine;

namespace BattleSim.Core
{
    public static class SimpleSpriteFactory
    {
        private static Sprite _whitePixel;

        public static Sprite GetWhitePixel()
        {
            if (_whitePixel != null)
            {
                return _whitePixel;
            }

            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();

            _whitePixel = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
            _whitePixel.name = "WhitePixel_Runtime";
            return _whitePixel;
        }

        public static Sprite CreateCircle(int size = 64)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            float radius = size * 0.5f;
            float radiusSqr = radius * radius;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x, y);
                    float distanceSqr = (p - center).sqrMagnitude;
                    Color color = distanceSqr <= radiusSqr ? Color.white : Color.clear;
                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply();

            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
            sprite.name = "Circle_Runtime";
            return sprite;
        }
    }
}
