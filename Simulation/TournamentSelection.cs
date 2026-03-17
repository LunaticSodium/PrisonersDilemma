using System;
using System.Collections.Generic;
using System.Linq;
using PrisonersDilemma.Interfaces;

namespace PrisonersDilemma.Simulation
{
    /// <summary>
    /// An evolution rule that uses tournament selection to populate the next generation.
    /// </summary>
    /// <remarks>
    /// For each of the N slots in the new generation, <see cref="TournamentSize"/> agents are
    /// sampled at random (with replacement) from the current population (weighted by counts).
    /// The sampled agent with the highest per-agent fitness wins the slot.
    /// Per-agent fitness is defined as <c>fitnessScore[i] / count[i]</c>.
    /// </remarks>
    public class TournamentSelection : IEvolutionRule
    {
        /// <summary>
        /// Gets the number of contestants drawn for each selection event.
        /// Defaults to 5.
        /// </summary>
        public int TournamentSize { get; init; } = 5;

        /// <inheritdoc/>
        public string Name => "TournamentSelection";

        /// <summary>
        /// Computes the next generation's strategy counts using tournament selection.
        /// </summary>
        /// <param name="strategyNames">Ordered list of strategy names.</param>
        /// <param name="currentCounts">Current count of agents for each strategy (same order).</param>
        /// <param name="fitnessScores">Total fitness score for each strategy (same order).</param>
        /// <param name="totalPopulation">Total number of agents in the next generation (N).</param>
        /// <param name="rng">Random number generator to use for sampling.</param>
        /// <returns>
        /// An integer array of the same length as <paramref name="strategyNames"/> containing
        /// the agent count assigned to each strategy in the next generation.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when any required argument is null.</exception>
        /// <exception cref="ArgumentException">
        /// Thrown when argument list lengths do not match or the current population is empty.
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

            // Step 1: Build a flat list of (strategyIndex, perAgentFitness) expanded by counts.
            // We store the index rather than the name to make counting O(1).
            var flatList = new List<(int strategyIndex, double perAgentFitness)>();
            for (int i = 0; i < k; i++)
            {
                int count = currentCounts[i];
                if (count <= 0) continue;

                double perAgentFitness = fitnessScores[i] / count;
                for (int c = 0; c < count; c++)
                    flatList.Add((i, perAgentFitness));
            }

            if (flatList.Count == 0)
                throw new ArgumentException("The current population is empty (all counts are zero).");

            int flatSize = flatList.Count;
            int tournamentSize = Math.Min(TournamentSize, flatSize);

            int[] nextCounts = new int[k];

            // Step 2: For each new agent, run a tournament.
            for (int slot = 0; slot < totalPopulation; slot++)
            {
                int winnerIndex = -1;
                double winnerFitness = double.NegativeInfinity;

                for (int t = 0; t < tournamentSize; t++)
                {
                    int pick = rng.Next(flatSize);
                    var (stratIdx, fitness) = flatList[pick];
                    if (fitness > winnerFitness)
                    {
                        winnerFitness = fitness;
                        winnerIndex = stratIdx;
                    }
                }

                nextCounts[winnerIndex]++;
            }

            return nextCounts;
        }
    }
}
