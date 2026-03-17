using System;
using System.Collections.Generic;
using PrisonersDilemma.Interfaces;

namespace PrisonersDilemma.Strategies
{
    /// <summary>
    /// Generous Tit for Tat. Behaves like standard Tit for Tat but when it would
    /// normally retaliate with a defection, it instead cooperates with probability 1/3.
    /// This forgiveness prevents long cycles of mutual retaliation caused by noise
    /// or single accidental defections. Uses a seeded <see cref="Random"/> for
    /// reproducibility, with a fresh <see cref="Random"/> created on each <see cref="Reset"/>.
    /// </summary>
    public class GenerousTFT : IStrategy
    {
        private Random _rng;

        /// <summary>
        /// Initialises a new instance of <see cref="GenerousTFT"/> with a seeded
        /// random number generator (seed 42) for determinism.
        /// </summary>
        public GenerousTFT()
        {
            _rng = new Random(42);
        }

        /// <summary>
        /// Gets the name of this strategy.
        /// </summary>
        public string Name => "Generous Tit for Tat";

        /// <summary>
        /// Cooperates on round 0. Subsequently mirrors the opponent's last action,
        /// except when retaliating: cooperates with probability 1/3 instead of defecting.
        /// </summary>
        /// <param name="myHistory">The history of this strategy's own actions.</param>
        /// <param name="opponentHistory">The history of the opponent's actions.</param>
        /// <returns>
        /// <see cref="Action.Cooperate"/> on round 0 or when the opponent last cooperated.
        /// When the opponent last defected, returns <see cref="Action.Cooperate"/> with
        /// probability 1/3 and <see cref="Action.Defect"/> with probability 2/3.
        /// </returns>
        public Action GetAction(IReadOnlyList<Action> myHistory, IReadOnlyList<Action> opponentHistory)
        {
            if (opponentHistory.Count == 0)
                return Action.Cooperate;

            if (opponentHistory[opponentHistory.Count - 1] == Action.Cooperate)
                return Action.Cooperate;

            // Opponent defected last round: forgive with probability 1/3.
            return (_rng.NextDouble() < 1.0 / 3.0) ? Action.Cooperate : Action.Defect;
        }

        /// <summary>
        /// Resets the strategy state, creating a fresh (unseeded) <see cref="Random"/>.
        /// </summary>
        public void Reset()
        {
            _rng = new Random();
        }

        /// <summary>
        /// Creates a new <see cref="GenerousTFT"/> instance with a fresh seeded RNG.
        /// </summary>
        /// <returns>A new <see cref="GenerousTFT"/> instance.</returns>
        public IStrategy Clone()
        {
            return new GenerousTFT();
        }
    }
}
