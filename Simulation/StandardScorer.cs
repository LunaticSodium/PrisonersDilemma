using System;
using System.Collections.Generic;
using System.Linq;
using PrisonersDilemma.Interfaces;

namespace PrisonersDilemma.Simulation
{
    /// <summary>
    /// Implements the classic Prisoner's Dilemma payoff matrix scoring.
    /// </summary>
    /// <remarks>
    /// Payoff matrix:
    /// <list type="table">
    ///   <listheader><term>Player1 \ Player2</term><description>Cooperate / Defect</description></listheader>
    ///   <item><term>Cooperate</term><description>(3, 3) / (0, 5)</description></item>
    ///   <item><term>Defect</term><description>(5, 0) / (1, 1)</description></item>
    /// </list>
    /// </remarks>
    public class StandardScorer : IScorer
    {
        /// <summary>
        /// Scores a single round of the Prisoner's Dilemma given both players' actions.
        /// </summary>
        /// <param name="player1Action">The action taken by player 1.</param>
        /// <param name="player2Action">The action taken by player 2.</param>
        /// <returns>
        /// A tuple of (player1Score, player2Score) according to the standard payoff matrix:
        /// CC -> (3,3), CD -> (0,5), DC -> (5,0), DD -> (1,1).
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when an unrecognised action combination is provided.
        /// </exception>
        public (double player1, double player2) Score(Action player1Action, Action player2Action)
        {
            return (player1Action, player2Action) switch
            {
                (Action.Cooperate, Action.Cooperate) => (3.0, 3.0),
                (Action.Cooperate, Action.Defect)    => (0.0, 5.0),
                (Action.Defect,    Action.Cooperate) => (5.0, 0.0),
                (Action.Defect,    Action.Defect)    => (1.0, 1.0),
                _ => throw new ArgumentOutOfRangeException(
                    $"Unrecognised action combination: ({player1Action}, {player2Action})")
            };
        }
    }
}
