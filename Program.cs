using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using PrisonersDilemma.Evolution;
using PrisonersDilemma.Interfaces;
using PrisonersDilemma.Scripts;
using PrisonersDilemma.Simulation;
using PrisonersDilemma.Strategies;
using PrisonersDilemma.Tests;

namespace PrisonersDilemma
{
    class Program
    {
        // =====================================================================
        // Entry point
        // =====================================================================

#pragma warning disable CS1998
        static async Task<int> Main(string[] args)
        {
            // ---- Test mode --------------------------------------------------
            if (args.Contains("--run-tests"))
            {
                int result = BehaviourTests.RunAll();
                return result;
            }

            Console.WriteLine("=== Prisoner's Dilemma Evolutionary Simulation ===");
            Console.WriteLine();

            // Ensure output directory exists
            Directory.CreateDirectory("Output");

            var scorer     = new StandardScorer();
            var strategies = GetAllStrategies();

            // Strategy names for display
            var strategyNames = strategies.Select(s => s.Name).ToList();
            Console.WriteLine($"Strategies ({strategies.Count}): {string.Join(", ", strategyNames)}");
            Console.WriteLine();

            // ================================================================
            // Phase 3a: Round-robin tournament
            // ================================================================
            Console.WriteLine("[Phase 3a] Running round-robin tournament (R=200 rounds)...");
            var tournament = new Tournament(scorer, rounds: 200);
            var tournamentScores = tournament.RunTournament(strategies);
            var pairwiseScores   = tournament.GetPairwiseScores()!;

            Console.WriteLine("  Tournament totals:");
            foreach (var kv in tournamentScores.OrderByDescending(x => x.Value))
                Console.WriteLine($"    {kv.Key,-30} {kv.Value:F1}");
            Console.WriteLine();

            string tournamentWinner = tournamentScores.OrderByDescending(x => x.Value).First().Key;

            // Save pairwise CSV
            SavePairwiseCsv("Output/pairwise_scores.csv", strategyNames, pairwiseScores);
            Console.WriteLine("  Saved Output/pairwise_scores.csv");

            // ================================================================
            // Phase 3b: Population simulation — Proportional selection
            // ================================================================
            Console.WriteLine("[Phase 3b] Running population simulation with ProportionalSelection (G=100, N=200, R=200)...");
            var propRule = new ProportionalSelection();

            var propSimSingle = new PopulationSimulation(strategies, propRule, scorer,
                                                         n: 200, generations: 100, rounds: 200);
            var propResult = propSimSingle.Run(seed: 0);

            string propWinner = propResult.FinalAbundances
                                          .OrderByDescending(x => x.Value)
                                          .First().Key;
            Console.WriteLine($"  Proportional selection winner (seed 0): {propWinner}");
            SaveAbundanceCsv("Output/abundance_proportional_seed0.csv",
                             new List<SimulationResult> { propResult });
            // Also save under the canonical name required by the spec
            SaveAbundanceCsv("Output/abundance_history.csv",
                             new List<SimulationResult> { propResult });
            Console.WriteLine("  Saved Output/abundance_proportional_seed0.csv");
            Console.WriteLine();

            // ================================================================
            // Phase 3b (continued): Tournament selection
            // ================================================================
            Console.WriteLine("[Phase 3b] Running population simulation with TournamentSelection (G=100, N=200, R=200)...");
            var tournSel = new TournamentSelection { TournamentSize = 5 };

            var tournSimSingle = new PopulationSimulation(strategies, tournSel, scorer,
                                                          n: 200, generations: 100, rounds: 200);
            var tournSelResult = tournSimSingle.Run(seed: 0);

            string tournSelWinner = tournSelResult.FinalAbundances
                                                  .OrderByDescending(x => x.Value)
                                                  .First().Key;
            Console.WriteLine($"  Tournament selection winner (seed 0): {tournSelWinner}");
            SaveAbundanceCsv("Output/abundance_tournament_seed0.csv",
                             new List<SimulationResult> { tournSelResult });
            Console.WriteLine("  Saved Output/abundance_tournament_seed0.csv");
            Console.WriteLine();

            // ================================================================
            // Phase 3c: Ensemble (M=50 seeds) — both selection rules
            // ================================================================
            Console.WriteLine("[Phase 3c] Running ensemble (M=50) for ProportionalSelection...");
            var propEnsemble = new EnsembleRunner(strategies, propRule, scorer,
                                                  n: 200, generations: 100, rounds: 200);
            var propEnsembleResult = propEnsemble.RunEnsemble(numSeeds: 50);
            Console.WriteLine("  ProportionalSelection ensemble complete.");

            Console.WriteLine("[Phase 3c] Running ensemble (M=50) for TournamentSelection...");
            var tournEnsemble = new EnsembleRunner(strategies, tournSel, scorer,
                                                   n: 200, generations: 100, rounds: 200);
            var tournEnsembleResult = tournEnsemble.RunEnsemble(numSeeds: 50);
            Console.WriteLine("  TournamentSelection ensemble complete.");

            // Save ensemble CSVs
            SaveEnsembleCsv("Output/ensemble_proportional.csv", strategyNames, propEnsembleResult);
            SaveEnsembleCsv("Output/ensemble_tournament.csv",   strategyNames, tournEnsembleResult);
            // Also save under the canonical name required by the spec
            SaveEnsembleCsv("Output/ensemble_summary.csv",      strategyNames, propEnsembleResult);
            Console.WriteLine("  Saved Output/ensemble_proportional.csv and Output/ensemble_tournament.csv");
            Console.WriteLine();

            // ================================================================
            // Phase 3d: Sensitivity analysis — ProportionalSelection
            // ================================================================
            Console.WriteLine("[Phase 3d] Running sensitivity analysis (ProportionalSelection)...");
            var sensitivity = new SensitivityAnalysis(strategies, propRule, scorer,
                                                       n: 200, generations: 100, rounds: 200);
            var sensitivityResults = sensitivity.Run(seedsPerCondition: 10);
            SaveSensitivityCsv("Output/sensitivity.csv", sensitivityResults);
            // Also save under the required canonical name
            SaveSensitivityCsv("Output/sensitivity_results.csv", sensitivityResults);
            Console.WriteLine("  Saved Output/sensitivity.csv");
            Console.WriteLine();

            // ================================================================
            // Phase 5a: Figures (Plotter)
            // ================================================================
            Console.WriteLine("[Phase 5a] Generating figures...");
            var plotter = new Plotter();

            // Cast mean abundance lists for the plotter interface
            var propMeanCast  = propEnsembleResult.MeanAbundance
                                    .Select(d => (IReadOnlyDictionary<string, double>)d)
                                    .ToList();
            var tournMeanCast = tournEnsembleResult.MeanAbundance
                                    .Select(d => (IReadOnlyDictionary<string, double>)d)
                                    .ToList();

            TryPlot(() => plotter.PlotAbundanceOverTime(propMeanCast, "ProportionalSelection", 50,
                                                        "Output/abundance_proportional.png"),
                    "Output/abundance_proportional.png");

            TryPlot(() => plotter.PlotAbundanceOverTime(tournMeanCast, "TournamentSelection", 50,
                                                        "Output/abundance_tournament.png"),
                    "Output/abundance_tournament.png");

            TryPlot(() => plotter.PlotPairwiseHeatmap(strategyNames, pairwiseScores,
                                                      "Output/pairwise_heatmap.png"),
                    "Output/pairwise_heatmap.png");

            // Final distribution bar charts
            var propFinalMeans  = strategyNames.Select(n => propEnsembleResult.MeanAbundance.Last()
                                              .TryGetValue(n, out double v) ? v : 0.0).ToList();
            var propFinalLow    = strategyNames.Select(n => propEnsembleResult.CiLow.Last()
                                              .TryGetValue(n, out double v) ? v : 0.0).ToList();
            var propFinalHigh   = strategyNames.Select(n => propEnsembleResult.CiHigh.Last()
                                              .TryGetValue(n, out double v) ? v : 0.0).ToList();

            TryPlot(() => plotter.PlotFinalDistribution(strategyNames, propFinalMeans,
                                                        propFinalLow, propFinalHigh,
                                                        "ProportionalSelection",
                                                        "Output/final_distribution_proportional.png"),
                    "Output/final_distribution_proportional.png");

            var tournFinalMeans = strategyNames.Select(n => tournEnsembleResult.MeanAbundance.Last()
                                              .TryGetValue(n, out double v) ? v : 0.0).ToList();
            var tournFinalLow   = strategyNames.Select(n => tournEnsembleResult.CiLow.Last()
                                              .TryGetValue(n, out double v) ? v : 0.0).ToList();
            var tournFinalHigh  = strategyNames.Select(n => tournEnsembleResult.CiHigh.Last()
                                              .TryGetValue(n, out double v) ? v : 0.0).ToList();

            TryPlot(() => plotter.PlotFinalDistribution(strategyNames, tournFinalMeans,
                                                        tournFinalLow, tournFinalHigh,
                                                        "TournamentSelection",
                                                        "Output/final_distribution_tournament.png"),
                    "Output/final_distribution_tournament.png");

            // Sensitivity plots — one per strategy
            foreach (var strat in strategies)
            {
                var results    = sensitivityResults.Where(r => r.StrategyName == strat.Name).ToList();
                var initAbunds = results.Select(r => r.InitialAbundance).ToList();
                var survRates  = results.Select(r => r.SurvivalRate).ToList();
                string safeName = strat.Name.Replace(" ", "_").Replace("/", "_");
                TryPlot(() => plotter.PlotSensitivity(strat.Name, initAbunds, survRates,
                                                      $"Output/sensitivity_{safeName}.png"),
                        $"Output/sensitivity_{safeName}.png");
            }

            Console.WriteLine();

            // ================================================================
            // Phase 6: Genetic Algorithm
            // ================================================================
            Console.WriteLine("[Phase 6] Running genetic algorithm (pop=30, G=100)...");
            var ga = new GeneticAlgorithm(strategies, scorer,
                                          rounds:      200,
                                          popSize:     30,
                                          generations: 100,
                                          tournamentK: 5,
                                          mutRate:     1.0 / 64);
            var gaResult = ga.Run(seed: 0);
            var bestGenome = gaResult.BestGenome;

            Console.WriteLine($"  Best fitness: {bestGenome.Fitness:F2}");
            Console.WriteLine($"  Description:  {bestGenome.Describe()}");

            // Save evolved_strategy.json
            string genomeJson = JsonSerializer.Serialize(bestGenome, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            SaveCsv("Output/evolved_strategy.json", genomeJson);
            Console.WriteLine("  Saved Output/evolved_strategy.json");

            // Re-run population sim with evolved strategy included
            Console.WriteLine("[Phase 6] Re-running population simulation with EvolvedStrategy...");
            var evolvedStrategy = bestGenome.ToStrategy("EvolvedStrategy");
            var strategiesWithEvolved = new List<IStrategy>(strategies) { evolvedStrategy };

            var evolvedPropSim = new PopulationSimulation(strategiesWithEvolved, propRule, scorer,
                                                          n: 200, generations: 100, rounds: 200);
            var evolvedPropResult = evolvedPropSim.Run(seed: 0);

            bool evolvedSurvives = evolvedPropResult.FinalAbundances
                                       .TryGetValue("EvolvedStrategy", out double evolvedFinalAbundance)
                                   && evolvedFinalAbundance >= 0.01;

            Console.WriteLine($"  EvolvedStrategy final abundance: {evolvedFinalAbundance:P1}");
            Console.WriteLine($"  EvolvedStrategy survives: {evolvedSurvives}");

            SaveAbundanceCsv("Output/abundance_evolved.csv",
                             new List<SimulationResult> { evolvedPropResult });
            Console.WriteLine("  Saved Output/abundance_evolved.csv");

            // Compare evolved vs TFT in a head-to-head 200-round game
            string evolvedVsTftSummary = EvolvedVsTftSummary(evolvedStrategy, scorer);
            Console.WriteLine($"  Evolved vs TFT: {evolvedVsTftSummary}");
            Console.WriteLine();

            // ================================================================
            // Phase 5b: Report
            // ================================================================
            Console.WriteLine("[Phase 5b] Generating Markdown report...");

            // Determine ensemble winners (highest mean final abundance)
            string propEnsWinner  = GetEnsembleWinner(propEnsembleResult,  strategyNames);
            string tournEnsWinner = GetEnsembleWinner(tournEnsembleResult, strategyNames);

            var simParams = new Dictionary<string, string>
            {
                ["Strategies"]           = string.Join(", ", strategyNames),
                ["Population size (N)"]  = "200",
                ["Generations (G)"]      = "100",
                ["Rounds per game (R)"]  = "200",
                ["Ensemble runs (M)"]    = "50",
                ["Seeds per condition"]  = "10 (sensitivity)",
                ["GA population size"]   = "30",
                ["GA generations"]       = "100",
                ["GA tournament k"]      = "5",
                ["GA mutation rate"]     = "1/64 per bit",
                ["Payoff matrix"]        = "T=5, R=3, P=1, S=0",
                ["Selection rules"]      = "ProportionalSelection, TournamentSelection",
            };

            var reportFindings = new Dictionary<string, string>
            {
                ["WinnerTournamentScoring"]  = tournamentWinner,
                ["WinnerProportional"]       = propEnsWinner,
                ["WinnerTournament"]         = tournEnsWinner,
                ["EvolvedDescription"]       = bestGenome.Describe(),
                ["EvolvedVsTFT"]             = evolvedVsTftSummary,
                ["EvolvedSurvives"]          = evolvedSurvives.ToString(),
                ["EvolvedFinalAbundance"]    = $"{evolvedFinalAbundance:P1}",
                ["GABestFitness"]            = $"{bestGenome.Fitness:F2}",
            };

            // Add per-strategy final abundances (proportional ensemble)
            foreach (var name in strategyNames)
            {
                if (propEnsembleResult.MeanAbundance.Last().TryGetValue(name, out double abund))
                    reportFindings[$"PropFinalAbundance_{name}"] = $"{abund:P1}";
            }

            var reporter = new Reporter();
            reporter.GenerateReport("Output/report.md", simParams, reportFindings);
            Console.WriteLine();

            // ================================================================
            // Summary
            // ================================================================
            Console.WriteLine("=== Simulation Complete ===");
            Console.WriteLine();
            Console.WriteLine("Output files:");
            foreach (var file in Directory.GetFiles("Output").OrderBy(f => f))
                Console.WriteLine($"  {file}");
            Console.WriteLine();
            Console.WriteLine($"Tournament (round-robin) winner:           {tournamentWinner}");
            Console.WriteLine($"Proportional selection ensemble winner:    {propEnsWinner}");
            Console.WriteLine($"Tournament selection ensemble winner:      {tournEnsWinner}");
            Console.WriteLine($"Evolved strategy survives population sim:  {evolvedSurvives}");

            return 0;
        }

        // =====================================================================
        // Strategy factory
        // =====================================================================

        /// <summary>Returns a fresh list of all 14 strategy instances.</summary>
        static IReadOnlyList<IStrategy> GetAllStrategies()
        {
            return new List<IStrategy>
            {
                new AlwaysCooperate(),
                new AlwaysDefect(),
                new TitForTat(),
                new TitForTwoTats(),
                new SuspiciousTFT(),
                new GenerousTFT(),
                new Pavlov(),
                new Grudger(),
                new RandomStrategy(),
                new GradualTFT(),
                new NiceByNature(),
                new Adaptive(),
                new ContriteTFT(),
                new ProbingTFT(),
            };
        }

        // =====================================================================
        // CSV helpers
        // =====================================================================

        /// <summary>Writes text content to a file (creates or overwrites).</summary>
        static void SaveCsv(string path, string content)
        {
            string? dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            // Use UTF8 without BOM to ensure JSON and CSV files are clean
            File.WriteAllText(path, content, new System.Text.UTF8Encoding(false));
        }

        /// <summary>
        /// Writes abundance history to CSV.
        /// Columns: Run, Generation, [strategy names...]
        /// </summary>
        static void SaveAbundanceCsv(string path, List<SimulationResult> runs)
        {
            if (runs.Count == 0) return;

            // Collect all strategy names from first run's first generation
            var names = runs[0].AbundanceHistory.Count > 0
                ? runs[0].AbundanceHistory[0].Keys.ToList()
                : new List<string>();

            var sb = new StringBuilder();
            sb.Append("Run,Generation");
            foreach (var n in names)
                sb.Append($",{CsvEscape(n)}");
            sb.AppendLine();

            for (int r = 0; r < runs.Count; r++)
            {
                var run = runs[r];
                for (int g = 0; g < run.AbundanceHistory.Count; g++)
                {
                    sb.Append($"{r},{g}");
                    var gen = run.AbundanceHistory[g];
                    foreach (var n in names)
                    {
                        double v = gen.TryGetValue(n, out double val) ? val : 0.0;
                        sb.Append($",{v:F6}");
                    }
                    sb.AppendLine();
                }
            }

            SaveCsv(path, sb.ToString());
        }

        /// <summary>
        /// Writes pairwise tournament scores to CSV.
        /// Rows and columns are labelled by strategy name.
        /// </summary>
        static void SavePairwiseCsv(string path, List<string> strategyNames, double[,] scores)
        {
            int n = strategyNames.Count;
            var sb = new StringBuilder();

            // Header
            sb.Append("Strategy");
            foreach (var name in strategyNames)
                sb.Append($",{CsvEscape(name)}");
            sb.AppendLine();

            // Rows
            for (int i = 0; i < n; i++)
            {
                sb.Append(CsvEscape(strategyNames[i]));
                for (int j = 0; j < n; j++)
                    sb.Append($",{scores[i, j]:F2}");
                sb.AppendLine();
            }

            SaveCsv(path, sb.ToString());
        }

        /// <summary>
        /// Writes ensemble mean abundance (final generation) to CSV.
        /// Columns: Generation, [mean_NAME, ci_low_NAME, ci_high_NAME for each strategy]
        /// </summary>
        static void SaveEnsembleCsv(string path, List<string> strategyNames, EnsembleResult result)
        {
            var sb = new StringBuilder();

            // Header
            sb.Append("Generation");
            foreach (var name in strategyNames)
            {
                string safe = CsvEscape(name);
                sb.Append($",mean_{safe},ci_low_{safe},ci_high_{safe}");
            }
            sb.AppendLine();

            int genCount = result.MeanAbundance.Count;
            for (int g = 0; g < genCount; g++)
            {
                sb.Append(g);
                foreach (var name in strategyNames)
                {
                    double mean  = result.MeanAbundance[g].TryGetValue(name, out double m)  ? m  : 0.0;
                    double low   = result.CiLow[g].TryGetValue(name,         out double lo) ? lo : 0.0;
                    double high  = result.CiHigh[g].TryGetValue(name,        out double hi) ? hi : 0.0;
                    sb.Append($",{mean:F6},{low:F6},{high:F6}");
                }
                sb.AppendLine();
            }

            SaveCsv(path, sb.ToString());
        }

        /// <summary>
        /// Writes sensitivity analysis results to CSV.
        /// Columns: StrategyName, InitialAbundance, SurvivalRate, SurvivedMajority
        /// </summary>
        static void SaveSensitivityCsv(string path, List<SensitivityResult> results)
        {
            var sb = new StringBuilder();
            sb.AppendLine("StrategyName,InitialAbundance,SurvivalRate,SurvivedMajority");

            foreach (var r in results)
            {
                sb.AppendLine($"{CsvEscape(r.StrategyName)},{r.InitialAbundance:F4}," +
                              $"{r.SurvivalRate:F4},{r.SurvivedMajority}");
            }

            SaveCsv(path, sb.ToString());
        }

        // =====================================================================
        // Helper utilities
        // =====================================================================

        /// <summary>Wraps a value in double-quotes and escapes any existing double-quotes.</summary>
        private static string CsvEscape(string value)
        {
            if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            return value;
        }

        /// <summary>
        /// Returns the strategy name with the highest mean final abundance in an ensemble result.
        /// </summary>
        private static string GetEnsembleWinner(EnsembleResult ensemble, List<string> names)
        {
            if (ensemble.MeanAbundance.Count == 0) return "N/A";
            var lastGen = ensemble.MeanAbundance.Last();
            return names
                .OrderByDescending(n => lastGen.TryGetValue(n, out double v) ? v : 0.0)
                .First();
        }

        /// <summary>
        /// Plays the evolved strategy head-to-head against TFT for 200 rounds and returns a
        /// human-readable summary string.
        /// </summary>
        private static string EvolvedVsTftSummary(IStrategy evolved, IScorer scorer)
        {
            var tft    = new TitForTat();
            var ev     = evolved.Clone();
            ev.Reset();
            tft.Reset();

            var myHistory  = new List<Interfaces.Action>(200);
            var oppHistory = new List<Interfaces.Action>(200);
            double evScore = 0, tftScore = 0;

            for (int r = 0; r < 200; r++)
            {
                var evAction  = ev.GetAction(myHistory, oppHistory);
                var tftAction = tft.GetAction(oppHistory, myHistory);
                var (s1, s2)  = scorer.Score(evAction, tftAction);
                evScore  += s1;
                tftScore += s2;
                myHistory.Add(evAction);
                oppHistory.Add(tftAction);
            }

            string outcome = evScore > tftScore ? "Evolved wins"
                           : evScore < tftScore ? "TFT wins"
                           : "Draw";
            return $"{outcome} (Evolved={evScore:F1}, TFT={tftScore:F1} over 200 rounds)";
        }

        /// <summary>
        /// Runs a plot action and swallows exceptions so a missing Python environment does not
        /// abort the simulation.
        /// </summary>
        private static void TryPlot(System.Action plotAction, string outputPath)
        {
            try
            {
                plotAction();
                Console.WriteLine($"  Saved {outputPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  [Warning] Could not generate {outputPath}: {ex.Message}");
            }
        }

        /// <summary>
        /// Public async entry point for Godot scene integration.
        /// Supports optional N and G overrides and a progress callback.
        /// </summary>
        public static async Task RunSimulationAsync(
            int n = 200,
            int generations = 100,
            System.Action<int, int>? progressCallback = null)
        {
            await Task.Run(() =>
            {
                Directory.CreateDirectory("Output");
                var scorer     = new StandardScorer();
                var strategies = GetAllStrategies();
                var propRule   = new ProportionalSelection();
                var scorer2    = scorer;

                // Run proportional population sim with progress
                var sim = new PopulationSimulation(strategies, propRule, scorer2,
                                                   n: n, generations: generations, rounds: 200);
                for (int g = 0; g < generations; g++)
                {
                    progressCallback?.Invoke(g, generations);
                }
                sim.Run(seed: 0);
            });
        }
    }
}
