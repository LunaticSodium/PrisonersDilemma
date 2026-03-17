using System.Collections.Generic;

namespace PrisonersDilemma.Interfaces
{
    /// <summary>
    /// Generates figures by calling a Python subprocess with matplotlib.
    /// </summary>
    public interface IPlotter
    {
        /// <summary>
        /// Plot strategy abundance over generations.
        /// </summary>
        /// <param name="abundanceHistory">List of per-generation dictionaries mapping strategy name to abundance fraction.</param>
        /// <param name="evolutionRuleName">Name of the evolution rule used.</param>
        /// <param name="numRuns">Number of ensemble runs (M).</param>
        /// <param name="outputPath">Output PNG file path.</param>
        void PlotAbundanceOverTime(
            IReadOnlyList<IReadOnlyDictionary<string, double>> abundanceHistory,
            string evolutionRuleName,
            int numRuns,
            string outputPath);

        /// <summary>Plot a pairwise score heatmap.</summary>
        /// <param name="strategyNames">Names of strategies (row/col headers).</param>
        /// <param name="scores">2D score matrix [i,j] = score of strategy i vs j.</param>
        /// <param name="outputPath">Output PNG file path.</param>
        void PlotPairwiseHeatmap(
            IReadOnlyList<string> strategyNames,
            double[,] scores,
            string outputPath);

        /// <summary>Plot final distribution with 95% CI error bars.</summary>
        /// <param name="strategyNames">Names of strategies.</param>
        /// <param name="means">Mean final abundance per strategy.</param>
        /// <param name="ciLow">Lower bound of 95% CI per strategy.</param>
        /// <param name="ciHigh">Upper bound of 95% CI per strategy.</param>
        /// <param name="evolutionRuleName">Name of the evolution rule.</param>
        /// <param name="outputPath">Output PNG file path.</param>
        void PlotFinalDistribution(
            IReadOnlyList<string> strategyNames,
            IReadOnlyList<double> means,
            IReadOnlyList<double> ciLow,
            IReadOnlyList<double> ciHigh,
            string evolutionRuleName,
            string outputPath);

        /// <summary>Plot sensitivity results for a strategy.</summary>
        /// <param name="strategyName">Name of the strategy.</param>
        /// <param name="initialAbundances">Initial abundance levels tested.</param>
        /// <param name="survivalRates">Survival rate at generation G/2 for each initial abundance.</param>
        /// <param name="outputPath">Output PNG file path.</param>
        void PlotSensitivity(
            string strategyName,
            IReadOnlyList<double> initialAbundances,
            IReadOnlyList<double> survivalRates,
            string outputPath);
    }
}
