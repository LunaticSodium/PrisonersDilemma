using System;
using System.Collections.Generic;
using System.Linq;
using PrisonersDilemma.Interfaces;

namespace PrisonersDilemma.Simulation
{
    /// <summary>
    /// Represents the outcome of a sensitivity analysis condition for a single strategy
    /// at a specific initial abundance level.
    /// </summary>
    public class SensitivityResult
    {
        /// <summary>Gets the name of the strategy under test.</summary>
        public string StrategyName { get; init; } = string.Empty;

        /// <summary>
        /// Gets the initial abundance fraction used in this condition (e.g. 0.01 or 0.5).
        /// </summary>
        public double InitialAbundance { get; init; }

        /// <summary>
        /// Gets the fraction of seeds in which the strategy survived to the midpoint generation
        /// (generation G/2). Survival is defined as an abundance fraction >= 0.01.
        /// </summary>
        public double SurvivalRate { get; init; }

        /// <summary>
        /// Gets a value indicating whether the strategy survived in the majority of seeds
        /// (<see cref="SurvivalRate"/> > 0.5).
        /// </summary>
        public bool SurvivedMajority => SurvivalRate > 0.5;
    }

    /// <summary>
    /// Tests the evolutionary robustness of each strategy by running simulations with two
    /// different initial abundance levels (0.01 and 0.5) and recording the survival rate
    /// at the midpoint generation.
    /// </summary>
    /// <remarks>
    /// For each strategy S and each initial abundance in {0.01, 0.5}:
    /// <list type="bullet">
    ///   <item>S starts with <c>round(initialAbundance * N)</c> agents.</item>
    ///   <item>The remaining agents are distributed equally among all other strategies.</item>
    ///   <item>
    ///     <c>seedsPerCondition</c> independent simulations are run. Survival is recorded when
    ///     S's abundance fraction at generation <c>floor(generations / 2)</c> is >= 0.01.
    ///   </item>
    /// </list>
    /// </remarks>
    public class SensitivityAnalysis
    {
        private readonly IReadOnlyList<IStrategy> _allStrategies;
        private readonly IEvolutionRule _rule;
        private readonly IScorer _scorer;
        private readonly int _n;
        private readonly int _generations;
        private readonly int _rounds;

        /// <summary>
        /// Initial abundance fractions to test for each strategy.
        /// </summary>
        private static readonly double[] InitialAbundances = { 0.01, 0.5 };

        /// <summary>
        /// Initialises a new <see cref="SensitivityAnalysis"/>.
        /// </summary>
        /// <param name="allStrategies">All strategies to evaluate.</param>
        /// <param name="rule">The evolution rule applied each generation.</param>
        /// <param name="scorer">The scorer used to evaluate round outcomes.</param>
        /// <param name="n">Total population size. Defaults to 200.</param>
        /// <param name="generations">Number of generations per simulation. Defaults to 100.</param>
        /// <param name="rounds">Rounds per game per generation. Defaults to 200.</param>
        /// <exception cref="ArgumentNullException">Thrown when any required argument is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="n"/>, <paramref name="generations"/>, or
        /// <paramref name="rounds"/> is less than 1.
        /// </exception>
        public SensitivityAnalysis(
            IReadOnlyList<IStrategy> allStrategies,
            IEvolutionRule rule,
            IScorer scorer,
            int n = 200,
            int generations = 100,
            int rounds = 200)
        {
            _allStrategies = allStrategies ?? throw new ArgumentNullException(nameof(allStrategies));
            _rule          = rule          ?? throw new ArgumentNullException(nameof(rule));
            _scorer        = scorer        ?? throw new ArgumentNullException(nameof(scorer));

            if (n < 1)           throw new ArgumentOutOfRangeException(nameof(n),           "Population size must be at least 1.");
            if (generations < 1) throw new ArgumentOutOfRangeException(nameof(generations), "Generations must be at least 1.");
            if (rounds < 1)      throw new ArgumentOutOfRangeException(nameof(rounds),      "Rounds must be at least 1.");

            _n           = n;
            _generations = generations;
            _rounds      = rounds;
        }

