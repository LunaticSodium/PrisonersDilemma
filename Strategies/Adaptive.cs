using System;
using System.Collections.Generic;
using PrisonersDilemma.Interfaces;

namespace PrisonersDilemma.Strategies
{
    /// <summary>
    /// Adaptive strategy. Plays a fixed opening sequence of C, C, C, D, D, D for the
    /// first six rounds. From round 6 onward it reviews the cumulative payoffs from all
    /// past cooperative and defective moves (using the standard payoff matrix: CC=3,
    /// CD=0, DC=5, DD=1) and chooses whichever action has yielded the higher average
    /// payoff. If averages are equal it cooperates.
    /// </summary>
    public class Adaptive : IStrategy
    {
        private static readonly Action[] OpeningSequence = new[]
        {
            Action.Cooperate, Action.Cooperate, Action.Cooperate,
            Action.Defect,    Action.Defect,    Action.Defect
        };

        private double _coopScoreSum;
        private double _defScoreSum;
        private int _coopRounds;
        private int _defRounds;

        /// <summary>
        /// Initialises a new instance of <see cref="Adaptive"/>.
        /// </summary>
        public Adaptive()
        {
            _coopScoreSum = 0;
            _defScoreSum  = 0;
            _coopRounds   = 0;
            _defRounds    = 0;
        }

        /// <summary>
        /// Gets the name of this strategy.
        /// </summary>
        public string Name => "Adaptive";

        /// <summary>
        /// Returns the fixed opening move for rounds 0–5, then selects the action with
        /// the higher historical average payoff. Tracks payoff history after each move.
        /// </summary>
        /// <param name="myHistory">The history of this strategy's own actions.</param>
        /// <param name="opponentHistory">The history of the opponent's actions.</param>
        /// <returns>The adaptively chosen <see cref="Action"/>.</returns>
        public Action GetAction(IReadOnlyList<Action> myHistory, IReadOnlyList<Action> opponentHistory)
        {
            int round = myHistory.Count;

            // Update running payoff totals based on the most recently completed round.
            if (round > 0)
            {
                Action myLast  = myHistory[round - 1];
                Action oppLast = opponentHistory[round - 1];
                int payoff     = GetPayoff(myLast, oppLast);

                if (myLast == Action.Cooperate)
                {
                    _coopScoreSum += payoff;
                    _coopRounds++;
                }
                else
                {
                    _defScoreSum += payoff;
                    _defRounds++;
                }
            }

            // Play fixed opening sequence for rounds 0–5.
            if (round < OpeningSequence.Length)
                return OpeningSequence[round];

            // Choose action with higher average payoff; cooperate on tie or missing data.
            double coopAvg = _coopRounds > 0 ? _coopScoreSum / _coopRounds : 0.0;
            double defAvg  = _defRounds  > 0 ? _defScoreSum  / _defRounds  : 0.0;

            return defAvg > coopAvg ? Action.Defect : Action.Cooperate;
        }

        /// <summary>
        /// Resets all accumulated payoff statistics to their initial values.
        /// </summary>
        public void Reset()
        {
            _coopScoreSum = 0;
            _defScoreSum  = 0;
            _coopRounds   = 0;
            _defRounds    = 0;
        }

        /// <summary>
        /// Creates a new instance of <see cref="Adaptive"/> in the initial state.
        /// </summary>
        /// <returns>A new <see cref="Adaptive"/> instance.</returns>
        public IStrategy Clone()
        {
            return new Adaptive();
        }

        /// <summary>
        /// Returns the payoff for the row player given both players' actions.
        /// </summary>
        /// <param name="mine">This player's action.</param>
        /// <param name="theirs">The opponent's action.</param>
        /// <returns>The integer payoff: CC=3, CD=0, DC=5, DD=1.</returns>
        private static int GetPayoff(Action mine, Action theirs)
        {
            if (mine == Action.Cooperate && theirs == Action.Cooperate) return 3;
            if (mine == Action.Cooperate && theirs == Action.Defect)    return 0;
            if (mine == Action.Defect    && theirs == Action.Cooperate) return 5;
            return 1; // DD
        }
    }
}
