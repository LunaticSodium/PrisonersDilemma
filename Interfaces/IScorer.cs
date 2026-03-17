namespace PrisonersDilemma.Interfaces
{
    /// <summary>
    /// Computes payoffs from a round's outcome according to the payoff matrix.
    /// </summary>
    public interface IScorer
    {
        /// <summary>
        /// Compute payoffs for both players given their moves.
        /// </summary>
        /// <param name="playerAction">The action of player 1.</param>
        /// <param name="opponentAction">The action of player 2.</param>
        /// <returns>Tuple of (player1Payoff, player2Payoff).</returns>
        (double player1, double player2) Score(Action playerAction, Action opponentAction);
    }
}
