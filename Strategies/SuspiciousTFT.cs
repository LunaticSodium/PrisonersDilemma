using System;
using System.Collections.Generic;
using PrisonersDilemma.Interfaces;

namespace PrisonersDilemma.Strategies
{
    /// <summary>
    /// Suspicious Tit for Tat. Identical to standard Tit for Tat except it opens
    /// with a defection on the first move, signalling initial distrust. After the
    /// first round it copies the opponent's most recent action. This can destabilise
    /// cooperation when paired against cooperative strategies.
    /// </summary>
    public class SuspiciousTFT : IStrategy
    {
        /// <summary>
        /// Gets the name of this strategy.
        /// </summary>
        public string Name => "Suspicious Tit for Tat";

        /// <summary>
        /// Defects on round 0; thereafter copies the opponent's last action.
        /// </summary>
        /// <param name="myHistory">The history of this strategy's own actions.</param>
        /// <param name="opponentHistory">The history of the opponent's actions.</param>
        /// <returns>
        /// <see cref="Action.Defect"/> on the first round, or the opponent's last
        /// action on subsequent rounds.
        /// </returns>
        public Action GetAction(IReadOnlyList<Action> myHistory, IReadOnlyList<Action> opponentHistory)
        {
            if (opponentHistory.Count == 0)
                return Action.Defect;

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
        /// Creates a new instance of <see cref="SuspiciousTFT"/>.
        /// </summary>
        /// <returns>A new <see cref="SuspiciousTFT"/> instance.</returns>
        public IStrategy Clone()
        {
            return new SuspiciousTFT();
        }
    }
}