        /// <summary>
        /// Runs the sensitivity analysis for every strategy at both initial abundance levels.
        /// </summary>
        /// <param name="seedsPerCondition">
        /// The number of independent random seeds to run for each (strategy, initialAbundance)
        /// condition. Defaults to 10.
        /// </param>
        /// <returns>
        /// A list of <see cref="SensitivityResult"/> objects — one per (strategy, initialAbundance)
        /// combination — sorted by strategy name then initial abundance.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="seedsPerCondition"/> is less than 1.
        /// </exception>
        public List<SensitivityResult> Run(int seedsPerCondition = 10)
        {
            if (seedsPerCondition < 1)
                throw new ArgumentOutOfRangeException(nameof(seedsPerCondition), "At least one seed per condition is required.");

            int midpointGeneration = _generations / 2;
            var results = new List<SensitivityResult>();

            foreach (var targetStrategy in _allStrategies)
            {
                foreach (double initialAbundance in InitialAbundances)
                {
                    int survivedCount = 0;

                    for (int seed = 0; seed < seedsPerCondition; seed++)
                    {
                        // Build an initial count vector for this condition.
                        int[] initialCounts = BuildInitialCounts(targetStrategy.Name, initialAbundance);

                        // Run the simulation with custom initial counts.
                        var simResult = RunWithCustomCounts(initialCounts, seed);

                        // Check survival at the midpoint generation.
                        // AbundanceHistory is 0-indexed; index midpointGeneration - 1 is the state
                        // recorded at the start of that generation. Use the last available index
                        // if midpointGeneration exceeds history length.
                        int checkIndex = Math.Min(midpointGeneration, simResult.AbundanceHistory.Count - 1);
                        var abundanceAtMidpoint = simResult.AbundanceHistory[checkIndex];

                        if (abundanceAtMidpoint.TryGetValue(targetStrategy.Name, out double abundance)
                            && abundance >= 0.01)
                        {
                            survivedCount++;
                        }
                    }

                    results.Add(new SensitivityResult
                    {
                        StrategyName     = targetStrategy.Name,
                        InitialAbundance = initialAbundance,
                        SurvivalRate     = (double)survivedCount / seedsPerCondition
                    });
                }
            }

            return results;
        }

        // -----------------------------------------------------------------------------------------
        // Private helpers
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Builds the initial agent count array for a target strategy at the specified abundance.
        /// The target strategy receives <c>round(initialAbundance * N)</c> agents (at least 1).
        /// The remaining agents are distributed equally among all other strategies, with any
        /// remainder going to the first other strategies.
        /// </summary>
        private int[] BuildInitialCounts(string targetStrategyName, double initialAbundance)
        {
            int k = _allStrategies.Count;
            int[] counts = new int[k];

            int targetCount = Math.Max(1, (int)Math.Round(initialAbundance * _n));
            targetCount = Math.Min(targetCount, _n); // safety clamp

            int remaining = _n - targetCount;
            int othersCount = k - 1;

            // Find the index of the target strategy.
            int targetIndex = -1;
            for (int i = 0; i < k; i++)
            {
                if (_allStrategies[i].Name == targetStrategyName)
                {
                    targetIndex = i;
                    break;
                }
            }

            counts[targetIndex] = targetCount;

            if (othersCount > 0 && remaining > 0)
            {
                int baseOther = remaining / othersCount;
                int otherRemainder = remaining % othersCount;
                int othersSeen = 0;

                for (int i = 0; i < k; i++)
                {
                    if (i == targetIndex) continue;
                    counts[i] = baseOther + (othersSeen < otherRemainder ? 1 : 0);
                    othersSeen++;
                }
            }

            return counts;
        }

