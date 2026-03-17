using System;
using System.Collections.Generic;
using PrisonersDilemma.Interfaces;

namespace PrisonersDilemma.Strategies
{
    /// <summary>
    /// Gradual Tit for Tat. Cooperates until the opponent defects, then retaliates
    /// with a number of consecutive defections equal to the total number of times
    /// the opponent has defected so far. After completing the retaliation burst, it
    /// cooperates twice as a "cool-off" signal before resuming normal watch mode.
    /// This graduated response is more proportional than Grudger and more punishing
    /// than standard TFT.
    /// </summary>
    public class GradualTFT : IStrategy
    {
        private int _defectCount;
        private int _retaliationsLeft;
        private int _cooperationCooldown;

        /// <summary>
        /// Initialises a new instance of <see cref="GradualTFT"/>.
        /// </summary>
        public GradualTFT()
        {
            _defectCount = 0;
            _retaliationsLeft = 0;
            _cooperationCooldown = 0;
        }

        /// <summary>
        /// Gets the name of this strategy.
        /// </summary>
        public string Name => "Gradual Tit for Tat";

        /// <summary>
        /// Cooperates on round 0. Tracks opponent defections and responds with a
        /// proportional burst of defections followed by a two-round cooperative cool-off.
        /// </summary>
        /// <param name="myHistory">The history of this strategy's own actions.</param>
        /// <param name="opponentHistory">The history of the opponent's actions.</param>
        /// <returns>The chosen <see cref="Action"/> based on gradual retaliation logic.</returns>
        public Action GetAction(IReadOnlyList<Action> myHistory, IReadOnlyList<Action> opponentHistory)
        {
            if (opponentHistory.Count == 0)
                return Action.Cooperate;

            // Update state based on the opponent's most recent move.
            Action oppLast = opponentHistory[opponentHistory.Count - 1];

            if (_retaliationsLeft == 0 && _cooperationCooldown == 0)
            {
                // Normal watch mode: check if opponent just defected.
                if (oppLast == Action.Defect)
                {
                    _defectCount++;
                    _retaliationsLeft = _defectCount; // will be decremented below
                }
            }

            // Execute retaliation burst.
            if (_retaliationsLeft > 0)
            {
                _retaliationsLeft--;
                if (_retaliationsLeft == 0)
                    _cooperationCooldown = 2;
                return Action.Defect;
            }

            // Execute cooperation cool-off.
            if (_cooperationCooldown > 0)
            {
                _cooperationCooldown--;
                return Action.Cooperate;
            }

            return Action.Cooperate;
        }

        /// <summary>
        /// Resets all internal counters to their initial values.
        /// </summary>
        public void Reset()
        {
            _defectCount = 0;
            _retaliationsLeft = 0;
            _cooperationCooldown = 0;
        }

        /// <summary>
        /// Creates a new instance of <see cref="GradualTFT"/> in the initial state.
        /// </summary>
        /// <returns>A new <see cref="GradualTFT"/> instance.</returns>
        public IStrategy Clone()
        {
            return new GradualTFT();
        }
    }
}
