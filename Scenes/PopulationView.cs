// PopulationView.cs — Godot scene script for the live population chart.
// This file is excluded from the standalone dotnet build (see csproj).
// Compiled by Godot when running with the Godot engine.
using System.Collections.Generic;
using Godot;

namespace PrisonersDilemma.Scenes
{
    /// <summary>
    /// Draws a live stacked area chart of strategy abundances over generations.
    /// Updated every 5 generations during a simulation run.
    /// </summary>
    public partial class PopulationView : Node
    {
        /// <summary>
        /// Abundance history: list of per-generation dictionaries (strategy -> fraction).
        /// </summary>
        private readonly List<Dictionary<string, double>> _history = new();

        /// <summary>Strategy colour palette.</summary>
        private static readonly Dictionary<string, Color> StrategyColors = new()
        {
            ["Always Cooperate"]      = new Color("#2196F3"),
            ["Always Defect"]         = new Color("#F44336"),
            ["Tit for Tat"]           = new Color("#4CAF50"),
            ["Tit for Two Tats"]      = new Color("#8BC34A"),
            ["Suspicious Tit for Tat"]= new Color("#FF9800"),
            ["Generous Tit for Tat"]  = new Color("#009688"),
            ["Pavlov"]                = new Color("#9C27B0"),
            ["Grudger"]               = new Color("#795548"),
            ["Random"]                = new Color("#9E9E9E"),
            ["Gradual Tit for Tat"]   = new Color("#3F51B5"),
            ["Nice by Nature"]        = new Color("#E91E63"),
            ["Adaptive"]              = new Color("#CDDC39"),
            ["Contrite Tit for Tat"]  = new Color("#00BCD4"),
            ["Probing Tit for Tat"]   = new Color("#FF5722"),
            ["EvolvedStrategy"]       = new Color("#FFD700"),
        };

        /// <summary>
        /// Update the chart with a new generation's data.
        /// Only redraws every 5 generations for performance.
        /// </summary>
        /// <param name="generation">Current generation index.</param>
        /// <param name="abundances">Strategy name to abundance fraction.</param>
        public void UpdateGeneration(int generation, Dictionary<string, double> abundances)
        {
            _history.Add(new Dictionary<string, double>(abundances));

            if (generation % 5 == 0)
                QueueRedraw();
        }

        /// <summary>Clear the chart for a new simulation run.</summary>
        public void Clear()
        {
            _history.Clear();
            QueueRedraw();
        }

        /// <inheritdoc/>
        public override void _Draw()
        {
            if (_history.Count == 0) return;

            var rect = GetViewportRect();
            float w = rect.Size.X;
            float h = rect.Size.Y;
            float margin = 40f;
            float chartW = w - 2 * margin;
            float chartH = h - 2 * margin;

            // Draw background
            DrawRect(new Rect2(margin, margin, chartW, chartH), new Color(0.1f, 0.1f, 0.1f));

            if (_history.Count < 2) return;

            var strategies = new List<string>(_history[0].Keys);
            int nGens = _history.Count;

            // Stacked area: compute cumulative sums per generation
            for (int si = 0; si < strategies.Count; si++)
            {
                string s = strategies[si];
                var points = new Vector2[nGens * 2];

                for (int g = 0; g < nGens; g++)
                {
                    float x = margin + g * chartW / (nGens - 1);
                    // Compute cumulative bottom
                    float bottom = 0f;
                    for (int k = 0; k < si; k++)
                        bottom += (float)(_history[g].GetValueOrDefault(strategies[k], 0.0));
                    float top = bottom + (float)(_history[g].GetValueOrDefault(s, 0.0));

                    points[g] = new Vector2(x, margin + chartH - top * chartH);
                    points[nGens * 2 - 1 - g] = new Vector2(x, margin + chartH - bottom * chartH);
                }

                Color color = StrategyColors.TryGetValue(s, out var c) ? c : new Color(0.5f, 0.5f, 0.5f);
                DrawPolygon(points, new[] { color });
            }

            // Draw axes
            DrawLine(new Vector2(margin, margin), new Vector2(margin, margin + chartH), Colors.White);
            DrawLine(new Vector2(margin, margin + chartH), new Vector2(margin + chartW, margin + chartH), Colors.White);

            // Legend
            float legendX = margin + chartW + 10;
            float legendY = margin;
            foreach (var s in strategies)
            {
                Color color = StrategyColors.TryGetValue(s, out var c) ? c : new Color(0.5f, 0.5f, 0.5f);
                DrawRect(new Rect2(legendX, legendY, 12, 12), color);
                DrawString(ThemeDB.FallbackFont, new Vector2(legendX + 16, legendY + 10), s.Length > 15 ? s[..15] : s, modulate: Colors.White);
                legendY += 16;
            }
        }
    }
}
