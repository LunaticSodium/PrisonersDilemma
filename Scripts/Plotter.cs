using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using PrisonersDilemma.Interfaces;

namespace PrisonersDilemma.Scripts
{
    /// <summary>
    /// Generates matplotlib figures via a Python subprocess.
    /// Writes a temporary Python script to disk, executes it with PYTHONPATH pointing at
    /// /workspace/python-packages, then verifies the exit code.
    /// </summary>
    public class Plotter : IPlotter
    {
        private readonly string _pythonPath;
        private readonly string _pythonPackagesPath;

        // Canonical color palette keyed by strategy display name
        private static readonly Dictionary<string, string> StrategyColors = new()
        {
            ["AlwaysCooperate"]  = "#2196F3",
            ["AlwaysDefect"]     = "#F44336",
            ["TitForTat"]        = "#4CAF50",
            ["TitForTwoTats"]    = "#8BC34A",
            ["SuspiciousTFT"]    = "#FF9800",
            ["GenerousTFT"]      = "#009688",
            ["Pavlov"]           = "#9C27B0",
            ["Grudger"]          = "#795548",
            ["RandomStrategy"]   = "#9E9E9E",
            ["GradualTFT"]       = "#3F51B5",
            ["NiceByNature"]     = "#E91E63",
            ["Adaptive"]         = "#CDDC39",
            ["ContriteTFT"]      = "#00BCD4",
            ["ProbingTFT"]       = "#FF5722",
            ["EvolvedStrategy"]  = "#FFD700",
        };

        private const string FallbackColor = "#AAAAAA";

        /// <summary>Create a plotter using the specified Python executable and packages path.</summary>
        public Plotter(string pythonPath = "python3", string pythonPackagesPath = "/workspace/python-packages")
        {
            _pythonPath         = pythonPath;
            _pythonPackagesPath = pythonPackagesPath;
        }

        // ------------------------------------------------------------------ IPlotter

        /// <inheritdoc/>
        public void PlotAbundanceOverTime(
            IReadOnlyList<IReadOnlyDictionary<string, double>> abundanceHistory,
            string evolutionRuleName,
            int numRuns,
            string outputPath)
        {
            // Collect strategy names (preserve insertion order from first generation)
            var names = new List<string>();
            if (abundanceHistory.Count > 0)
                foreach (var kv in abundanceHistory[0])
                    names.Add(kv.Key);

            // Build per-strategy series as JSON arrays
            var seriesDict = new Dictionary<string, List<double>>();
            foreach (var name in names)
                seriesDict[name] = new List<double>(abundanceHistory.Count);

            foreach (var gen in abundanceHistory)
                foreach (var name in names)
                    seriesDict[name].Add(gen.TryGetValue(name, out double v) ? v : 0.0);

            var sb = new StringBuilder();
            sb.AppendLine("import sys");
            sb.AppendLine($"sys.path.insert(0, {JsonString(_pythonPackagesPath)})");
            sb.AppendLine("import matplotlib");
            sb.AppendLine("matplotlib.use('Agg')");
            sb.AppendLine("import matplotlib.pyplot as plt");
            sb.AppendLine("import json");
            sb.AppendLine();
            sb.AppendLine($"names  = {JsonArray(names)}");
            sb.AppendLine($"colors = {JsonColorMap(names)}");
            sb.AppendLine($"series = {JsonSeriesMap(seriesDict)}");
            sb.AppendLine($"title  = {JsonString($"Strategy Abundance Over Time ({evolutionRuleName}, {numRuns} run(s))")}");
            sb.AppendLine($"output = {JsonString(outputPath)}");
            sb.AppendLine();
            sb.AppendLine("fig, ax = plt.subplots(figsize=(12, 7))");
            sb.AppendLine("generations = list(range(len(next(iter(series.values())))))");
            sb.AppendLine("for name in names:");
            sb.AppendLine("    ax.plot(generations, series[name], label=name, color=colors[name], linewidth=1.5)");
            sb.AppendLine("ax.set_xlabel('Generation')");
            sb.AppendLine("ax.set_ylabel('Abundance')");
            sb.AppendLine("ax.set_title(title)");
            sb.AppendLine("ax.legend(loc='upper right', fontsize=8, ncol=2)");
            sb.AppendLine("ax.set_ylim(0, 1)");
            sb.AppendLine("plt.tight_layout()");
            sb.AppendLine("plt.savefig(output, dpi=300)");
            sb.AppendLine("plt.close()");

            RunScript(sb.ToString(), outputPath);
        }

        /// <inheritdoc/>
        public void PlotPairwiseHeatmap(
            IReadOnlyList<string> strategyNames,
            double[,] scores,
            string outputPath)
        {
            int n = strategyNames.Count;
            // Flatten scores row-major
            var flat = new List<double>(n * n);
            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++)
                    flat.Add(scores[i, j]);

