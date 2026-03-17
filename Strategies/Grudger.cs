using System;
using System.Collections.Generic;
using PrisonersDilemma.Interfaces;

namespace PrisonersDilemma.Strategies
{
    /// <summary>
    /// The Grudger (also known as Grim Trigger) strategy. Cooperates unconditionally
    /// until the opponent defects even once, then switches to permanent defection for
    /// the remainder of the game. It never forgives a single betrayal, making it a
    /// powerful deterrent but inflexible in noisy environments.
    /// </summary>
    public class Grudger : IStrategy
    {
        private bool _betrayed;

        /// <summary>
        /// Initialises a new instance of <see cref="Grudger"/> in a cooperative state.
        /// </summary>
        public Grudger()
        {
            _betrayed = false;
        }

        /// <summary>
        /// Gets the name of this strategy.
        /// </summary>
        public string Name => "Grudger";

        /// <summary>
        /// Cooperates until the opponent defects. Once a defection is detected,
        /// defects for all remaining rounds.
        /// </summary>
        /// <param name="myHistory">The history of this strategy's own actions.</param>
        /// <param name="opponentHistory">The history of the opponent's actions.</param>
        /// <returns>
        /// <see cref="Action.Cooperate"/> until the opponent has defected at least once;
        /// <see cref="Action.Defect"/> permanently thereafter.
        /// </returns>
        public Action GetAction(IReadOnlyList<Action> myHistory, IReadOnlyList<Action> opponentHistory)
        {
            if (!_betrayed && opponentHistory.Count > 0
                && opponentHistory[opponentHistory.Count - 1] == Action.Defect)
            {
                _betrayed = true;
            }

            return _betrayed ? Action.Defect : Action.Cooperate;
        }

        /// <summary>
        /// Resets the strategy, clearing the betrayal flag so cooperation resumes.
        /// </summary>
        public void Reset()
        {
            _betrayed = false;
        }

        /// <summary>
        /// Creates a new instance of <see cref="Grudger"/> in the initial cooperative state.
        /// </summary>
        /// <returns>A new <see cref="Grudger"/> instance.</returns>
        public IStrategy Clone()
        {
            return new Grudger();
        }
    }
}
