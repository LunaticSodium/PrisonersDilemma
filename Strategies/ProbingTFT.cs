using System;
using System.Collections.Generic;
using PrisonersDilemma.Interfaces;

namespace PrisonersDilemma.Strategies
{
    /// <summary>
    /// ProbingTFT (Probing Tit for Tat) periodically probes the opponent by defecting
    /// on random rounds (roughly 5% of the time) even if the opponent has been cooperating.
    /// If the opponent retaliates (defects in the next round after a probe), ProbingTFT
    /// reverts to pure TFT. If the opponent doesn't retaliate (continues cooperating),
    /// ProbingTFT exploits more aggressively (defects every 10th round). This models
    /// opportunistic agents who test opponent forgiveness to maximise exploitation while
    /// avoiding sustained conflict. Uses seeded Random.
    /// </summary>
    public class ProbingTFT : IStrategy
    {
        private Random _rng = new Random(12345);
        private bool _probing = false;
        private int _probeTimer = 0;

        /// <summary>
        /// Gets the name of this strategy.
        /// </summary>
        public string Name => "Probing Tit for Tat";

        /// <summary>
        /// Cooperates on round 0. From round 1 onward, generally mirrors the opponent's
        /// last action (TFT), but probes approximately every 20 rounds by defecting.
        /// If the opponent failed to retaliate after the last probe, enters exploit mode
        /// (defects every 10th round). Exits exploit mode if the opponent retaliates.
        /// </summary>
        /// <param name="myHistory">The history of this strategy's own actions.</param>
        /// <param name="opponentHistory">The history of the opponent's actions.</param>
        /// <returns>The chosen <see cref="Action"/>.</returns>
        public Action GetAction(IReadOnlyList<Action> myHistory, IReadOnlyList<Action> opponentHistory)
        {
            if (opponentHistory.Count == 0)
            {
                _probeTimer++;
                return Action.Cooperate;
            }

            Action oppLast = opponentHistory[opponentHistory.Count - 1];
            Action myLast  = myHistory[myHistory.Count - 1];

            // Determine whether the opponent is retaliating against a prior probe.
            bool opponentRetaliating = (myLast == Action.Defect && oppLast == Action.Defect);
            bool opponentIgnoredProbe = (myLast == Action.Defect && oppLast == Action.Cooperate);

            if (opponentRetaliating)
            {
                // Opponent pushed back: exit exploit mode and revert to TFT.
                _probing = false;
            }
            else if (opponentIgnoredProbe)
            {
                // Opponent did not retaliate: enter exploit mode.
                _probing = true;
            }

            Action decision;

            // Probing probe: defect every ~20 rounds.
            if (_probeTimer % 20 == 0)
            {
                decision = Action.Defect;
            }
            else if (_probing && _probeTimer % 10 == 0)
            {
                // Exploit mode: defect every 10 rounds.
                decision = Action.Defect;
            }
            else
            {
                // Standard TFT: copy opponent's last move.
                decision = oppLast;
            }

            _probeTimer++;
            return decision;
        }

        /// <summary>
        /// Resets all internal state, including the probe timer and exploit mode flag.
        /// </summary>
        public void Reset()
        {
            _rng        = new Random(12345);
            _probing    = false;
            _probeTimer = 0;
        }

        /// <summary>
        /// Creates a new instance of <see cref="ProbingTFT"/> in the initial state.
        /// </summary>
        /// <returns>A new <see cref="ProbingTFT"/> instance.</returns>
        public IStrategy Clone()
        {
            return new ProbingTFT();
        }
    }
}
