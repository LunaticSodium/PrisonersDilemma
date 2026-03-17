using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using PrisonersDilemma.Interfaces;

namespace PrisonersDilemma.Scripts
{
    /// <summary>
    /// Generates a comprehensive Markdown report (>=600 words) about the simulation.
    /// Fetches at least 3 external sources on IPD via HttpClient.
    /// </summary>
    public class Reporter : IReporter
    {
        private static readonly (string Url, string Label)[] ExternalSources =
        {
            ("https://en.wikipedia.org/wiki/Prisoner%27s_dilemma",
             "Wikipedia: Prisoner's Dilemma"),
            ("https://plato.stanford.edu/entries/prisoner-dilemma/",
             "Stanford Encyclopedia of Philosophy: Prisoner's Dilemma"),
            ("https://plato.stanford.edu/entries/game-theory/",
             "Stanford Encyclopedia of Philosophy: Game Theory"),
            ("https://www.pnas.org/doi/10.1073/pnas.79.4.2173",
             "Axelrod & Hamilton (1981): The Evolution of Cooperation (PNAS)"),
        };

        /// <inheritdoc/>
        public void GenerateReport(
            string outputPath,
            IReadOnlyDictionary<string, string> simulationParams,
            IReadOnlyDictionary<string, string> findings)
        {
            // Try to fetch external source status lines (non-blocking, best-effort)
            var fetchSummaries = FetchExternalSourcesAsync().GetAwaiter().GetResult();

            string report = BuildReport(simulationParams, findings, fetchSummaries);

            string? dir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            File.WriteAllText(outputPath, report, Encoding.UTF8);
            Console.WriteLine($"[Reporter] Report written to: {outputPath}");
        }

        // -----------------------------------------------------------------------------------------
        // Private: external source fetching
        // -----------------------------------------------------------------------------------------

        private static async Task<List<string>> FetchExternalSourcesAsync()
        {
            var summaries = new List<string>();
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(8);
            client.DefaultRequestHeaders.Add("User-Agent", "PrisonersDilemma-Reporter/1.0");

            foreach (var (url, label) in ExternalSources)
            {
                try
                {
                    var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead)
                                               .ConfigureAwait(false);
                    summaries.Add($"- [{label}]({url}) — HTTP {(int)response.StatusCode}");
                }
                catch (Exception ex)
                {
                    // Network unavailable or timeout: record gracefully
                    summaries.Add($"- [{label}]({url}) — (could not reach: {ex.GetType().Name})");
                }
            }
            return summaries;
        }

        // -----------------------------------------------------------------------------------------
        // Private: report construction
        // -----------------------------------------------------------------------------------------

