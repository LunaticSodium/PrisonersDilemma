using System;
using System.Collections.Generic;
using PrisonersDilemma.Interfaces;

namespace PrisonersDilemma.Strategies
{
    /// <summary>
    /// The classic Tit for Tat strategy. Cooperates on the first move, then mirrors
    /// the opponent's most recent action on every subsequent move. Known for its
    /// simplicity, niceness, provocability, and forgiveness. Won Axelrod's tournaments.
    /// </summary>
    public class TitForTat : IStrategy
    {
        /// <summary>
        /// Gets the name of this strategy.
        /// </summary>
        public string Name => "Tit for Tat";

        /// <summary>
        /// Cooperates on round 0; thereafter copies the opponent's last action.
        /// </summary>
        /// <param name="myHistory">The history of this strategy's own actions.</param>
        /// <param name="opponentHistory">The history of the opponent's actions.</param>
        /// <returns>
        /// <see cref="Action.Cooperate"/> on the first round, or the opponent's last
        /// action on subsequent rounds.
        /// </returns>
        public Action GetAction(IReadOnlyList<Action> myHistory, IReadOnlyList<Action> opponentHistory)
        {
            if (opponentHistory.Count == 0)
                return Action.Cooperate;

            return opponentHistory[opponentHistory.Count - 1];
        }

        /// <summary>
        /// Resets the strategy state. No-op for this strategy as it holds no mutable state.
        /// </summary>
        public void Reset()
        {
            // No state to reset.
        }

        /// <summary>
        /// Creates a new instance of <see cref="TitForTat"/>.
        /// </summary>
        /// <returns>A new <see cref="TitForTat"/> instance.</returns>
        public IStrategy Clone()
        {
            return new TitForTat();
        }
    }
}
