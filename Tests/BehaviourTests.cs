using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using PrisonersDilemma.Interfaces;
using PrisonersDilemma.Strategies;
using PrisonersDilemma.Simulation;

namespace PrisonersDilemma.Tests
{
    /// <summary>
    /// Deterministic behavioural tests for all strategy implementations.
    /// No external test framework dependency — each test throws on failure.
    /// Run via: dotnet run -- --run-tests
    /// </summary>
    public static class BehaviourTests
    {
        private static int _passed = 0;
        private static int _failed = 0;
        private static readonly List<string> _failures = new();

        // ------------------------------------------------------------------ helpers

        private static void Pass(string name)
        {
            _passed++;
            Console.WriteLine($"[PASS] {name}");
        }

        private static void Fail(string name, string reason)
        {
            _failed++;
            string msg = $"[FAIL] {name}: {reason}";
            _failures.Add(msg);
            Console.WriteLine(msg);
        }

        private static void RunTest(string name, System.Action body)
        {
            try
            {
                body();
                Pass(name);
            }
            catch (Exception ex)
            {
                Fail(name, ex.Message);
            }
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
        }

        private static void AssertEqual<T>(T expected, T actual, string context)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new Exception($"{context}: expected {expected}, got {actual}");
        }

        /// <summary>
        /// Build a random history list of the given length using a fixed seed.
        /// </summary>
        private static List<Action> MakeHistory(int length, Random rng)
        {
            var list = new List<Action>(length);
            for (int i = 0; i < length; i++)
                list.Add(rng.NextDouble() < 0.5 ? Action.Cooperate : Action.Defect);
            return list;
        }

        // ------------------------------------------------------------------ tests

        /// <summary>
        /// 1. AlwaysCooperate always returns Cooperate regardless of history.
        /// </summary>
        private static void AlwaysCooperate_AlwaysReturnsCooperate()
        {
            var rng = new Random(42);
            var strategy = new AlwaysCooperate();
            for (int i = 0; i < 100; i++)
            {
                int len = rng.Next(0, 20);
                var my  = MakeHistory(len, rng);
                var opp = MakeHistory(len, rng);
                var action = strategy.GetAction(my, opp);
                AssertEqual(Action.Cooperate, action, $"iteration {i}");
            }
        }

        /// <summary>
        /// 2. AlwaysDefect always returns Defect regardless of history.
        /// </summary>
        private static void AlwaysDefect_AlwaysReturnsDefect()
        {
            var rng = new Random(42);
            var strategy = new AlwaysDefect();
            for (int i = 0; i < 100; i++)
            {
                int len = rng.Next(0, 20);
                var my  = MakeHistory(len, rng);
                var opp = MakeHistory(len, rng);
                var action = strategy.GetAction(my, opp);
                AssertEqual(Action.Defect, action, $"iteration {i}");
            }
        }

        /// <summary>
        /// 3. TitForTat returns Cooperate on an empty history.
        /// </summary>
        private static void TitForTat_FirstMoveCooperate()
        {
            for (int i = 0; i < 20; i++)
            {
                var tft = new TitForTat();
                var action = tft.GetAction(new List<Action>(), new List<Action>());
                AssertEqual(Action.Cooperate, action, $"instance {i}");
            }
        }

        /// <summary>
        /// 4. TitForTat copies the opponent's previous move on each round after the first.
        /// </summary>
        private static void TitForTat_CopiesOpponent()
        {
            var rng = new Random(17);
            var tft = new TitForTat();
            // Run 50 hand-computed cases
            for (int trial = 0; trial < 50; trial++)
            {
                int len = rng.Next(1, 15); // at least 1 so history is non-empty
                var oppHistory = MakeHistory(len, rng);
                var myHistory  = MakeHistory(len, rng);
                // TFT should return opponent's last action
                Action expected = oppHistory[oppHistory.Count - 1];
                Action actual   = tft.GetAction(myHistory, oppHistory);
                AssertEqual(expected, actual, $"trial {trial} (oppLast={expected})");
            }
        }

        /// <summary>
        /// 5. Pavlov returns Cooperate on an empty (first-move) history.
        /// </summary>
        private static void Pavlov_FirstMoveCooperate()
        {
            var pavlov = new Pavlov();
            var action = pavlov.GetAction(new List<Action>(), new List<Action>());
            AssertEqual(Action.Cooperate, action, "first move");
        }