            var sb = new StringBuilder();
            sb.AppendLine("import sys");
            sb.AppendLine($"sys.path.insert(0, {JsonString(_pythonPackagesPath)})");
            sb.AppendLine("import matplotlib");
            sb.AppendLine("matplotlib.use('Agg')");
            sb.AppendLine("import matplotlib.pyplot as plt");
            sb.AppendLine("import numpy as np");
            sb.AppendLine();
            sb.AppendLine($"names = {JsonArray(strategyNames)}");
            sb.AppendLine($"flat  = {JsonArray(flat)}");
            sb.AppendLine($"n     = {n}");
            sb.AppendLine($"output = {JsonString(outputPath)}");
            sb.AppendLine();
            sb.AppendLine("data = np.array(flat).reshape(n, n)");
            sb.AppendLine("fig, ax = plt.subplots(figsize=(10, 8))");
            sb.AppendLine("im = ax.imshow(data, aspect='auto', cmap='RdYlGn')");
            sb.AppendLine("plt.colorbar(im, ax=ax, label='Score')");
            sb.AppendLine("ax.set_xticks(range(n))");
            sb.AppendLine("ax.set_yticks(range(n))");
            sb.AppendLine("ax.set_xticklabels(names, rotation=45, ha='right', fontsize=8)");
            sb.AppendLine("ax.set_yticklabels(names, fontsize=8)");
            sb.AppendLine("ax.set_title('Pairwise Score Heatmap')");
            sb.AppendLine("ax.set_xlabel('Opponent')");
            sb.AppendLine("ax.set_ylabel('Strategy')");
            sb.AppendLine("for i in range(n):");
            sb.AppendLine("    for j in range(n):");
            sb.AppendLine("        ax.text(j, i, f'{data[i,j]:.0f}', ha='center', va='center', fontsize=6, color='black')");
            sb.AppendLine("plt.tight_layout()");
            sb.AppendLine("plt.savefig(output, dpi=300)");
            sb.AppendLine("plt.close()");

            RunScript(sb.ToString(), outputPath);
        }

        /// <inheritdoc/>
        public void PlotFinalDistribution(
            IReadOnlyList<string> strategyNames,
            IReadOnlyList<double> means,
            IReadOnlyList<double> ciLow,
            IReadOnlyList<double> ciHigh,
            string evolutionRuleName,
            string outputPath)
        {
            var sb = new StringBuilder();
            sb.AppendLine("import sys");
            sb.AppendLine($"sys.path.insert(0, {JsonString(_pythonPackagesPath)})");
            sb.AppendLine("import matplotlib");
            sb.AppendLine("matplotlib.use('Agg')");
            sb.AppendLine("import matplotlib.pyplot as plt");
            sb.AppendLine("import numpy as np");
            sb.AppendLine();
            sb.AppendLine($"names    = {JsonArray(strategyNames)}");
            sb.AppendLine($"means    = {JsonArray(means)}");
            sb.AppendLine($"ci_low   = {JsonArray(ciLow)}");
            sb.AppendLine($"ci_high  = {JsonArray(ciHigh)}");
            sb.AppendLine($"colors   = {JsonColorMap(strategyNames)}");
            sb.AppendLine($"title    = {JsonString($"Final Strategy Distribution ({evolutionRuleName})")}");
            sb.AppendLine($"output   = {JsonString(outputPath)}");
            sb.AppendLine();
            sb.AppendLine("x = np.arange(len(names))");
            sb.AppendLine("err_low  = [means[i] - ci_low[i]  for i in range(len(names))]");
            sb.AppendLine("err_high = [ci_high[i] - means[i] for i in range(len(names))]");
            sb.AppendLine("bar_colors = [colors[n] for n in names]");
            sb.AppendLine("fig, ax = plt.subplots(figsize=(12, 7))");
            sb.AppendLine("bars = ax.bar(x, means, color=bar_colors, yerr=[err_low, err_high],");
            sb.AppendLine("              capsize=4, error_kw={'elinewidth': 1.2, 'ecolor': '#333333'})");
            sb.AppendLine("ax.set_xticks(x)");
            sb.AppendLine("ax.set_xticklabels(names, rotation=45, ha='right', fontsize=9)");
            sb.AppendLine("ax.set_ylabel('Mean Final Abundance')");
            sb.AppendLine("ax.set_title(title)");
            sb.AppendLine("ax.set_ylim(0, 1)");
            sb.AppendLine("plt.tight_layout()");
            sb.AppendLine("plt.savefig(output, dpi=300)");
            sb.AppendLine("plt.close()");

            RunScript(sb.ToString(), outputPath);
        }

