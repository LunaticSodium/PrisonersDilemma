using System;
using System.Collections.Generic;
using System.Linq;
using PrisonersDilemma.Interfaces;

namespace PrisonersDilemma.Simulation
{
    /// <summary>
    /// Represents the complete result of a population simulation run.
    /// </summary>
    public class SimulationResult
    {
        /// <summary>Gets the random seed used for this simulation run.</summary>
        public int Seed { get; init; }

        /// <summary>
        /// Gets the abundance history across all generations.
        /// Each entry is a dictionary mapping strategy name to its fraction of the total population
        /// at that generation (0-indexed).
        /// </summary>
        public IReadOnlyList<Dictionary<string, double>> AbundanceHistory { get; init; }
            = Array.Empty<Dictionary<string, double>>();

        /// <summary>Gets the abundance fractions for the final generation.</summary>
        public Dictionary<string, double> FinalAbundances { get; init; }
            = new Dictionary<string, double>();

        /// <summary>Gets the raw agent counts (as doubles) for the final generation.</summary>
        public Dictionary<string, double> FinalCounts { get; init; }
            = new Dictionary<string, double>();
    }

    /// <summary>
    /// Orchestrates a multi-generation population evolution simulation using a specified
    /// evolution rule and scorer.
    /// </summary>
    /// <remarks>
    /// At each generation a strategy-level weighted round-robin tournament is run.
    /// Each strategy pair's contribution to fitness is the per-round payoff multiplied by
    /// the product of their agent counts, giving a strategy-level total score.
    /// Per-agent fitness is then <c>totalScore / count</c>.
    /// </remarks>
    public class PopulationSimulation
    {
        private readonly IReadOnlyList<IStrategy> _strategies;
        private readonly IEvolutionRule _rule;
        private readonly IScorer _scorer;
        private readonly int _n;
        private readonly int _generations;
        private readonly int _rounds;

        /// <summary>
        /// Initialises a new <see cref="PopulationSimulation"/>.
        /// </summary>
        /// <param name="strategies">The set of strategies competing in the simulation.</param>
        /// <param name="rule">The evolution rule used to produce each new generation.</param>
        /// <param name="scorer">The scorer used to evaluate round outcomes.</param>
        /// <param name="n">Total population size. Defaults to 200.</param>
        /// <param name="generations">Number of generations to simulate. Defaults to 100.</param>
        /// <param name="rounds">Rounds per game in each generation's tournament. Defaults to 200.</param>
        /// <exception cref="ArgumentNullException">Thrown when any required argument is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="n"/>, <paramref name="generations"/>, or
        /// <paramref name="rounds"/> is less than 1.
        /// </exception>
        public PopulationSimulation(
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
        /// Runs the population simulation with the specified random seed.
        /// </summary>
        /// <param name="seed">The seed for the random number generator.</param>
        /// <returns>A <see cref="SimulationResult"/> containing the full history of the run.</returns>
        public SimulationResult Run(int seed)
        {
            var rng = new Random(seed);
            int k = _strategies.Count;

            // Initialise counts: distribute N agents equally; remainder goes to the first strategies.
            int[] counts = new int[k];
            int baseCount = _n / k;
            int remainder = _n % k;
            for (int i = 0; i < k; i++)
                counts[i] = baseCount + (i < remainder ? 1 : 0);

            var strategyNames = _strategies.Select(s => s.Name).ToList();
            var abundanceHistory = new List<Dictionary<string, double>>(_generations);

            // Pre-compute base score matrix once (average total score per game between two strategies).
            var baseScores = ComputeBaseScores();

            for (int g = 0; g < _generations; g++)
            {
                // Record abundance at the start of each generation.
                abundanceHistory.Add(ComputeAbundances(strategyNames, counts));

                // Compute per-strategy total fitness via weighted round-robin.
                double[] fitnessScores = ComputeFitness(counts, baseScores);

                // Apply evolution rule to get next generation counts.
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

        // -----------------------------------------------------------------------------------------
        // Private helpers
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Computes a symmetric k x k matrix where <c>baseScores[i, j].scoreI</c> is the total
        /// score earned by strategy i over one full game (all rounds) against strategy j,
        /// averaged over both orderings (i as player 1 and i as player 2).
        /// </summary>
        private (double scoreI, double scoreJ)[,] ComputeBaseScores()
        {
            int k = _strategies.Count;
            var result = new (double scoreI, double scoreJ)[k, k];

            for (int i = 0; i < k; i++)
            {
                for (int j = i; j < k; j++)
                {
                    double totalI = 0.0;
                    double totalJ = 0.0;
                    int gamesPlayed = 0;

                    // Game A: strategy i as player 1, strategy j as player 2.
                    var (aI, aJ) = RunGameScores(_strategies[i], _strategies[j]);
                    totalI += aI;
                    totalJ += aJ;
                    gamesPlayed++;

                    if (i != j)
                    {
                        // Game B: strategy j as player 1, strategy i as player 2.
                        var (bJ, bI) = RunGameScores(_strategies[j], _strategies[i]);
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
        /// Plays a single directed game between two strategy instances (cloned internally) and
        /// returns the total scores for player 1 and player 2 across all rounds.
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
                var (s, t) = _scorer.Score(a1, a2);
                total1 += s;
                total2 += t;
                p1History.Add(a1);
                p2History.Add(a2);
            }

            return (total1, total2);
        }

        /// <summary>
        /// Computes per-strategy total fitness using a weighted round-robin.
        /// For each pair (i, j), strategy i's fitness contribution is
        /// <c>baseScores[i, j].scoreI * interactions</c>, where interactions equals
        /// <c>counts[i] * counts[j]</c> for i != j and <c>counts[i] * (counts[i] - 1) / 2</c>
        /// for self-play (i == j).
        /// </summary>
        private double[] ComputeFitness(int[] counts, (double scoreI, double scoreJ)[,] baseScores)
        {
            int k = _strategies.Count;
            double[] fitness = new double[k];

            for (int i = 0; i < k; i++)
            {
                if (counts[i] == 0) continue;

                for (int j = 0; j < k; j++)
                {
                    if (counts[j] == 0) continue;

                    double interactions;
                    if (i == j)
                    {
                        // Number of unique pairs among same-strategy agents.
                        interactions = counts[i] * (counts[i] - 1.0) / 2.0;
                    }
                    else
                    {
                        interactions = (double)counts[i] * counts[j];
                    }

                    fitness[i] += baseScores[i, j].scoreI * interactions;
                }
            }

            return fitness;
        }

        /// <summary>
        /// Builds an abundance dictionary mapping strategy name to its fraction of the population.
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