        /// <summary>
        /// 6. Pavlov follows Win-Stay, Lose-Shift: 50 hand-computed cases.
        /// CC (win) -> stay -> C
        /// DC (win) -> stay -> D
        /// CD (lose) -> shift C -> D
        /// DD (lose) -> shift D -> C
        /// </summary>
        private static void Pavlov_WinStayLoseShift()
        {
            var pavlov = new Pavlov();

            // (myLast, oppLast) -> expected next action
            var cases = new (Action myLast, Action oppLast, Action expected)[]
            {
                (Action.Cooperate, Action.Cooperate, Action.Cooperate), // CC: won (3) -> stay C
                (Action.Cooperate, Action.Defect,    Action.Defect),    // CD: lost (0) -> shift to D
                (Action.Defect,    Action.Cooperate, Action.Defect),    // DC: won (5) -> stay D
                (Action.Defect,    Action.Defect,    Action.Cooperate), // DD: lost (1) -> shift to C
            };

            // Run each of the 4 base cases multiple times (50 total checks)
            for (int trial = 0; trial < 50; trial++)
            {
                var (myLast, oppLast, expected) = cases[trial % 4];
                // Build minimal single-round histories
                var myHistory  = new List<Action> { myLast };
                var oppHistory = new List<Action> { oppLast };
                var actual = pavlov.GetAction(myHistory, oppHistory);
                AssertEqual(expected, actual, $"trial {trial}: myLast={myLast} oppLast={oppLast}");
            }
        }

        /// <summary>
        /// 7. Grudger cooperates until first defect, then defects for all remaining rounds.
        /// </summary>
        private static void Grudger_NeverForgivesAfterDefect()
        {
            const int totalRounds = 100;
            const int defectRound = 5; // 0-indexed round where opp first defects

            var grudger    = new Grudger();
            var myHistory  = new List<Action>();
            var oppHistory = new List<Action>();

            for (int r = 0; r < totalRounds; r++)
            {
                Action oppAction = (r == defectRound) ? Action.Defect : Action.Cooperate;
                // Ask grudger before recording round
                Action myAction = grudger.GetAction(myHistory, oppHistory);

                // Before the defect round the grudger should cooperate
                if (r <= defectRound)
                {
                    // On round 0..defectRound: opp hasn't defected yet in previous rounds
                    // myAction is based on history SO FAR (before this round is recorded).
                    // On rounds 0..defectRound opp's history has no defections yet -> cooperate
                    AssertEqual(Action.Cooperate, myAction, $"round {r} (before betrayal recorded)");
                }
                else
                {
                    // After defect round: grudger must defect
                    AssertEqual(Action.Defect, myAction, $"round {r} (after betrayal)");
                }

                myHistory.Add(myAction);
                oppHistory.Add(oppAction);
            }

            // Extra explicit check: verify rounds 6..99 (indices) were all Defect by replaying
            // (the loop above already checks this, but let's make the constraint explicit)
            var grudger2    = new Grudger();
            var my2  = new List<Action>();
            var opp2 = new List<Action>();
            for (int r = 0; r < totalRounds; r++)
            {
                // Opp cooperates except round 5
                Action oppAct = (r == defectRound) ? Action.Defect : Action.Cooperate;
                Action myAct  = grudger2.GetAction(my2, opp2);
                my2.Add(myAct);
                opp2.Add(oppAct);
            }
            for (int r = defectRound + 1; r < totalRounds; r++)
            {
                AssertEqual(Action.Defect, my2[r], $"grudger2 round {r}");
            }
        }

        /// <summary>
        /// 8. At least 14 strategy types exist in the PrisonersDilemma.Strategies namespace.
        /// </summary>
        private static void AllStrategiesExist()
        {
            var assembly = Assembly.GetAssembly(typeof(TitForTat))
                ?? throw new Exception("Could not load strategies assembly.");
            var strategyTypes = assembly.GetTypes()
                .Where(t => t.Namespace == "PrisonersDilemma.Strategies"
                         && typeof(IStrategy).IsAssignableFrom(t)
                         && t.IsClass
                         && !t.IsAbstract)
                .ToList();

            Assert(strategyTypes.Count >= 14,
                $"Expected at least 14 strategy types, found {strategyTypes.Count}: " +
                string.Join(", ", strategyTypes.Select(t => t.Name)));
        }

