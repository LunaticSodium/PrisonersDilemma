using System;
using System.Collections.Generic;
using PrisonersDilemma.Interfaces;

namespace PrisonersDilemma.Strategies
{
    /// <summary>
    /// A strategy that always defects regardless of the opponent's history.
    /// This is the dominant strategy in a single-shot Prisoner's Dilemma by classical
    /// game theory, but performs poorly in repeated games against cooperative strategies.
    /// </summary>
    public class AlwaysDefect : IStrategy
    {
        /// <summary>
        /// Gets the name of this strategy.
        /// </summary>
        public string Name => "Always Defect";

        /// <summary>
        /// Always returns <see cref="Action.Defect"/> regardless of history.
        /// </summary>
        /// <param name="myHistory">The history of this strategy's own actions (ignored).</param>
        /// <param name="opponentHistory">The history of the opponent's actions (ignored).</param>
        /// <returns>Always <see cref="Action.Defect"/>.</returns>
        public Action GetAction(IReadOnlyList<Action> myHistory, IReadOnlyList<Action> opponentHistory)
        {
            return Action.Defect;
        }

        /// <summary>
        /// Resets the strategy state. No-op for this strategy as it has no state.
        /// </summary>
        public void Reset()
        {
            // No state to reset.
        }

        /// <summary>
        /// Creates a new instance of <see cref="AlwaysDefect"/>.
        /// </summary>
        /// <returns>A new <see cref="AlwaysDefect"/> instance.</returns>
        public IStrategy Clone()
        {
            return new AlwaysDefect();
        }
    }
}
