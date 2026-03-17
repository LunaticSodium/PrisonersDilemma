using System;
using System.Collections.Generic;
using System.Linq;
using PrisonersDilemma.Interfaces;

namespace PrisonersDilemma.Evolution
{
    /// <summary>Result of one GA run.</summary>
    public class GaResult
    {
        /// <summary>Best genome found.</summary>
        public Genome BestGenome { get; set; } = new Genome();
        /// <summary>Best fitness per generation.</summary>
        public List<double> BestFitnessHistory { get; set; } = new();
        /// <summary>Mean fitness per generation.</summary>
        public List<double> MeanFitnessHistory { get; set; } = new();
        /// <summary>Number of generations run.</summary>
        public int Generations { get; set; }
    }

    /// <summary>
    /// Evolves a strategy genome using tournament selection, single-point crossover,
    /// and bit-flip mutation against a fixed set of named strategy opponents.
    /// </summary>
    public class GeneticAlgorithm
    {
        private readonly IReadOnlyList<IStrategy> _opponents;
        private readonly IScorer _scorer;
        private readonly int _rounds;
        private readonly int _popSize;
        private readonly int _generations;
        private readonly int _tournamentK;
        private readonly double _mutRate;

        /// <summary>
        /// Initialise the GA.
        /// </summary>
        /// <param name="opponents">Fixed named strategies used as fitness evaluators.</param>
        /// <param name="scorer">Payoff scorer.</param>
        /// <param name="rounds">Rounds per game in fitness evaluation.</param>
        /// <param name="popSize">Population size.</param>
        /// <param name="generations">Number of generations to run.</param>
        /// <param name="tournamentK">Tournament selection size.</param>
        /// <param name="mutRate">Per-bit mutation rate.</param>
        public GeneticAlgorithm(
            IReadOnlyList<IStrategy> opponents,
            IScorer scorer,
            int rounds = 200,
            int popSize = 30,
            int generations = 100,
            int tournamentK = 5,
            double mutRate = 1.0 / 64)
        {
            _opponents   = opponents   ?? throw new ArgumentNullException(nameof(opponents));
            _scorer      = scorer      ?? throw new ArgumentNullException(nameof(scorer));
            _rounds      = rounds;
            _popSize     = popSize;
            _generations = generations;
            _tournamentK = tournamentK;
            _mutRate     = mutRate;
        }

        /// <summary>Run the genetic algorithm and return results.</summary>
        public GaResult Run(int seed = 0)
        {
            var rng = new Random(seed);
            var population = InitialPopulation(rng);

            var result = new GaResult { Generations = _generations };
            Genome? best = null;

            for (int gen = 0; gen < _generations; gen++)
            {
                EvaluateFitness(population);

                var genBest = population.MaxBy(g => g.Fitness)!;
                double genMean = population.Average(g => g.Fitness);
                result.BestFitnessHistory.Add(genBest.Fitness);
                result.MeanFitnessHistory.Add(genMean);

                if (best == null || genBest.Fitness > best.Fitness)
                    best = genBest.Clone();

                // Build next generation
                var next = new List<Genome>(_popSize);

                // Elitism: carry over top 2
                foreach (var elite in population.OrderByDescending(g => g.Fitness).Take(2))
                    next.Add(elite.Clone());

                while (next.Count < _popSize)
                {
                    var parent1 = TournamentSelect(population, rng);
                    var parent2 = TournamentSelect(population, rng);
                    var (c1, c2) = Genome.Crossover(parent1, parent2, rng);
                    c1.Mutate(rng, _mutRate);
                    c2.Mutate(rng, _mutRate);
                    next.Add(c1);
                    if (next.Count < _popSize) next.Add(c2);
                }

                population = next;
            }

            // Final evaluation of last generation
            EvaluateFitness(population);
            var finalBest = population.MaxBy(g => g.Fitness)!;
            if (best == null || finalBest.Fitness > best.Fitness)
                best = finalBest.Clone();

            result.BestGenome = best!;
            return result;
        }

        private List<Genome> InitialPopulation(Random rng)
        {
            var pop = new List<Genome>(_popSize);
            for (int i = 0; i < _popSize; i++)
                pop.Add(Genome.Random(rng));
            return pop;
        }

        private void EvaluateFitness(List<Genome> population)
        {
            foreach (var genome in population)
            {
                var strategy = genome.ToStrategy("Evolved");
                double totalScore = 0;
                foreach (var opp in _opponents)
                    totalScore += PlayGame(strategy, opp.Clone());
                genome.Fitness = totalScore / _opponents.Count;
            }
        }

        private double PlayGame(IStrategy player, IStrategy opponent)
        {
            player.Reset();
            opponent.Reset();
            var myHistory  = new List<Action>(_rounds);
            var oppHistory = new List<Action>(_rounds);
            double totalScore = 0;

            for (int r = 0; r < _rounds; r++)
            {
                var myAction  = player.GetAction(myHistory, oppHistory);
                var oppAction = opponent.GetAction(oppHistory, myHistory);
                var (s1, _)   = _scorer.Score(myAction, oppAction);
                totalScore += s1;
                myHistory.Add(myAction);
                oppHistory.Add(oppAction);
            }
            return totalScore;
        }

        private Genome TournamentSelect(List<Genome> population, Random rng)
        {
            Genome? best = null;
            for (int i = 0; i < _tournamentK; i++)
            {
                var candidate = population[rng.Next(population.Count)];
                if (best == null || candidate.Fitness > best.Fitness)
                    best = candidate;
            }
            return best!.Clone();
        }
    }
}
