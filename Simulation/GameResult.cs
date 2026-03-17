using System;
using System.Collections.Generic;
using System.Linq;
using PrisonersDilemma.Interfaces;

namespace PrisonersDilemma.Simulation
{
    /// <summary>
    /// Represents the complete result of a single game (series of rounds) between two strategies.
    /// </summary>
    /// <param name="Player1Name">The name of the first player's strategy.</param>
    /// <param name="Player2Name">The name of the second player's strategy.</param>
    /// <param name="Player1Score">The total accumulated score for player 1 over all rounds.</param>
    /// <param name="Player2Score">The total accumulated score for player 2 over all rounds.</param>
    /// <param name="Rounds">The number of rounds played in this game.</param>
    /// <param name="Player1History">The ordered list of actions taken by player 1, one per round.</param>
    /// <param name="Player2History">The ordered list of actions taken by player 2, one per round.</param>
    public record GameResult(
        string Player1Name,
        string Player2Name,
        double Player1Score,
        double Player2Score,
        int Rounds,
        IReadOnlyList<Action> Player1History,
        IReadOnlyList<Action> Player2History
    );
}