        /// <summary>
        /// Runs a simulation starting from the specified initial counts and returns the result.
        /// This bypasses <see cref="PopulationSimulation"/>'s built-in uniform initialisation by
        /// using a thin wrapper that injects the provided starting counts.
        /// </summary>
        private SimulationResult RunWithCustomCounts(int[] initialCounts, int seed)
        {
            var rng = new Random(seed);
            int k = _allStrategies.Count;
            var strategyNames = _allStrategies.Select(s => s.Name).ToList();

            // Pre-compute base score matrix.
            var baseScores = ComputeBaseScores();

            int[] counts = (int[])initialCounts.Clone();
            var abundanceHistory = new List<Dictionary<string, double>>(_generations);

            for (int g = 0; g < _generations; g++)
            {
                abundanceHistory.Add(ComputeAbundances(strategyNames, counts));
                double[] fitnessScores = ComputeFitness(counts, baseScores);
                counts = _rule.NextGeneration(strategyNames, counts, fitnessScores, _n, rng);
            }

            var finalAbundances = ComputeAbundances(strategyNames, counts);
            var finalCounts = new Dictionary<string, double>();
            for (int i = 0; i < k; i++)
                finalCounts[strategyNames[i]] = counts[i];

            return new SimulationResult
            {
                Seed             = seed,
                AbundanceHistory = abundanceHistory,
                FinalAbundances  = finalAbundances,
                FinalCounts      = finalCounts
            };
        }

        /// <summary>
        /// Computes the base score matrix (average total score per game between every strategy pair).
        /// </summary>
        private (double scoreI, double scoreJ)[,] ComputeBaseScores()
        {
            int k = _allStrategies.Count;
            var result = new (double scoreI, double scoreJ)[k, k];

            for (int i = 0; i < k; i++)
            {
                for (int j = i; j < k; j++)
                {
                    double totalI = 0.0;
                    double totalJ = 0.0;
                    int gamesPlayed = 0;

                    var (aI, aJ) = RunGameScores(_allStrategies[i], _allStrategies[j]);
                    totalI += aI;
                    totalJ += aJ;
                    gamesPlayed++;

                    if (i != j)
                    {
                        var (bJ, bI) = RunGameScores(_allStrategies[j], _allStrategies[i]);
                        totalI += bI;
                        totalJ += bJ;
                        gamesPlayed++;
                    }

                    double avgI = totalI / gamesPlayed;
                    double avgJ = totalJ / gamesPlayed;

                    result[i, j] = (avgI, avgJ);
                    result[j, i] = (avgJ, avgI);
                }
            }

            return result;
        }

        /// <summary>
        /// Plays a directed game between two strategies and returns their total scores.
        /// </summary>
        private (double p1Score, double p2Score) RunGameScores(IStrategy s1, IStrategy s2)
        {
            var p1 = s1.Clone();
            var p2 = s2.Clone();
            p1.Reset();
            p2.Reset();

            var p1History = new List<Action>(_rounds);
            var p2History = new List<Action>(_rounds);
            double total1 = 0.0;
            double total2 = 0.0;

            for (int r = 0; r < _rounds; r++)
            {
                Action a1 = p1.GetAction(p1History, p2History);
                Action a2 = p2.GetAction(p2History, p1History);
                var (sc1, sc2) = _scorer.Score(a1, a2);
                total1 += sc1;
                total2 += sc2;
                p1History.Add(a1);
                p2History.Add(a2);
            }

            return (total1, total2);
        }

        /// <summary>
        /// Computes weighted round-robin fitness scores for each strategy given current counts.
        /// </summary>
        private double[] ComputeFitness(int[] counts, (double scoreI, double scoreJ)[,] baseScores)
        {
            int k = _allStrategies.Count;
            double[] fitness = new double[k];

            for (int i = 0; i < k; i++)
            {
                if (counts[i] == 0) continue;

                for (int j = 0; j < k; j++)
                {
                    if (counts[j] == 0) continue;

                    double interactions = i == j
                        ? counts[i] * (counts[i] - 1.0) / 2.0
                        : (double)counts[i] * counts[j];

                    fitness[i] += baseScores[i, j].scoreI * interactions;
                }
            }

            return fitness;
        }

        /// <summary>
        /// Builds an abundance dictionary mapping each strategy name to its population fraction.
        /// </summary>
        private static Dictionary<string, double> ComputeAbundances(List<string> names, int[] counts)
        {
            var dict = new Dictionary<string, double>(names.Count);
            double total = counts.Sum();

            for (int i = 0; i < names.Count; i++)
                dict[names[i]] = total > 0 ? counts[i] / total : 0.0;

            return dict;
        }
    }
}
