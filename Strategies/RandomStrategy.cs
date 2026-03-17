using System;
using System.Collections.Generic;
using PrisonersDilemma.Interfaces;

namespace PrisonersDilemma.Strategies
{
    /// <summary>
    /// A strategy that chooses cooperate or defect with equal 50% probability on
    /// every move, independent of history. The constructor uses an unseeded
    /// <see cref="Random"/> for true randomness. Cloned instances use a seed derived
    /// from the strategy's <see cref="Name"/> hash code for reproducibility.
    /// </summary>
    public class RandomStrategy : IStrategy
    {
        private Random _rng;

        /// <summary>
        /// Initialises a new instance of <see cref="RandomStrategy"/> with a
        /// non-deterministic random number generator.
        /// </summary>
        public RandomStrategy()
        {
            _rng = new Random();
        }

        /// <summary>
        /// Private constructor used by <see cref="Clone"/> to supply a specific seed.
        /// </summary>
        /// <param name="seed">The seed for the random number generator.</param>
        private RandomStrategy(int seed)
        {
            _rng = new Random(seed);
        }

        /// <summary>
        /// Gets the name of this strategy.
        /// </summary>
        public string Name => "Random";

        /// <summary>
        /// Returns <see cref="Action.Cooperate"/> or <see cref="Action.Defect"/> each
        /// with 50% probability, regardless of history.
        /// </summary>
        /// <param name="myHistory">The history of this strategy's own actions (ignored).</param>
        /// <param name="opponentHistory">The history of the opponent's actions (ignored).</param>
        /// <returns>A randomly chosen <see cref="Action"/>.</returns>
        public Action GetAction(IReadOnlyList<Action> myHistory, IReadOnlyList<Action> opponentHistory)
        {
            return _rng.NextDouble() < 0.5 ? Action.Cooperate : Action.Defect;
        }

        /// <summary>
        /// Resets the strategy, replacing the RNG with a fresh non-deterministic instance.
        /// </summary>
        public void Reset()
        {
            _rng = new Random();
        }

        /// <summary>
        /// Creates a new <see cref="RandomStrategy"/> seeded with the hash of <see cref="Name"/>
        /// for deterministic behaviour in cloned instances.
        /// </summary>
        /// <returns>A seeded <see cref="RandomStrategy"/> instance.</returns>
        public IStrategy Clone()
        {
            return new RandomStrategy(Name.GetHashCode());
        }
    }
}
