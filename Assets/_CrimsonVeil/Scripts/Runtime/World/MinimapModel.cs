using System.Collections.Generic;
using UnityEngine;

namespace CrimsonVeil.World
{
    /// <summary>
    /// The level as the minimap sees it, engine independent: a grid over the level's extent with the solid ground
    /// marked, and what the hero has explored (cells within <see cref="RevealRadius"/> of where he has been). With
    /// <see cref="RevealAll"/> the whole level shows; without it only the explored part does - the rest is fog.
    /// </summary>
    public sealed class MinimapModel
    {
        /// <summary>Metres per cell.</summary>
        public readonly float CellSize;
        public readonly Rect Bounds;
        public readonly int Width, Height;
        public bool RevealAll = true;
        public float RevealRadius = 9f;

        readonly bool[] solid;
        readonly bool[] explored;

        /// <summary>Set when what the map shows changed (newly explored cells): the picture needs redrawing.</summary>
        public bool Dirty { get; set; } = true;

        public MinimapModel(IEnumerable<Rect> blocks, float cellSize = 0.25f, float margin = 4f, float maxDepthBelowGround = 4f)
        {
            CellSize = Mathf.Max(0.05f, cellSize);
            float xMin = float.MaxValue, xMax = float.MinValue, topMin = float.MaxValue, yMax = float.MinValue;
            var list = new List<Rect>(blocks);
            foreach (var r in list)
            {
                xMin = Mathf.Min(xMin, r.xMin); xMax = Mathf.Max(xMax, r.xMax);
                topMin = Mathf.Min(topMin, r.yMax); yMax = Mathf.Max(yMax, r.yMax);
            }
            if (list.Count == 0) { xMin = 0f; xMax = 10f; topMin = 0f; yMax = 10f; }
            // the deep insides of the ground are of no interest: the map goes a little below the lowest floor
            Bounds = Rect.MinMaxRect(xMin - margin, topMin - maxDepthBelowGround, xMax + margin, yMax + margin);
            Width = Mathf.Max(1, Mathf.CeilToInt(Bounds.width / CellSize));
            Height = Mathf.Max(1, Mathf.CeilToInt(Bounds.height / CellSize));
            solid = new bool[Width * Height];
            explored = new bool[Width * Height];
            foreach (var r in list) Fill(r);
        }

        void Fill(Rect r)
        {
            int x0 = Mathf.Max(0, Mathf.FloorToInt((r.xMin - Bounds.xMin) / CellSize));
            int x1 = Mathf.Min(Width - 1, Mathf.CeilToInt((r.xMax - Bounds.xMin) / CellSize) - 1);
            int y0 = Mathf.Max(0, Mathf.FloorToInt((r.yMin - Bounds.yMin) / CellSize));
            int y1 = Mathf.Min(Height - 1, Mathf.CeilToInt((r.yMax - Bounds.yMin) / CellSize) - 1);
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                    solid[y * Width + x] = true;
        }

        public bool InGrid(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;
        public bool IsSolid(int x, int y) => InGrid(x, y) && solid[y * Width + x];
        public bool IsExplored(int x, int y) => InGrid(x, y) && explored[y * Width + x];
        /// <summary>Does the map show this cell (revealed, or everything is)?</summary>
        public bool IsVisible(int x, int y) => InGrid(x, y) && (RevealAll || explored[y * Width + x]);

        public Vector2Int WorldToCell(Vector2 p) =>
            new Vector2Int(Mathf.FloorToInt((p.x - Bounds.xMin) / CellSize), Mathf.FloorToInt((p.y - Bounds.yMin) / CellSize));

        /// <summary>A world point as 0..1 across the map (for placing markers on the picture).</summary>
        public Vector2 WorldToUv(Vector2 p) => new Vector2((p.x - Bounds.xMin) / Bounds.width, (p.y - Bounds.yMin) / Bounds.height);

        /// <summary>The hero has been at <paramref name="p"/>: everything within the reveal radius is explored.</summary>
        public void Reveal(Vector2 p)
        {
            var c = WorldToCell(p);
            int r = Mathf.CeilToInt(RevealRadius / CellSize);
            float r2 = (RevealRadius / CellSize) * (RevealRadius / CellSize);
            for (int y = Mathf.Max(0, c.y - r); y <= Mathf.Min(Height - 1, c.y + r); y++)
                for (int x = Mathf.Max(0, c.x - r); x <= Mathf.Min(Width - 1, c.x + r); x++)
                {
                    if ((x - c.x) * (x - c.x) + (y - c.y) * (y - c.y) > r2) continue;
                    int i = y * Width + x;
                    if (explored[i]) continue;
                    explored[i] = true;
                    if (!RevealAll) Dirty = true;
                }
        }

        /// <summary>The share of the map explored (0..1).</summary>
        public float ExploredFraction()
        {
            int n = 0;
            foreach (var e in explored) if (e) n++;
            return n / (float)explored.Length;
        }
    }
}
