using System;
using System.Collections.Generic;
using PrisonersDilemma.Interfaces;

namespace PrisonersDilemma.Strategies
{
    /// <summary>
    /// A strategy that always cooperates regardless of the opponent's history.
    /// This is the most naive cooperative strategy and is easily exploited by defectors,
    /// but performs well in highly cooperative environments.
    /// </summary>
    public class AlwaysCooperate : IStrategy
    {
        /// <summary>
        /// Gets the name of this strategy.
        /// </summary>
        public string Name => "Always Cooperate";

        /// <summary>
        /// Always returns <see cref="Action.Cooperate"/> regardless of history.
        /// </summary>
        /// <param name="myHistory">The history of this strategy's own actions (ignored).</param>
        /// <param name="opponentHistory">The history of the opponent's actions (ignored).</param>
        /// <returns>Always <see cref="Action.Cooperate"/>.</returns>
        public Action GetAction(IReadOnlyList<Action> myHistory, IReadOnlyList<Action> opponentHistory)
        {
            return Action.Cooperate;
        }

        /// <summary>
        /// Resets the strategy state. No-op for this strategy as it has no state.
        /// </summary>
        public void Reset()
        {
            // No state to reset.
        }

        /// <summary>
        /// Creates a new instance of <see cref="AlwaysCooperate"/>.
        /// </summary>
        /// <returns>A new <see cref="AlwaysCooperate"/> instance.</returns>
        public IStrategy Clone()
        {
            return new AlwaysCooperate();
        }
    }
}
