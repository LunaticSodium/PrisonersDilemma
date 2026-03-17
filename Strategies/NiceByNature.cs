using System;
using System.Collections.Generic;
using PrisonersDilemma.Interfaces;

namespace PrisonersDilemma.Strategies
{
    /// <summary>
    /// Nice by Nature. Cooperates on round 0. From round 1 onward it uses a sliding
    /// window of the last 10 rounds (or all rounds if fewer than 10 have been played)
    /// to assess the opponent's behaviour. If the opponent defected in more than 60%
    /// of the rounds in that window, it defects; otherwise it cooperates. This gives
    /// opponents a reasonable benefit of the doubt while still responding to
    /// sustained aggression.
    /// </summary>
    public class NiceByNature : IStrategy
    {
        private const int WindowSize = 10;
        private const double DefectThreshold = 0.6;

        /// <summary>
        /// Gets the name of this strategy.
        /// </summary>
        public string Name => "Nice by Nature";

        /// <summary>
        /// Cooperates on round 0. Subsequently defects only when the opponent's defection
        /// rate exceeds 60% within the most recent 10-round sliding window.
        /// </summary>
        /// <param name="myHistory">The history of this strategy's own actions.</param>
        /// <param name="opponentHistory">The history of the opponent's actions.</param>
        /// <returns>
        /// <see cref="Action.Cooperate"/> unless the opponent has defected more than
        /// 60% of the time in the observation window.
        /// </returns>
        public Action GetAction(IReadOnlyList<Action> myHistory, IReadOnlyList<Action> opponentHistory)
        {
            if (opponentHistory.Count == 0)
                return Action.Cooperate;

            int windowStart = Math.Max(0, opponentHistory.Count - WindowSize);
            int windowLength = opponentHistory.Count - windowStart;

            int defectionsInWindow = 0;
            for (int i = windowStart; i < opponentHistory.Count; i++)
            {
                if (opponentHistory[i] == Action.Defect)
                    defectionsInWindow++;
            }

            double defectRate = (double)defectionsInWindow / windowLength;
            return defectRate > DefectThreshold ? Action.Defect : Action.Cooperate;
        }

        /// <summary>
        /// Resets the strategy state. No-op for this strategy as it holds no mutable state.
        /// </summary>
        public void Reset()
        {
            // No state to reset; all decisions are computed from the history lists.
        }

        /// <summary>
        /// Creates a new instance of <see cref="NiceByNature"/>.
        /// </summary>
        /// <returns>A new <see cref="NiceByNature"/> instance.</returns>
        public IStrategy Clone()
        {
            return new NiceByNature();
        }
    }
}
