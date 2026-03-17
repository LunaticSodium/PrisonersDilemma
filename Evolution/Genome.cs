using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using PrisonersDilemma.Interfaces;

namespace PrisonersDilemma.Evolution
{
    /// <summary>
    /// A genome for the self-evolving strategy. Uses a lookup table of 64 history states
    /// (last 3 own moves × last 3 opponent moves) mapping to C/D actions, plus a first-move bit.
    /// This gives 65 total bits defining the complete strategy.
    /// </summary>
    public class Genome
    {
        /// <summary>Number of history states (2^6 = 64).</summary>
        public const int TableSize = 64;

        /// <summary>First move: true = Cooperate, false = Defect.</summary>
        [JsonPropertyName("firstMove")]
        public bool FirstMove { get; set; }

        /// <summary>Lookup table mapping 6-bit history index to action (true=C, false=D).</summary>
        [JsonPropertyName("table")]
        public bool[] Table { get; set; } = new bool[TableSize];

        /// <summary>Fitness score assigned during evaluation.</summary>
        [JsonIgnore]
        public double Fitness { get; set; }

        /// <summary>Create a random genome.</summary>
        public static Genome Random(Random rng)
        {
            var g = new Genome();
            g.FirstMove = rng.NextDouble() < 0.5;
            for (int i = 0; i < TableSize; i++)
                g.Table[i] = rng.NextDouble() < 0.5;
            return g;
        }

        /// <summary>Deep copy.</summary>
        public Genome Clone()
        {
            var g = new Genome { FirstMove = this.FirstMove, Fitness = this.Fitness };
            Array.Copy(Table, g.Table, TableSize);
            return g;
        }

        /// <summary>Single-point crossover between two parents.</summary>
        public static (Genome child1, Genome child2) Crossover(Genome a, Genome b, Random rng)
        {
            // point == 0 means swap firstMove but no table entries; point == k means first k table entries from a
            int point = rng.Next(0, TableSize + 1);
            var c1 = new Genome { FirstMove = point == 0 ? b.FirstMove : a.FirstMove };
            var c2 = new Genome { FirstMove = point == 0 ? a.FirstMove : b.FirstMove };
            for (int i = 0; i < TableSize; i++)
            {
                c1.Table[i] = i < point ? a.Table[i] : b.Table[i];
                c2.Table[i] = i < point ? b.Table[i] : a.Table[i];
            }
            return (c1, c2);
        }

        /// <summary>Bit-flip mutation with probability mutRate per bit (default 1/64).</summary>
        public void Mutate(Random rng, double mutRate = 1.0 / 64)
        {
            if (rng.NextDouble() < mutRate) FirstMove = !FirstMove;
            for (int i = 0; i < TableSize; i++)
                if (rng.NextDouble() < mutRate) Table[i] = !Table[i];
        }

        /// <summary>
        /// Compute the 6-bit history index from the last 3 own moves and last 3 opponent moves.
        /// Encoding: myBits (3 bits, older=MSB) concatenated with oppBits (3 bits, older=MSB).
        /// Index = (myBits &lt;&lt; 3) | oppBits.
        /// </summary>
        public static int ComputeHistoryIndex(IReadOnlyList<Action> myHistory, IReadOnlyList<Action> oppHistory)
        {
            int n = myHistory.Count;
            int myBits = 0;
            for (int i = 0; i < 3; i++)
            {
                int idx = n - 3 + i;
                if (idx >= 0 && idx < n)
                    myBits |= (myHistory[idx] == Action.Defect ? 1 : 0) << (2 - i);
            }
            int oppBits = 0;
            for (int i = 0; i < 3; i++)
            {
                int idx = n - 3 + i;
                if (idx >= 0 && idx < n)
                    oppBits |= (oppHistory[idx] == Action.Defect ? 1 : 0) << (2 - i);
            }
            return (myBits << 3) | oppBits;
        }

        /// <summary>Convert this genome into an IStrategy instance.</summary>
        public IStrategy ToStrategy(string name = "EvolvedStrategy") => new GenomeStrategy(this, name);

        /// <summary>Describe the genome in plain English (what pattern it follows).</summary>
        public string Describe()
        {
            int cooperates = 0;
            for (int i = 0; i < TableSize; i++) if (Table[i]) cooperates++;
            double coopRate = cooperates / (double)TableSize;
            string openStr = FirstMove ? "Cooperate" : "Defect";
            string tendency = coopRate > 0.75 ? "mostly cooperative"
                            : coopRate > 0.5  ? "somewhat cooperative"
                            : coopRate > 0.25 ? "somewhat aggressive"
                            : "mostly defecting";
            return $"Opens with {openStr}. In general {tendency} ({coopRate:P0} of history states map to Cooperate). " +
                   $"Cooperates when opponent recently cooperated: {AnalyzePattern(true)}. " +
                   $"Responds to recent defection: {AnalyzePattern(false)}.";
        }

        private string AnalyzePattern(bool oppCooperating)
        {
            // Count cooperations when opp's last 3 are all C (oppBits = 000 = 0) or all D (oppBits = 111 = 7)
            int oppBits = oppCooperating ? 0 : 7;
            int coopCount = 0;
            for (int myBits = 0; myBits < 8; myBits++)
            {
                int idx = (myBits << 3) | oppBits;
                if (Table[idx]) coopCount++;
            }
            return coopCount >= 6 ? "usually cooperates"
                 : coopCount >= 4 ? "mixed response"
                 : "usually defects";
        }
    }

    /// <summary>
    /// An IStrategy backed by a Genome lookup table.
    /// </summary>
    public class GenomeStrategy : IStrategy
    {
        private readonly Genome _genome;

        /// <inheritdoc/>
        public string Name { get; }

        /// <summary>The underlying genome.</summary>
        public Genome Genome => _genome;

        /// <summary>Create a strategy from a genome.</summary>
        public GenomeStrategy(Genome genome, string name = "EvolvedStrategy")
        {
            _genome = genome ?? throw new ArgumentNullException(nameof(genome));
            Name = name;
        }

        /// <inheritdoc/>
        public Action GetAction(IReadOnlyList<Action> myHistory, IReadOnlyList<Action> oppHistory)
        {
            if (myHistory.Count == 0)
                return _genome.FirstMove ? Action.Cooperate : Action.Defect;
            int idx = Genome.ComputeHistoryIndex(myHistory, oppHistory);
            return _genome.Table[idx] ? Action.Cooperate : Action.Defect;
        }

        /// <inheritdoc/>
        public void Reset() { }

        /// <inheritdoc/>
        public IStrategy Clone() => new GenomeStrategy(_genome.Clone(), Name);
    }
}
