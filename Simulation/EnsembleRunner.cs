using System;
using System.Collections.Generic;
using System.Linq;
using PrisonersDilemma.Interfaces;

namespace PrisonersDilemma.Simulation
{
    /// <summary>
    /// Holds the aggregated statistics from an ensemble of population simulation runs.
    /// </summary>
    public class EnsembleResult
    {
        /// <summary>
        /// Gets the per-generation mean abundance of each strategy, averaged across all runs.
        /// Each element is a dictionary mapping strategy name to mean abundance fraction.
        /// </summary>
        public IReadOnlyList<Dictionary<string, double>> MeanAbundance { get; init; }
            = Array.Empty<Dictionary<string, double>>();

        /// <summary>
        /// Gets the per-generation lower bound of the 95% confidence interval for each strategy's
        /// abundance. Computed as <c>mean - 1.96 * std / sqrt(numSeeds)</c>.
        /// </summary>
        public IReadOnlyList<Dictionary<string, double>> CiLow { get; init; }
            = Array.Empty<Dictionary<string, double>>();

        /// <summary>
        /// Gets the per-generation upper bound of the 95% confidence interval for each strategy's
        /// abundance. Computed as <c>mean + 1.96 * std / sqrt(numSeeds)</c>.
        /// </summary>
        public IReadOnlyList<Dictionary<string, double>> CiHigh { get; init; }
            = Array.Empty<Dictionary<string, double>>();

        /// <summary>
        /// Gets the full list of individual simulation results, one per seed.
        /// </summary>
        public IReadOnlyList<SimulationResult> AllRuns { get; init; }
            = Array.Empty<SimulationResult>();
    }

    /// <summary>
    /// Runs multiple independent seeds of <see cref="PopulationSimulation"/> and aggregates the
    /// results into per-generation mean abundances and 95% confidence intervals.
    /// </summary>
    public class EnsembleRunner
    {
        private readonly IReadOnlyList<IStrategy> _strategies;
        private readonly IEvolutionRule _rule;
        private readonly IScorer _scorer;
        private readonly int _n;
        private readonly int _generations;
        private readonly int _rounds;

        /// <summary>
        /// Initialises a new <see cref="EnsembleRunner"/>.
        /// </summary>
        /// <param name="strategies">The strategies to include in each simulation.</param>
        /// <param name="rule">The evolution rule applied each generation.</param>
        /// <param name="scorer">The scorer used to evaluate round outcomes.</param>
        /// <param name="n">Total population size per simulation. Defaults to 200.</param>
        /// <param name="generations">Number of generations per simulation. Defaults to 100.</param>
        /// <param name="rounds">Rounds per game per generation. Defaults to 200.</param>
        /// <exception cref="ArgumentNullException">Thrown when any required argument is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="n"/>, <paramref name="generations"/>, or
        /// <paramref name="rounds"/> is less than 1.
        /// </exception>
        public EnsembleRunner(
            IReadOnlyList<IStrategy> strategies,
            IEvolutionRule rule,
            IScorer scorer,
            int n = 200,
            int generations = 100,
            int rounds = 200)
        {
            _strategies  = strategies  ?? throw new ArgumentNullException(nameof(strategies));
            _rule        = rule        ?? throw new ArgumentNullException(nameof(rule));
            _scorer      = scorer      ?? throw new ArgumentNullException(nameof(scorer));

            if (n < 1)           throw new ArgumentOutOfRangeException(nameof(n),           "Population size must be at least 1.");
            if (generations < 1) throw new ArgumentOutOfRangeException(nameof(generations), "Generations must be at least 1.");
            if (rounds < 1)      throw new ArgumentOutOfRangeException(nameof(rounds),      "Rounds must be at least 1.");

            _n           = n;
            _generations = generations;
            _rounds      = rounds;
        }

        /// <summary>
        /// Runs the ensemble of simulations and returns aggregated statistics.
        /// </summary>
        /// <param name="numSeeds">
        /// The number of independent seeds to run (seeds 0 through <paramref name="numSeeds"/> - 1).
        /// </param>
        /// <returns>
        /// An <see cref="EnsembleResult"/> containing per-generation mean abundance, 95% CI bounds,
        /// and all individual simulation results.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="numSeeds"/> is less than 1.
        /// </exception>
        public EnsembleResult RunEnsemble(int numSeeds)
        {
            if (numSeeds < 1)
                throw new ArgumentOutOfRangeException(nameof(numSeeds), "At least one seed is required.");

            var allRuns = new List<SimulationResult>(numSeeds);
            var sim = new PopulationSimulation(_strategies, _rule, _scorer, _n, _generations, _rounds);

            for (int seed = 0; seed < numSeeds; seed++)
                allRuns.Add(sim.Run(seed));

            var strategyNames = _strategies.Select(s => s.Name).ToList();
            int genCount = _generations; // AbundanceHistory has _generations entries

            var meanAbundance = new List<Dictionary<string, double>>(genCount);
            var ciLow         = new List<Dictionary<string, double>>(genCount);
            var ciHigh        = new List<Dictionary<string, double>>(genCount);

            double sqrtN = Math.Sqrt(numSeeds);

            for (int g = 0; g < genCount; g++)
            {
                var meanDict = new Dictionary<string, double>(strategyNames.Count);
                var lowDict  = new Dictionary<string, double>(strategyNames.Count);
                var highDict = new Dictionary<string, double>(strategyNames.Count);

                foreach (var name in strategyNames)
                {
                    // Collect this strategy's abundance at generation g across all runs.
                    double[] values = allRuns
                        .Select(r => r.AbundanceHistory[g].TryGetValue(name, out double v) ? v : 0.0)
                        .ToArray();

                    double mean = values.Average();
                    double std  = ComputeStdDev(values, mean);
                    double margin = 1.96 * std / sqrtN;

                    meanDict[name] = mean;
                    lowDict[name]  = mean - margin;
                    highDict[name] = mean + margin;
                }

                meanAbundance.Add(meanDict);
                ciLow.Add(lowDict);
                ciHigh.Add(highDict);
            }

            return new EnsembleResult
            {
                MeanAbundance = meanAbundance,
                CiLow         = ciLow,
                CiHigh        = ciHigh,
                AllRuns       = allRuns
            };
        }

        // -----------------------------------------------------------------------------------------
        // Private helpers
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Computes the sample standard deviation of an array of values given the pre-computed mean.
        /// Returns 0 when there is fewer than 2 data points.
        /// </summary>
        private static double ComputeStdDev(double[] values, double mean)
        {
            if (values.Length < 2) return 0.0;

            double sumSq = values.Sum(v => (v - mean) * (v - mean));
            return Math.Sqrt(sumSq / (values.Length - 1));
        }
    }
}
