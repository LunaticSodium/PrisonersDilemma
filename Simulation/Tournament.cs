using System;
using System.Collections.Generic;
using System.Linq;
using PrisonersDilemma.Interfaces;

namespace PrisonersDilemma.Simulation
{
    /// <summary>
    /// Runs a round-robin tournament in which every unordered pair of strategies plays against
    /// each other in both orderings (A vs B and B vs A), accumulating total scores per strategy.
    /// </summary>
    /// <remarks>
    /// For each unordered pair {i, j} the tournament plays two directed games:
    /// strategy i as player 1 against strategy j as player 2, and vice-versa.
    /// The scores from both games are summed and added to each strategy's total.
    /// Self-play (i == j) is also included, playing the strategy against a separate clone of itself.
    /// </remarks>
    public class Tournament
    {
        private readonly IScorer _scorer;
        private readonly int _rounds;
        private double[,]? _pairwiseScores;

        /// <summary>
        /// Initialises a new <see cref="Tournament"/> with the specified scorer and number of rounds.
        /// </summary>
        /// <param name="scorer">The scorer used to evaluate each round's outcome.</param>
        /// <param name="rounds">The number of rounds each pair of strategies plays. Defaults to 200.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="scorer"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="rounds"/> is less than 1.</exception>
        public Tournament(IScorer scorer, int rounds = 200)
        {
            _scorer = scorer ?? throw new ArgumentNullException(nameof(scorer));
            if (rounds < 1)
                throw new ArgumentOutOfRangeException(nameof(rounds), "Rounds must be at least 1.");
            _rounds = rounds;
        }

        /// <summary>
        /// Runs a round-robin tournament among the provided strategies.
        /// </summary>
        /// <param name="strategies">
        /// The list of strategies to compete. Each strategy will be cloned internally so that the
        /// original instances are not modified.
        /// </param>
        /// <returns>
        /// A dictionary mapping each strategy's name to its total accumulated score across all games.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="strategies"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="strategies"/> is empty.</exception>
        public Dictionary<string, double> RunTournament(IReadOnlyList<IStrategy> strategies)
        {
            if (strategies == null) throw new ArgumentNullException(nameof(strategies));
            if (strategies.Count == 0) throw new ArgumentException("At least one strategy is required.", nameof(strategies));

            int n = strategies.Count;
            _pairwiseScores = new double[n, n];

            var totalScores = new Dictionary<string, double>();
            foreach (var s in strategies)
            {
                if (!totalScores.ContainsKey(s.Name))
                    totalScores[s.Name] = 0.0;
            }

            // Play every ordered pair (i, j) including i == j (self-play with a clone).
            for (int i = 0; i < n; i++)
            {
                for (int j = i; j < n; j++)
                {
                    // Game A: strategy i as player 1, strategy j as player 2
                    var gameA = PlayGame(strategies[i], strategies[j]);
                    // Game B (reverse): strategy j as player 1, strategy i as player 2
                    // (skip if i == j to avoid double-counting self-play)
                    GameResult gameB = i == j
                        ? null!
                        : PlayGame(strategies[j], strategies[i]);

                    double scoreI = gameA.Player1Score + (gameB != null ? gameB.Player2Score : 0.0);
                    double scoreJ = gameA.Player2Score + (gameB != null ? gameB.Player1Score : 0.0);

                    // For self-play both contributions go to the same strategy
                    if (i == j)
                    {
                        scoreI = gameA.Player1Score + gameA.Player2Score;
                        scoreJ = scoreI;
                    }

                    _pairwiseScores[i, j] = scoreI;
                    _pairwiseScores[j, i] = scoreJ;

                    totalScores[strategies[i].Name] += scoreI;
                    if (i != j)
                        totalScores[strategies[j].Name] += scoreJ;
                }
            }

            return totalScores;
        }

        /// <summary>
        /// Returns the pairwise score matrix produced by the most recent call to
        /// <see cref="RunTournament"/>.
        /// </summary>
        /// <returns>
        /// A two-dimensional array where element [i, j] is the total score earned by strategy i
        /// when playing against strategy j (in both orderings). Returns <c>null</c> if
        /// <see cref="RunTournament"/> has not yet been called.
        /// </returns>
        public double[,]? GetPairwiseScores() => _pairwiseScores;

        /// <summary>
        /// Plays a single directed game between two strategies for the configured number of rounds.
        /// </summary>
        /// <param name="player1">The strategy acting as player 1.</param>
        /// <param name="player2">The strategy acting as player 2.</param>
        /// <returns>A <see cref="GameResult"/> containing scores and action histories.</returns>
        private GameResult PlayGame(IStrategy player1, IStrategy player2)
        {
            // Clone both strategies so internal state is isolated per game
            var p1 = player1.Clone();
            var p2 = player2.Clone();
            p1.Reset();
            p2.Reset();

            var p1History = new List<Action>(_rounds);
            var p2History = new List<Action>(_rounds);
            double p1Total = 0.0;
            double p2Total = 0.0;

            for (int r = 0; r < _rounds; r++)
            {
                Action a1 = p1.GetAction(p1History, p2History);
                Action a2 = p2.GetAction(p2History, p1History);

                var (s1, s2) = _scorer.Score(a1, a2);
                p1Total += s1;
                p2Total += s2;

                p1History.Add(a1);
                p2History.Add(a2);
            }

            return new GameResult(
                player1.Name,
                player2.Name,
                p1Total,
                p2Total,
                _rounds,
                p1History.AsReadOnly(),
                p2History.AsReadOnly()
            );
        }
    }
}
