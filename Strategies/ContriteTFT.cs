using System;
using System.Collections.Generic;
using PrisonersDilemma.Interfaces;

namespace PrisonersDilemma.Strategies
{
    /// <summary>
    /// ContriteTFT (Contrite Tit for Tat) is a noise-tolerant variant of TFT. It tracks
    /// whether it accidentally defected against a cooperating opponent (contrite state).
    /// If contrite, it cooperates unconditionally next round to repair the relationship,
    /// preventing cycles of mutual retaliation from a single accidental defect. This is
    /// particularly valuable in environments with communication or implementation errors.
    /// Inspired by: Sugden (1986) The Economics of Rights, Cooperation and Welfare.
    /// </summary>
    public class ContriteTFT : IStrategy
    {
        private bool _contrite;
#pragma warning disable CS0414
        private bool _provoked;
#pragma warning restore CS0414

        /// <summary>
        /// Initialises a new instance of <see cref="ContriteTFT"/>.
        /// </summary>
        public ContriteTFT()
        {
            _contrite  = false;
            _provoked  = false;
        }

        /// <summary>
        /// Gets the name of this strategy.
        /// </summary>
        public string Name => "Contrite Tit for Tat";

        /// <summary>
        /// Cooperates on round 0. Each subsequent round:
        /// <list type="bullet">
        ///   <item><description>If contrite (we previously defected while opponent cooperated): cooperate unconditionally and clear the contrite flag.</description></item>
        ///   <item><description>If opponent defected last round and we cooperated last round: retaliate with defection.</description></item>
        ///   <item><description>If opponent defected last round and we also defected last round: cooperate to break the mutual-defection cycle, entering a contrite state.</description></item>
        ///   <item><description>Otherwise (opponent cooperated): cooperate.</description></item>
        /// </list>
        /// </summary>
        /// <param name="myHistory">The history of this strategy's own actions.</param>
        /// <param name="opponentHistory">The history of the opponent's actions.</param>
        /// <returns>The chosen <see cref="Action"/>.</returns>
        public Action GetAction(IReadOnlyList<Action> myHistory, IReadOnlyList<Action> opponentHistory)
        {
            if (opponentHistory.Count == 0)
                return Action.Cooperate;

            Action myLast  = myHistory[myHistory.Count - 1];
            Action oppLast = opponentHistory[opponentHistory.Count - 1];

            // If we are contrite (we defected against a cooperator last round), cooperate
            // unconditionally this round to repair the relationship.
            if (_contrite)
            {
                _contrite = false;
                return Action.Cooperate;
            }

            if (oppLast == Action.Defect)
            {
                if (myLast == Action.Cooperate)
                {
                    // Opponent defected while we cooperated: retaliate.
                    _provoked = true;
                    return Action.Defect;
                }
                else
                {
                    // Both defected last round: cooperate to break the cycle,
                    // and become contrite to prevent further escalation.
                    _contrite = true;
                    _provoked = false;
                    return Action.Cooperate;
                }
            }

            // Opponent cooperated last round.
            _contrite = false;
            _provoked = false;
            return Action.Cooperate;
        }

        /// <summary>
        /// Resets the contrite and provoked flags to their initial values.
        /// </summary>
        public void Reset()
        {
            _contrite = false;
            _provoked = false;
        }

        /// <summary>
        /// Creates a new instance of <see cref="ContriteTFT"/> in the initial state.
        /// </summary>
        /// <returns>A new <see cref="ContriteTFT"/> instance.</returns>
        public IStrategy Clone()
        {
            return new ContriteTFT();
        }
    }
}
