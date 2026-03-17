using System;
using System.Collections.Generic;
using PrisonersDilemma.Interfaces;

namespace PrisonersDilemma.Strategies
{
    /// <summary>
    /// The Tit for Two Tats strategy. More forgiving than standard Tit for Tat:
    /// only retaliates (defects) when the opponent has defected in <em>both</em> of the
    /// two most recent rounds. Single defections are tolerated, making this strategy
    /// more robust to noise and accidental defections.
    /// </summary>
    public class TitForTwoTats : IStrategy
    {
        /// <summary>
        /// Gets the name of this strategy.
        /// </summary>
        public string Name => "Tit for Two Tats";

        /// <summary>
        /// Cooperates on rounds 0 and 1. From round 2 onward, defects only if the
        /// opponent defected in both of the two preceding rounds.
        /// </summary>
        /// <param name="myHistory">The history of this strategy's own actions.</param>
        /// <param name="opponentHistory">The history of the opponent's actions.</param>
        /// <returns>
        /// <see cref="Action.Defect"/> if the opponent defected in the last two consecutive
        /// rounds; otherwise <see cref="Action.Cooperate"/>.
        /// </returns>
        public Action GetAction(IReadOnlyList<Action> myHistory, IReadOnlyList<Action> opponentHistory)
        {
            int n = opponentHistory.Count;

            if (n < 2)
                return Action.Cooperate;

            bool lastDefected = opponentHistory[n - 1] == Action.Defect;
            bool secondLastDefected = opponentHistory[n - 2] == Action.Defect;

            return (lastDefected && secondLastDefected) ? Action.Defect : Action.Cooperate;
        }

        /// <summary>
        /// Resets the strategy state. No-op for this strategy as it holds no mutable state.
        /// </summary>
        public void Reset()
        {
            // No state to reset.
        }

        /// <summary>
        /// Creates a new instance of <see cref="TitForTwoTats"/>.
        /// </summary>
        /// <returns>A new <see cref="TitForTwoTats"/> instance.</returns>
        public IStrategy Clone()
        {
            return new TitForTwoTats();
        }
    }
}
