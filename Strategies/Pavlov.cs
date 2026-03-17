using System;
using System.Collections.Generic;
using PrisonersDilemma.Interfaces;

namespace PrisonersDilemma.Strategies
{
    /// <summary>
    /// The Pavlov (Win-Stay, Lose-Shift) strategy. Cooperates on the first move.
    /// On each subsequent move it evaluates the outcome of the previous round using
    /// the standard payoff matrix:
    /// <list type="bullet">
    ///   <item><description>CC (mutual cooperate) = 3 points — repeat own last move.</description></item>
    ///   <item><description>DC (I defected, they cooperated) = 5 points — repeat own last move.</description></item>
    ///   <item><description>CD (I cooperated, they defected) = 0 points — switch move.</description></item>
    ///   <item><description>DD (mutual defect) = 1 point — switch move.</description></item>
    /// </list>
    /// Pavlov converges to mutual cooperation against cooperative strategies and can
    /// correct mutual defection cycles over time.
    /// </summary>
    public class Pavlov : IStrategy
    {
        /// <summary>
        /// Gets the name of this strategy.
        /// </summary>
        public string Name => "Pavlov";

        /// <summary>
        /// Cooperates on round 0. On subsequent rounds, repeats the previous action
        /// if the outcome was "winning" (CC or DC), or switches if "losing" (CD or DD).
        /// </summary>
        /// <param name="myHistory">The history of this strategy's own actions.</param>
        /// <param name="opponentHistory">The history of the opponent's actions.</param>
        /// <returns>The chosen <see cref="Action"/> based on win-stay, lose-shift logic.</returns>
        public Action GetAction(IReadOnlyList<Action> myHistory, IReadOnlyList<Action> opponentHistory)
        {
            if (myHistory.Count == 0)
                return Action.Cooperate;

            Action myLast = myHistory[myHistory.Count - 1];
            Action oppLast = opponentHistory[opponentHistory.Count - 1];

            // Win conditions: CC (3 pts) or DC (5 pts) — stay with current move.
            bool win = (myLast == Action.Cooperate && oppLast == Action.Cooperate)
                    || (myLast == Action.Defect   && oppLast == Action.Cooperate);

            if (win)
                return myLast;

            // Lose conditions: CD (0 pts) or DD (1 pt) — shift to opposite move.
            return myLast == Action.Cooperate ? Action.Defect : Action.Cooperate;
        }

        /// <summary>
        /// Resets the strategy state. No-op for this strategy as it holds no mutable state.
        /// </summary>
        public void Reset()
        {
            // No state to reset.
        }

        /// <summary>
        /// Creates a new instance of <see cref="Pavlov"/>.
        /// </summary>
        /// <returns>A new <see cref="Pavlov"/> instance.</returns>
        public IStrategy Clone()
        {
            return new Pavlov();
        }
    }
}
