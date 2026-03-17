using System;
using System.Collections.Generic;

namespace PrisonersDilemma.Interfaces
{
    /// <summary>
    /// Defines how a population of strategy agents is updated between generations.
    /// </summary>
    public interface IEvolutionRule
    {
        /// <summary>Display name of this evolution rule.</summary>
        string Name { get; }

        /// <summary>
        /// Produce the next generation given current abundances and fitness scores.
        /// </summary>
        /// <param name="strategyNames">Names of all strategies in the population.</param>
        /// <param name="currentCounts">Current agent count per strategy (same order as names).</param>
        /// <param name="fitnessScores">Total score per strategy this generation (same order as names).</param>
        /// <param name="totalPopulation">Total number of agents N.</param>
        /// <param name="rng">Random number generator for stochastic selection.</param>
        /// <returns>New agent counts per strategy.</returns>
        int[] NextGeneration(
            IReadOnlyList<string> strategyNames,
            IReadOnlyList<int> currentCounts,
            IReadOnlyList<double> fitnessScores,
            int totalPopulation,
            Random rng);
    }
}
