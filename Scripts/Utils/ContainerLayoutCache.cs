using System;
using System.Collections.Generic;
using Godot;
using Scripts.Config;

namespace Scripts.Utils
{
    public class ContainerLayoutCache
    {
        private struct LayoutKey : IEquatable<LayoutKey>
        {
            public int CardCount;
            public float CurveMultiplier;
            public float RotationMultiplier;
            public float BaselineY;

            public LayoutKey(int cardCount, float curveMultiplier, float rotationMultiplier, float baselineY)
            {
                CardCount = cardCount;
                CurveMultiplier = curveMultiplier;
                RotationMultiplier = rotationMultiplier;
                BaselineY = baselineY;
            }

            public bool Equals(LayoutKey other)
            {
                return CardCount == other.CardCount &&
                       Math.Abs(CurveMultiplier - other.CurveMultiplier) < 0.001f &&
                       Math.Abs(RotationMultiplier - other.RotationMultiplier) < 0.001f &&
                       Math.Abs(BaselineY - other.BaselineY) < 0.001f;
            }

            public override bool Equals(object obj)
            {
                return obj is LayoutKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(CardCount, CurveMultiplier, RotationMultiplier, BaselineY);
            }
        }

        private struct LayoutData
        {
            public Vector2[] Positions;
            public float[] Rotations;

            public LayoutData(Vector2[] positions, float[] rotations)
            {
                Positions = positions;
                Rotations = rotations;
            }
        }

        private readonly Dictionary<LayoutKey, LayoutData> _cache = new();
        private readonly int _maxCacheSize;

        public ContainerLayoutCache(int maxCacheSize = GameConfig.LAYOUT_CACHE_MAX_SIZE)
        {
            _maxCacheSize = maxCacheSize;
        }

        public (Vector2[] positions, float[] rotations) GetLayout(
            int cardCount, 
            float curveMultiplier, 
            float rotationMultiplier, 
            float baselineY)
        {
            if (cardCount <= 0)
                return (Array.Empty<Vector2>(), Array.Empty<float>());

            var key = new LayoutKey(cardCount, curveMultiplier, rotationMultiplier, baselineY);

            if (_cache.TryGetValue(key, out var cachedData))
            {
                return (cachedData.Positions, cachedData.Rotations);
            }

            var positions = new Vector2[cardCount];
            var rotations = new float[cardCount];

            for (int i = 0; i < cardCount; i++)
            {
                float normalizedPos = cardCount > 1 ? (2.0f * i / (cardCount - 1) - 1.0f) : 0;
                float yOffset = Mathf.Pow(normalizedPos, 2) * -curveMultiplier;
                positions[i] = new Vector2(0, baselineY - yOffset);
                rotations[i] = normalizedPos * rotationMultiplier;
            }

            var layoutData = new LayoutData(positions, rotations);
            
            if (_cache.Count >= _maxCacheSize)
            {
                _cache.Clear();
            }
            
            _cache[key] = layoutData;

            return (positions, rotations);
        }

        public void ClearCache()
        {
            _cache.Clear();
        }

        public (int count, int maxSize) GetCacheStats()
        {
            return (_cache.Count, _maxCacheSize);
        }
    }
}