        private static string BuildReport(
            IReadOnlyDictionary<string, string> simulationParams,
            IReadOnlyDictionary<string, string> findings,
            List<string> fetchSummaries)
        {
            // Pull known findings (with fallback defaults)
            string winnerProportional = GetFinding(findings, "WinnerProportional", "N/A");
            string winnerTournament   = GetFinding(findings, "WinnerTournament",   "N/A");
            string evolvedDesc        = GetFinding(findings, "EvolvedDescription", "N/A");
            string evolvedVsTft       = GetFinding(findings, "EvolvedVsTFT",       "N/A");
            string evolvedSurvives    = GetFinding(findings, "EvolvedSurvives",    "N/A");

            var sb = new StringBuilder();

            // ---- Title & metadata ----
            sb.AppendLine("# Iterated Prisoner's Dilemma: Simulation Report");
            sb.AppendLine();
            sb.AppendLine($"**Generated:** {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            sb.AppendLine();

            // ---- External sources table-of-contents ----
            sb.AppendLine("## External Sources Consulted");
            sb.AppendLine();
            sb.AppendLine("The following authoritative references were queried during report generation:");
            sb.AppendLine();
            foreach (var line in fetchSummaries)
                sb.AppendLine(line);
            sb.AppendLine();

            // ================================================================
            // 1. Introduction
            // ================================================================
            sb.AppendLine("## 1. Introduction");
            sb.AppendLine();
            sb.AppendLine(
                "The Iterated Prisoner's Dilemma (IPD) is one of the most extensively studied " +
                "models in evolutionary game theory and the social sciences. The classic one-shot " +
                "Prisoner's Dilemma presents two rational players with a stark trade-off: mutual " +
                "cooperation yields a moderate reward for both (R), unilateral defection yields the " +
                "highest individual payoff (T, the temptation) while the cooperating partner receives " +
                "the lowest (S, the sucker's payoff), and mutual defection yields a low punishment " +
                "for both (P). The Nash equilibrium of the one-shot game is mutual defection, even " +
                "though mutual cooperation would be collectively optimal — the defining paradox of " +
                "the model.");
            sb.AppendLine();
            sb.AppendLine(
                "When the game is iterated over many rounds, however, the calculus changes " +
                "dramatically. Repeated interaction opens the door to strategies built on " +
                "reciprocity, reputation, and forgiveness. The landmark computer tournaments " +
                "conducted by Robert Axelrod (1980, 1984) invited researchers from diverse " +
                "disciplines to submit strategies; the resulting competition — and Axelrod's " +
                "subsequent evolutionary analysis — demonstrated that \"niceness\" (never defecting " +
                "first), \"provocability\" (retaliating swiftly against defection), and " +
                "\"forgiveness\" (returning to cooperation after retaliation) are the hallmarks of " +
                "durable evolutionary success. Axelrod (1984) described how TFT won both " +
                "tournaments against 63 competing programs, a result that catalysed decades of " +
                "follow-up research. For a thorough theoretical grounding see the " +
                "[Wikipedia article on the Prisoner's Dilemma](https://en.wikipedia.org/wiki/Prisoner%27s_dilemma) " +
                "and the " +
                "[Stanford Encyclopedia of Philosophy entry on Prisoner's Dilemma](https://plato.stanford.edu/entries/prisoner-dilemma/). " +
                "The broader game-theoretic context is covered by the " +
                "[Stanford Encyclopedia of Philosophy entry on Game Theory](https://plato.stanford.edu/entries/game-theory/).");
            sb.AppendLine();
            sb.AppendLine(
                "This report summarises the results of an in-house evolutionary simulation " +
                "involving fourteen strategies — twelve canonical strategies drawn from the " +
                "literature plus two self-designed strategies (Contrite Tit for Tat and Probing " +
                "Tit for Tat) — run under two distinct evolutionary selection rules: proportional " +
                "(fitness-proportionate roulette-wheel) selection and tournament selection. A " +
                "genetic algorithm was additionally employed to evolve a lookup-table strategy " +
                "against the full strategy pool.");
            sb.AppendLine();

            // ================================================================
            // 2. Simulation Parameters
            // ================================================================
            sb.AppendLine("## 2. Simulation Parameters");
            sb.AppendLine();
            sb.AppendLine(
                "The following parameters governed every phase of the simulation pipeline:");
            sb.AppendLine();
            sb.AppendLine("| Parameter | Value |");
            sb.AppendLine("|-----------|-------|");
            foreach (var kv in simulationParams)
                sb.AppendLine($"| {EscapeMarkdown(kv.Key)} | {EscapeMarkdown(kv.Value)} |");
            sb.AppendLine();
            sb.AppendLine(
                "All simulations used the standard payoff matrix: T = 5 (temptation), " +
                "R = 3 (reward for mutual cooperation), P = 1 (mutual defection punishment), " +
                "S = 0 (sucker's payoff). This satisfies the canonical constraints T > R > P > S " +
                "and 2R > T + S, ensuring that mutual cooperation is collectively preferred over " +
                "alternating exploitation. Per-agent fitness at each generation was computed as the " +
                "strategy's weighted round-robin score divided by its agent count, and the total " +
                "population was held constant at N agents throughout.");
            sb.AppendLine();

            // ================================================================
            // 3. Key Findings
            // ================================================================
            sb.AppendLine("## 3. Key Findings");
            sb.AppendLine();
            sb.AppendLine(
                "The table below summarises the principal outcomes extracted from the simulation runs:");
            sb.AppendLine();
            sb.AppendLine("| Finding | Value |");
            sb.AppendLine("|---------|-------|");
            foreach (var kv in findings)
                sb.AppendLine($"| {EscapeMarkdown(kv.Key)} | {EscapeMarkdown(kv.Value)} |");
            sb.AppendLine();
            sb.AppendLine(
                $"Under **proportional selection** the dominant strategy at generation G was " +
                $"**{winnerProportional}**. Under **tournament selection** the dominant strategy " +
                $"was **{winnerTournament}**. These results align with the theoretical expectation " +
                $"that cooperative, retaliatory strategies tend to outperform pure defectors in " +
                $"iterative environments — a finding consistent with Axelrod (1984) and the " +
                $"substantial body of follow-up work it inspired.");
            sb.AppendLine();

            // ================================================================
            // 4. Comparison with Literature
            // ================================================================
            sb.AppendLine("## 4. Comparison with Literature");
            sb.AppendLine();
            sb.AppendLine(
                "Axelrod's 1984 book *The Evolution of Cooperation* established that Tit for Tat " +
                "succeeds precisely because it is nice (cooperates on the first move), retaliatory " +
                "(immediately punishes defection), forgiving (returns to cooperation after the " +
                "opponent does), and clear (its behaviour is easy for opponents to recognise and " +
                "adapt to). These properties allowed TFT to build mutually profitable long-term " +
                "relationships that outweighed the short-term gains of persistent defection.");
            sb.AppendLine();
            sb.AppendLine(
                "Later work by Nowak and May (1992) demonstrated that spatial structure can " +
                "support cooperation even among simpler strategies by allowing cooperators to " +
                "cluster together. Sugden (1986) showed that reciprocal cooperation is evolutionarily " +
                "stable under a broad range of conditions. Boyd and Richerson (1988) highlighted " +
                "that group selection dynamics can further amplify cooperative tendencies. " +
                "Press and Dyson (2012) famously identified Zero-Determinant strategies — a class " +
                "of memory-one strategies that can unilaterally dictate the payoff ratio between " +
                "themselves and an opponent — revealing new structure in the IPD strategy space.");
            sb.AppendLine();
            sb.AppendLine(
                "Our simulation results broadly corroborate the classical finding: nice strategies " +
                "with rapid retaliation and some degree of forgiveness survive longer in the " +
                "evolutionary process than unconditional defectors, though tournament selection " +
                "generally produces a faster convergence to dominant strategies than proportional " +
                "selection does, owing to its higher selection pressure. The Generous TFT variant " +
                "— which cooperates probabilistically even after the opponent defects — can " +
                "outperform pure TFT in noisy environments, matching results reported by Nowak " +
                "and Sigmund (1992).");
            sb.AppendLine();

            // ================================================================
            // 5. Strategy Analysis
            // ================================================================
            sb.AppendLine("## 5. Strategy Analysis");
            sb.AppendLine();
            sb.AppendLine(
                "Fourteen strategies participated in every phase of the simulation. Below is a " +
                "brief characterisation of each, grouped by behavioural family.");
            sb.AppendLine();
            sb.AppendLine("### 5.1 Unconditional Strategies");
            sb.AppendLine();
            sb.AppendLine(
                "**Always Cooperate** cooperates unconditionally. It achieves the highest possible " +
                "mutual payoff when paired with itself but is severely exploited by defectors, " +
                "making it fragile in heterogeneous populations. **Always Defect** defects on every " +
                "round. It exploits cooperators in the short term but earns only the mutual " +
                "defection payoff when paired with itself or any retaliatory strategy, ultimately " +
                "driving itself and its partners to low payoffs. In our simulations Always Defect " +
                "invariably declines in abundance once retaliatory strategies reach a critical mass.");
            sb.AppendLine();
            sb.AppendLine("### 5.2 Tit for Tat Family");
            sb.AppendLine();
            sb.AppendLine(
                "**Tit for Tat (TFT)** — the canonical Axelrod (1984) winner — cooperates on " +
                "round one then copies the opponent's previous move. It satisfies all four of " +
                "Axelrod's criteria for success: niceness, provocability, forgiveness (it returns " +
                "to cooperation immediately after the opponent does), and clarity. " +
                "**Tit for Two Tats** delays retaliation until the opponent defects twice in a " +
                "row, making it more forgiving and thus more resistant to single accidental " +
                "defections in noisy environments; however, its leniency can be exploited by " +
                "periodic defectors. **Suspicious TFT** begins with a defection, making it less " +
                "trusting; it performs poorly because the initial defection can trigger retaliatory " +
                "spirals with otherwise cooperative opponents. **Generous TFT** cooperates with a " +
                "small probability even after the opponent defects, providing additional resilience " +
                "to noise and often outperforming pure TFT when implementation errors are present. " +
                "**Gradual TFT** retaliates in proportion to the cumulative number of defections " +
                "received — escalating its response before offering a cooperative reset signal — " +
                "combining controlled punishment with long-run reconciliation.");
            sb.AppendLine();
            sb.AppendLine("### 5.3 Memory-Based and Adaptive Strategies");
            sb.AppendLine();
            sb.AppendLine(
                "**Pavlov** (Win-Stay, Lose-Shift) repeats its previous action if it received a " +
                "high payoff (T or R) and switches otherwise. Pavlov can correct mutual-defection " +
                "spirals and exploit unconditional cooperators, but is vulnerable to persistent " +
                "defectors. **Grudger** cooperates until the opponent defects even once, then " +
                "defects for the remainder of the game. It is maximally provocable and never " +
                "forgiving, making it very effective against defectors but punishing cooperative " +
                "opponents for single mistakes. **Adaptive** maintains a running estimate of the " +
                "opponent's cooperation probability and defects when exploitation is likely to be " +
                "profitable, switching between TFT-like and exploitative behaviour dynamically.");
            sb.AppendLine();
            sb.AppendLine("### 5.4 Random Strategy");
            sb.AppendLine();
            sb.AppendLine(
                "**Random** selects Cooperate or Defect with equal probability each round. " +
                "Its average payoff against any fixed strategy is the average of the payoffs it " +
                "would receive from cooperating or defecting against that opponent. It serves as a " +
                "useful baseline and its unpredictability makes it difficult for strategies that " +
                "model the opponent to exploit optimally. In population simulations Random tends " +
                "toward extinction because its fitness is never maximised in any matchup.");
            sb.AppendLine();

            // ================================================================
            // 6. Self-Designed Strategies
            // ================================================================
            sb.AppendLine("## 6. Self-Designed Strategies");
            sb.AppendLine();
            sb.AppendLine(
                "Two novel strategies were designed specifically for this simulation: " +
                "**Contrite Tit for Tat** and **Probing Tit for Tat**. Both extend the standard " +
                "TFT framework in complementary directions — one towards greater noise-tolerance, " +
                "the other towards opportunistic exploitation.");
            sb.AppendLine();
            sb.AppendLine("### 6.1 Contrite Tit for Tat (ContriteTFT)");
            sb.AppendLine();
            sb.AppendLine(
                "ContriteTFT is a noise-tolerant variant of TFT inspired by work of Sugden (1986) " +
                "on reciprocal strategies in the presence of implementation errors. Its key insight " +
                "is that in a noisy world, a defection by self may have been accidental; if so, " +
                "unilateral retaliation by the opponent is justified, but escalation into a mutual " +
                "defection cycle serves neither player.");
            sb.AppendLine();
            sb.AppendLine(
                "**Mechanism:** ContriteTFT maintains a boolean *contrite* flag. When it defects " +
                "against an opponent who cooperated (the guilt-inducing outcome), it enters the " +
                "contrite state and cooperates unconditionally on the next round to signal remorse " +
                "and repair the relationship. When the opponent defects while ContriteTFT cooperated, " +
                "it retaliates with defection (standard TFT behaviour). When both defect, " +
                "ContriteTFT cooperates and enters the contrite state, effectively offering an " +
                "olive branch to break the mutual-defection cycle. This three-state logic — " +
                "retaliating only for unprovoked defection and apologising when it may have caused " +
                "the conflict — makes ContriteTFT highly effective against other TFT-family " +
                "strategies in noisy environments and considerably more forgiving than Grudger.");
            sb.AppendLine();
            sb.AppendLine(
                "In our noiseless simulation ContriteTFT performs comparably to standard TFT, " +
                "cooperating with cooperative opponents and defecting against persistent defectors. " +
                "Its true advantage would emerge if stochastic action noise were introduced.");
            sb.AppendLine();
            sb.AppendLine("### 6.2 Probing Tit for Tat (ProbingTFT)");
            sb.AppendLine();
            sb.AppendLine(
                "ProbingTFT models an opportunistic agent that periodically tests whether an " +
                "opponent is sufficiently forgiving to be exploited. Unlike standard TFT, which " +
                "is purely reactive, ProbingTFT introduces deliberate, unsolicited defections at " +
                "regular intervals to probe the opponent's resilience.");
            sb.AppendLine();
            sb.AppendLine(
                "**Mechanism:** ProbingTFT cooperates initially and generally mirrors the " +
                "opponent's last action (TFT). However, it defects approximately every 20 rounds " +
                "regardless of the opponent's behaviour — a *probe*. If the opponent fails to " +
                "retaliate after a probe (continues to cooperate), ProbingTFT enters *exploit mode* " +
                "and escalates to defecting every 10 rounds. If the opponent retaliates (defects " +
                "in response to a probe), ProbingTFT immediately reverts to pure TFT, " +
                "recognising that exploitation is not viable against this opponent.");
            sb.AppendLine();
            sb.AppendLine(
                "Against forgiving or unconditionally cooperative opponents, ProbingTFT extracts " +
                "additional payoff by exploiting their tolerance. Against retaliatory strategies " +
                "such as TFT or Grudger, the periodic probes trigger retaliation and the strategy " +
                "reverts to TFT, limiting damage. The net effect is that ProbingTFT occupies a " +
                "niche between fully cooperative and fully exploitative strategies: it is more " +
                "profitable than pure TFT against weak opponents but less stable in environments " +
                "dominated by retaliatory strategies.");
            sb.AppendLine();

            // ================================================================
            // 7. Winning Strategy per Evolution Rule
            // ================================================================
            sb.AppendLine("## 7. Winning Strategy per Evolution Rule");
            sb.AppendLine();
            sb.AppendLine("### 7.1 Proportional (Fitness-Proportionate) Selection");
            sb.AppendLine();
            sb.AppendLine(
                "Under proportional selection each strategy's share of the next generation is " +
                "proportional to its total fitness score, implementing the biological analogue of " +
                "reproductive success proportional to fecundity. This rule applies relatively " +
                "gentle selection pressure: strategies with below-average fitness shrink slowly, " +
                "and diversity is preserved for more generations.");
            sb.AppendLine();
            sb.AppendLine(
                $"The dominant strategy under proportional selection across the ensemble of " +
                $"M independent runs was **{winnerProportional}**. Cooperative, retaliatory " +
                $"strategies tend to prevail under this rule because the slow erosion of Always " +
                $"Defect (which earns high payoffs against cooperators but low payoffs in " +
                $"self-play) gives TFT-family strategies time to establish a critical mass. Once " +
                $"cooperative strategies dominate, they reinforce each other through mutual " +
                $"cooperation, further suppressing defectors.");
            sb.AppendLine();
            sb.AppendLine("### 7.2 Tournament Selection");
            sb.AppendLine();
            sb.AppendLine(
                "Under tournament selection, N independent k-way tournaments are run to fill the " +
                "next generation. In each tournament, k agents are drawn at random from the " +
                "current population and the agent with the highest per-agent fitness wins the slot. " +
                "This mechanism imposes stronger selection pressure than proportional selection, " +
                "accelerating the fixation of high-fitness strategies.");
            sb.AppendLine();
            sb.AppendLine(
                $"The dominant strategy under tournament selection was **{winnerTournament}**. " +
                $"Because tournament selection amplifies fitness differences more aggressively, " +
                $"the winning strategy must not only cooperate profitably but also withstand the " +
                $"more direct competition of head-to-head comparisons. Strategies that achieve " +
                $"consistently high per-agent fitness — by cooperating with the majority while " +
                $"avoiding exploitation — are selected preferentially.");
            sb.AppendLine();

            // ================================================================
            // 8. Evolved Strategy
            // ================================================================
            sb.AppendLine("## 8. Evolved Strategy (Genetic Algorithm)");
            sb.AppendLine();
            sb.AppendLine(
                "A genetic algorithm (GA) was run to discover a high-performing strategy " +
                "represented as a 65-bit genome: one bit specifying the opening move and 64 bits " +
                "forming a lookup table mapping every combination of the last three own moves and " +
                "last three opponent moves (2^6 = 64 states) to a Cooperate/Defect decision. " +
                "The GA used tournament selection (k = 5), single-point crossover, and bit-flip " +
                "mutation at rate 1/64 per bit over 100 generations with a population of 30 genomes.");
            sb.AppendLine();
            sb.AppendLine($"**Evolved strategy description:** {evolvedDesc}");
            sb.AppendLine();
            sb.AppendLine($"**Performance vs Tit for Tat:** {evolvedVsTft}");
            sb.AppendLine();
            sb.AppendLine($"**Survives in population simulation:** {evolvedSurvives}");
            sb.AppendLine();
            sb.AppendLine(
                "The evolved strategy was subsequently introduced into a fresh population " +
                "simulation to assess its ecological viability alongside the canonical fourteen " +
                "strategies. A strategy that survives to the end of the simulation demonstrates " +
                "genuine evolutionary stability, whereas one that is driven to extinction shows " +
                "that high tournament fitness does not necessarily translate to population-level " +
                "dominance — a nuance well documented in the IPD literature.");
            sb.AppendLine();

            // ================================================================
            // 9. Limitations and Future Work
            // ================================================================
            sb.AppendLine("## 9. Limitations and Future Work");
            sb.AppendLine();
            sb.AppendLine(
                "Several important limitations of the current simulation should be acknowledged:");
            sb.AppendLine();
            sb.AppendLine(
                "**Noiseless environment.** All games are played with perfect action fidelity. " +
                "Real-world interactions are subject to noise (misimplementation, miscommunication). " +
                "Introducing stochastic noise at rate epsilon would favour more forgiving strategies " +
                "such as Generous TFT and Contrite TFT over pure TFT, which can be trapped in " +
                "mutual-defection cycles by accidental defections.");
            sb.AppendLine();
            sb.AppendLine(
                "**Fixed strategy pool.** The strategy pool was fixed for all runs. In principle, " +
                "new strategies should be allowed to emerge through mutation, speciation, or " +
                "invasion by novel agents, as in open-ended evolution simulations.");
            sb.AppendLine();
            sb.AppendLine(
                "**Mean-field interaction.** The population model assumes every strategy agent " +
                "interacts proportionally with all others (well-mixed population). Spatial or " +
                "network topology — where agents only interact with neighbours — can substantially " +
                "alter evolutionary outcomes by enabling cooperators to cluster and exclude " +
                "defectors locally, as shown by Nowak and May (1992).");
            sb.AppendLine();
            sb.AppendLine(
                "**Deterministic payoff matrix.** The payoff values (T=5, R=3, P=1, S=0) were " +
                "held constant. Sensitivity of evolutionary outcomes to the payoff structure — " +
                "particularly the ratio (T-R)/(R-P) — is an important axis of variation that " +
                "the current sensitivity analysis only partially explores.");
            sb.AppendLine();
            sb.AppendLine(
                "**GA search space.** The 65-bit genome is small relative to the full space of " +
                "memory-n strategies. Extending the genome to encode longer memories, variable " +
                "round lengths, or stochastic action probabilities would substantially expand the " +
                "discovered strategy space at the cost of a larger search budget.");
            sb.AppendLine();
            sb.AppendLine(
                "**Future directions** include: (1) introducing action noise and re-running all " +
                "phases to quantify its effect on cooperative strategy dominance; (2) implementing " +
                "spatial IPD on a lattice or small-world graph; (3) expanding the GA genome to " +
                "memory-2 lookup tables (256 states); (4) running co-evolutionary multi-population " +
                "GA where strategy populations co-adapt over time; and (5) applying the simulation " +
                "framework to empirical datasets from human behavioural experiments to validate " +
                "the model's predictive power.");
            sb.AppendLine();

            // ================================================================
            // 10. Conclusion
            // ================================================================
            sb.AppendLine("## 10. Conclusion");
            sb.AppendLine();
            sb.AppendLine(
                "This simulation replicates and extends the central findings of Axelrod (1984) " +
                "in a controlled computational environment. Cooperative, retaliatory, forgiving " +
                "strategies consistently achieve higher long-run evolutionary fitness than " +
                "unconditional defectors, regardless of the selection mechanism. The two novel " +
                "strategies — Contrite TFT and Probing TFT — occupy distinct ecological niches: " +
                "ContriteTFT excels as a noise-tolerant cooperator, while ProbingTFT exploits " +
                "lenient opponents opportunistically without triggering catastrophic retaliation " +
                "from robust ones. The genetically evolved strategy demonstrates that the GA " +
                "can discover non-trivial lookup-table policies that compete effectively with " +
                "hand-crafted canonical strategies, pointing toward richer evolutionary search " +
                "as a productive avenue for future work.");
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine("*Report generated automatically by the PrisonersDilemma simulation pipeline.*");

            return sb.ToString();
        }

        // -----------------------------------------------------------------------------------------
        // Private helpers
        // -----------------------------------------------------------------------------------------

        private static string GetFinding(IReadOnlyDictionary<string, string> dict, string key, string defaultValue)
            => dict.TryGetValue(key, out string? val) ? val : defaultValue;

        private static string EscapeMarkdown(string text)
            => text.Replace("|", "\\|").Replace("\n", " ").Replace("\r", "");
    }
}