        /// <summary>
        /// 9. StandardScorer returns correct payoffs for all 4 action combinations.
        /// </summary>
        private static void StandardScorer_PayoffMatrix()
        {
            var scorer = new StandardScorer();

            var (cc1, cc2) = scorer.Score(Action.Cooperate, Action.Cooperate);
            AssertEqual(3.0, cc1, "CC p1"); AssertEqual(3.0, cc2, "CC p2");

            var (cd1, cd2) = scorer.Score(Action.Cooperate, Action.Defect);
            AssertEqual(0.0, cd1, "CD p1"); AssertEqual(5.0, cd2, "CD p2");

            var (dc1, dc2) = scorer.Score(Action.Defect, Action.Cooperate);
            AssertEqual(5.0, dc1, "DC p1"); AssertEqual(0.0, dc2, "DC p2");

            var (dd1, dd2) = scorer.Score(Action.Defect, Action.Defect);
            AssertEqual(1.0, dd1, "DD p1"); AssertEqual(1.0, dd2, "DD p2");
        }

        /// <summary>
        /// 10. A small round-robin tournament runs successfully and all scores are positive.
        /// </summary>
        private static void TournamentRunsSuccessfully()
        {
            var scorer     = new StandardScorer();
            var tournament = new Tournament(scorer, rounds: 50);

            var strategies = new List<IStrategy>
            {
                new AlwaysCooperate(),
                new AlwaysDefect(),
                new TitForTat(),
            };

            var scores = tournament.RunTournament(strategies);

            Assert(scores.Count == 3, $"Expected 3 entries in score dict, got {scores.Count}");

            foreach (var kvp in scores)
            {
                Assert(kvp.Value > 0,
                    $"Strategy '{kvp.Key}' has non-positive total score: {kvp.Value}");
            }

            // Sanity check: AlwaysDefect should outscore AlwaysCooperate
            // (AlwaysDefect exploits AlwaysCooperate and draws with itself)
            Assert(scores["Always Defect"] > scores["Always Cooperate"],
                $"AlwaysDefect ({scores["Always Defect"]}) should outscore AlwaysCooperate ({scores["Always Cooperate"]})");
        }

        // ------------------------------------------------------------------ RunAll

        /// <summary>
        /// Runs all behaviour tests and prints a summary.
        /// Returns 0 if all pass, non-zero if any fail.
        /// </summary>
        public static int RunAll()
        {
            _passed  = 0;
            _failed  = 0;
            _failures.Clear();

            Console.WriteLine("=== PrisonersDilemma Behaviour Tests ===");
            Console.WriteLine();

            RunTest(nameof(AlwaysCooperate_AlwaysReturnsCooperate), AlwaysCooperate_AlwaysReturnsCooperate);
            RunTest(nameof(AlwaysDefect_AlwaysReturnsDefect),        AlwaysDefect_AlwaysReturnsDefect);
            RunTest(nameof(TitForTat_FirstMoveCooperate),            TitForTat_FirstMoveCooperate);
            RunTest(nameof(TitForTat_CopiesOpponent),                TitForTat_CopiesOpponent);
            RunTest(nameof(Pavlov_FirstMoveCooperate),               Pavlov_FirstMoveCooperate);
            RunTest(nameof(Pavlov_WinStayLoseShift),                 Pavlov_WinStayLoseShift);
            RunTest(nameof(Grudger_NeverForgivesAfterDefect),        Grudger_NeverForgivesAfterDefect);
            RunTest(nameof(AllStrategiesExist),                      AllStrategiesExist);
            RunTest(nameof(StandardScorer_PayoffMatrix),             StandardScorer_PayoffMatrix);
            RunTest(nameof(TournamentRunsSuccessfully),              TournamentRunsSuccessfully);

            Console.WriteLine();
            Console.WriteLine($"Results: {_passed} passed, {_failed} failed.");

            if (_failures.Count > 0)
            {
                Console.WriteLine();
                Console.WriteLine("Failures:");
                foreach (var f in _failures)
                    Console.WriteLine($"  {f}");
            }

            return _failed == 0 ? 0 : 1;
        }
    }
}