        /// <inheritdoc/>
        public void PlotSensitivity(
            string strategyName,
            IReadOnlyList<double> initialAbundances,
            IReadOnlyList<double> survivalRates,
            string outputPath)
        {
            string color = GetColor(strategyName);

            var sb = new StringBuilder();
            sb.AppendLine("import sys");
            sb.AppendLine($"sys.path.insert(0, {JsonString(_pythonPackagesPath)})");
            sb.AppendLine("import matplotlib");
            sb.AppendLine("matplotlib.use('Agg')");
            sb.AppendLine("import matplotlib.pyplot as plt");
            sb.AppendLine();
            sb.AppendLine($"x      = {JsonArray(initialAbundances)}");
            sb.AppendLine($"y      = {JsonArray(survivalRates)}");
            sb.AppendLine($"color  = {JsonString(color)}");
            sb.AppendLine($"name   = {JsonString(strategyName)}");
            sb.AppendLine($"output = {JsonString(outputPath)}");
            sb.AppendLine();
            sb.AppendLine("fig, ax = plt.subplots(figsize=(10, 6))");
            sb.AppendLine("ax.plot(x, y, marker='o', color=color, linewidth=2, markersize=6)");
            sb.AppendLine("ax.set_xlabel('Initial Abundance')");
            sb.AppendLine("ax.set_ylabel('Survival Rate at G/2')");
            sb.AppendLine("ax.set_title(f'Sensitivity Analysis: {name}')");
            sb.AppendLine("ax.set_xlim(0, 1)");
            sb.AppendLine("ax.set_ylim(0, 1)");
            sb.AppendLine("ax.grid(True, linestyle='--', alpha=0.5)");
            sb.AppendLine("plt.tight_layout()");
            sb.AppendLine("plt.savefig(output, dpi=300)");
            sb.AppendLine("plt.close()");

            RunScript(sb.ToString(), outputPath);
        }

        // ------------------------------------------------------------------ internal helpers

        /// <summary>Write script to a temp file, run Python, validate exit code, and clean up.</summary>
        private void RunScript(string script, string outputPath)
        {
            // Ensure output directory exists
            string? dir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            string tempScript = Path.GetTempFileName() + ".py";
            try
            {
                File.WriteAllText(tempScript, script, Encoding.UTF8);

                var psi = new ProcessStartInfo
                {
                    FileName               = _pythonPath,
                    Arguments              = $"\"{tempScript}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError  = true,
                    UseShellExecute        = false,
                    CreateNoWindow         = true,
                };
                psi.Environment["PYTHONPATH"] = _pythonPackagesPath;

                using var process = Process.Start(psi)
                    ?? throw new InvalidOperationException($"Failed to start Python process at '{_pythonPath}'.");

                string stdout = process.StandardOutput.ReadToEnd();
                string stderr = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    throw new InvalidOperationException(
                        $"Python script exited with code {process.ExitCode}.\n" +
                        $"STDOUT: {stdout}\nSTDERR: {stderr}");
                }
            }
            finally
            {
                if (File.Exists(tempScript))
                    File.Delete(tempScript);
            }
        }

        /// <summary>Return the hex colour for a strategy name, falling back to grey.</summary>
        private static string GetColor(string strategyName)
        {
            // Try exact match first
            if (StrategyColors.TryGetValue(strategyName, out string? color))
                return color;
            // Try stripping spaces (e.g. "Always Cooperate" -> "AlwaysCooperate")
            string noSpaces = strategyName.Replace(" ", "");
            if (StrategyColors.TryGetValue(noSpaces, out string? color2))
                return color2;
            return FallbackColor;
        }

        // ------------------------------------------------------------------ JSON helpers

        private static string JsonString(string value)
            => JsonSerializer.Serialize(value);

        private static string JsonArray(IEnumerable<string> values)
            => "[" + string.Join(", ", System.Linq.Enumerable.Select(values, v => JsonString(v))) + "]";

        private static string JsonArray(IEnumerable<double> values)
            => "[" + string.Join(", ", System.Linq.Enumerable.Select(values, v => v.ToString("R", System.Globalization.CultureInfo.InvariantCulture))) + "]";

        private static string JsonColorMap(IEnumerable<string> names)
        {
            var sb = new StringBuilder("{");
            bool first = true;
            foreach (var name in names)
            {
                if (!first) sb.Append(", ");
                sb.Append($"{JsonString(name)}: {JsonString(GetColor(name))}");
                first = false;
            }
            sb.Append("}");
            return sb.ToString();
        }

        private static string JsonSeriesMap(Dictionary<string, List<double>> series)
        {
            var sb = new StringBuilder("{");
            bool first = true;
            foreach (var kv in series)
            {
                if (!first) sb.Append(", ");
                sb.Append($"{JsonString(kv.Key)}: {JsonArray(kv.Value)}");
                first = false;
            }
            sb.Append("}");
            return sb.ToString();
        }
    }
}
