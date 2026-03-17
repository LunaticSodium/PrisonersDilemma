using System;
using System.Collections.Generic;
using System.Linq;
using PrisonersDilemma.Interfaces;

namespace PrisonersDilemma.Simulation
{
    /// <summary>
    /// An evolution rule that allocates slots in the next generation proportionally to each
    /// strategy's fitness score (fitness-proportionate / roulette-wheel selection).
    /// </summary>
    /// <remarks>
    /// Each strategy's probability of filling a slot is:
    /// <code>P(i) = fitnessScores[i] / sum(fitnessScores)</code>
    /// If all fitness scores are zero the allocation is uniform.
    /// A cumulative distribution array and binary search are used for O(log k) per slot.
    /// </remarks>
    public class ProportionalSelection : IEvolutionRule
    {
        /// <inheritdoc/>
        public string Name => "ProportionalSelection";

        /// <summary>
        /// Computes the next generation's strategy counts using fitness-proportionate selection.
        /// </summary>
        /// <param name="strategyNames">Ordered list of strategy names.</param>
        /// <param name="currentCounts">Current count of agents for each strategy (same order).</param>
        /// <param name="fitnessScores">Total fitness score for each strategy (same order).</param>
        /// <param name="totalPopulation">Total number of agents in the population (N).</param>
        /// <param name="rng">Random number generator to use for sampling.</param>
        /// <returns>
        /// An integer array of the same length as <paramref name="strategyNames"/> containing
        /// the agent count assigned to each strategy in the next generation.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when any required argument is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when the lengths of <paramref name="strategyNames"/>, <paramref name="currentCounts"/>,
        /// and <paramref name="fitnessScores"/> do not match.
        /// </exception>
        public int[] NextGeneration(
            IReadOnlyList<string> strategyNames,
            IReadOnlyList<int> currentCounts,
            IReadOnlyList<double> fitnessScores,
            int totalPopulation,
            Random rng)
        {
            if (strategyNames == null) throw new ArgumentNullException(nameof(strategyNames));
            if (currentCounts == null) throw new ArgumentNullException(nameof(currentCounts));
            if (fitnessScores == null) throw new ArgumentNullException(nameof(fitnessScores));
            if (rng == null) throw new ArgumentNullException(nameof(rng));

            int k = strategyNames.Count;
            if (currentCounts.Count != k || fitnessScores.Count != k)
                throw new ArgumentException("strategyNames, currentCounts, and fitnessScores must all have the same length.");

            double totalFitness = fitnessScores.Sum();

            // Build cumulative distribution array
            double[] cumulative = new double[k];
            if (totalFitness <= 0.0)
            {
                // Uniform distribution fallback
                double uniform = 1.0 / k;
                for (int i = 0; i < k; i++)
                    cumulative[i] = uniform * (i + 1);
            }
            else
            {
                double running = 0.0;
                for (int i = 0; i < k; i++)
                {
                    running += fitnessScores[i] / totalFitness;
                    cumulative[i] = running;
                }
                // Ensure the last element is exactly 1.0 to avoid floating-point edge cases
                cumulative[k - 1] = 1.0;
            }

            int[] nextCounts = new int[k];

            for (int slot = 0; slot < totalPopulation; slot++)
            {
                double r = rng.NextDouble();
                // Binary search for the first cumulative value >= r
                int lo = 0, hi = k - 1;
                while (lo < hi)
                {
                    int mid = (lo + hi) / 2;
                    if (cumulative[mid] < r)
                        lo = mid + 1;
                    else
                        hi = mid;
                }
                nextCounts[lo]++;
            }

            return nextCounts;
        }
    }
}
