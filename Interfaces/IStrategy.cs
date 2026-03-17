using System.Collections.Generic;

namespace PrisonersDilemma.Interfaces
{
    /// <summary>Action a player can take in a round.</summary>
    public enum Action { Cooperate, Defect }

    /// <summary>
    /// Represents a tournament participant strategy for the Iterated Prisoner's Dilemma.
    /// Implementations must be deterministic given the same history (except RandomStrategy).
    /// </summary>
    public interface IStrategy
    {
        /// <summary>Unique display name for this strategy.</summary>
        string Name { get; }

        /// <summary>
        /// Choose an action given the history of moves so far.
        /// </summary>
        /// <param name="myHistory">This player's past moves (oldest first).</param>
        /// <param name="opponentHistory">Opponent's past moves (oldest first).</param>
        /// <returns>The chosen Action for this round.</returns>
        Action GetAction(IReadOnlyList<Action> myHistory, IReadOnlyList<Action> opponentHistory);

        /// <summary>Reset internal state for a new game.</summary>
        void Reset();

        /// <summary>Create a fresh independent copy of this strategy.</summary>
        IStrategy Clone();
    }
}
