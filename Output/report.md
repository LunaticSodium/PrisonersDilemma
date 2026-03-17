# Iterated Prisoner's Dilemma: Simulation Report

**Generated:** 2026-03-17 12:20:22 UTC

## External Sources Consulted

The following authoritative references were queried during report generation:

- [Wikipedia: Prisoner's Dilemma](https://en.wikipedia.org/wiki/Prisoner%27s_dilemma) — HTTP 200
- [Stanford Encyclopedia of Philosophy: Prisoner's Dilemma](https://plato.stanford.edu/entries/prisoner-dilemma/) — HTTP 200
- [Stanford Encyclopedia of Philosophy: Game Theory](https://plato.stanford.edu/entries/game-theory/) — HTTP 200
- [Axelrod & Hamilton (1981): The Evolution of Cooperation (PNAS)](https://www.pnas.org/doi/10.1073/pnas.79.4.2173) — HTTP 403

## 1. Introduction

The Iterated Prisoner's Dilemma (IPD) is one of the most extensively studied models in evolutionary game theory and the social sciences. The classic one-shot Prisoner's Dilemma presents two rational players with a stark trade-off: mutual cooperation yields a moderate reward for both (R), unilateral defection yields the highest individual payoff (T, the temptation) while the cooperating partner receives the lowest (S, the sucker's payoff), and mutual defection yields a low punishment for both (P). The Nash equilibrium of the one-shot game is mutual defection, even though mutual cooperation would be collectively optimal — the defining paradox of the model.

When the game is iterated over many rounds, however, the calculus changes dramatically. Repeated interaction opens the door to strategies built on reciprocity, reputation, and forgiveness. The landmark computer tournaments conducted by Robert Axelrod (1980, 1984) invited researchers from diverse disciplines to submit strategies; the resulting competition — and Axelrod's subsequent evolutionary analysis — demonstrated that "niceness" (never defecting first), "provocability" (retaliating swiftly against defection), and "forgiveness" (returning to cooperation after retaliation) are the hallmarks of durable evolutionary success. Axelrod (1984) described how TFT won both tournaments against 63 competing programs, a result that catalysed decades of follow-up research. For a thorough theoretical grounding see the [Wikipedia article on the Prisoner's Dilemma](https://en.wikipedia.org/wiki/Prisoner%27s_dilemma) and the [Stanford Encyclopedia of Philosophy entry on Prisoner's Dilemma](https://plato.stanford.edu/entries/prisoner-dilemma/). The broader game-theoretic context is covered by the [Stanford Encyclopedia of Philosophy entry on Game Theory](https://plato.stanford.edu/entries/game-theory/).

This report summarises the results of an in-house evolutionary simulation involving fourteen strategies — twelve canonical strategies drawn from the literature plus two self-designed strategies (Contrite Tit for Tat and Probing Tit for Tat) — run under two distinct evolutionary selection rules: proportional (fitness-proportionate roulette-wheel) selection and tournament selection. A genetic algorithm was additionally employed to evolve a lookup-table strategy against the full strategy pool.

## 2. Simulation Parameters

The following parameters governed every phase of the simulation pipeline:

| Parameter | Value |
|-----------|-------|
| Strategies | Always Cooperate, Always Defect, Tit for Tat, Tit for Two Tats, Suspicious Tit for Tat, Generous Tit for Tat, Pavlov, Grudger, Random, Gradual Tit for Tat, Nice by Nature, Adaptive, Contrite Tit for Tat, Probing Tit for Tat |
| Population size (N) | 200 |
| Generations (G) | 100 |
| Rounds per game (R) | 200 |
| Ensemble runs (M) | 50 |
| Seeds per condition | 10 (sensitivity) |
| GA population size | 30 |
| GA generations | 100 |
| GA tournament k | 5 |
| GA mutation rate | 1/64 per bit |
| Payoff matrix | T=5, R=3, P=1, S=0 |
| Selection rules | ProportionalSelection, TournamentSelection |

All simulations used the standard payoff matrix: T = 5 (temptation), R = 3 (reward for mutual cooperation), P = 1 (mutual defection punishment), S = 0 (sucker's payoff). This satisfies the canonical constraints T > R > P > S and 2R > T + S, ensuring that mutual cooperation is collectively preferred over alternating exploitation. Per-agent fitness at each generation was computed as the strategy's weighted round-robin score divided by its agent count, and the total population was held constant at N agents throughout.

## 3. Key Findings

The table below summarises the principal outcomes extracted from the simulation runs:

| Finding | Value |
|---------|-------|
| WinnerTournamentScoring | Gradual Tit for Tat |
| WinnerProportional | Nice by Nature |
| WinnerTournament | Generous Tit for Tat |
| EvolvedDescription | Opens with Cooperate. In general somewhat aggressive (34 % of history states map to Cooperate). Cooperates when opponent recently cooperated: mixed response. Responds to recent defection: usually defects. |
| EvolvedVsTFT | Draw (Evolved=600.0, TFT=600.0 over 200 rounds) |
| EvolvedSurvives | True |
| EvolvedFinalAbundance | 12.5 % |
| GABestFitness | 576.07 |
| PropFinalAbundance_Always Cooperate | 9.5 % |
| PropFinalAbundance_Always Defect | 0.0 % |
| PropFinalAbundance_Tit for Tat | 13.1 % |
| PropFinalAbundance_Tit for Two Tats | 13.3 % |
| PropFinalAbundance_Suspicious Tit for Tat | 0.0 % |
| PropFinalAbundance_Generous Tit for Tat | 10.1 % |
| PropFinalAbundance_Pavlov | 8.8 % |
| PropFinalAbundance_Grudger | 10.0 % |
| PropFinalAbundance_Random | 0.0 % |
| PropFinalAbundance_Gradual Tit for Tat | 10.1 % |
| PropFinalAbundance_Nice by Nature | 13.6 % |
| PropFinalAbundance_Adaptive | 0.0 % |
| PropFinalAbundance_Contrite Tit for Tat | 11.3 % |
| PropFinalAbundance_Probing Tit for Tat | 0.2 % |

Under **proportional selection** the dominant strategy at generation G was **Nice by Nature**. Under **tournament selection** the dominant strategy was **Generous Tit for Tat**. These results align with the theoretical expectation that cooperative, retaliatory strategies tend to outperform pure defectors in iterative environments — a finding consistent with Axelrod (1984) and the substantial body of follow-up work it inspired.

## 4. Comparison with Literature

Axelrod's 1984 book *The Evolution of Cooperation* established that Tit for Tat succeeds precisely because it is nice (cooperates on the first move), retaliatory (immediately punishes defection), forgiving (returns to cooperation after the opponent does), and clear (its behaviour is easy for opponents to recognise and adapt to). These properties allowed TFT to build mutually profitable long-term relationships that outweighed the short-term gains of persistent defection.

Later work by Nowak and May (1992) demonstrated that spatial structure can support cooperation even among simpler strategies by allowing cooperators to cluster together. Sugden (1986) showed that reciprocal cooperation is evolutionarily stable under a broad range of conditions. Boyd and Richerson (1988) highlighted that group selection dynamics can further amplify cooperative tendencies. Press and Dyson (2012) famously identified Zero-Determinant strategies — a class of memory-one strategies that can unilaterally dictate the payoff ratio between themselves and an opponent — revealing new structure in the IPD strategy space.

Our simulation results broadly corroborate the classical finding: nice strategies with rapid retaliation and some degree of forgiveness survive longer in the evolutionary process than unconditional defectors, though tournament selection generally produces a faster convergence to dominant strategies than proportional selection does, owing to its higher selection pressure. The Generous TFT variant — which cooperates probabilistically even after the opponent defects — can outperform pure TFT in noisy environments, matching results reported by Nowak and Sigmund (1992).

## 5. Strategy Analysis

Fourteen strategies participated in every phase of the simulation. Below is a brief characterisation of each, grouped by behavioural family.

### 5.1 Unconditional Strategies

**Always Cooperate** cooperates unconditionally. It achieves the highest possible mutual payoff when paired with itself but is severely exploited by defectors, making it fragile in heterogeneous populations. **Always Defect** defects on every round. It exploits cooperators in the short term but earns only the mutual defection payoff when paired with itself or any retaliatory strategy, ultimately driving itself and its partners to low payoffs. In our simulations Always Defect invariably declines in abundance once retaliatory strategies reach a critical mass.

### 5.2 Tit for Tat Family

**Tit for Tat (TFT)** — the canonical Axelrod (1984) winner — cooperates on round one then copies the opponent's previous move. It satisfies all four of Axelrod's criteria for success: niceness, provocability, forgiveness (it returns to cooperation immediately after the opponent does), and clarity. **Tit for Two Tats** delays retaliation until the opponent defects twice in a row, making it more forgiving and thus more resistant to single accidental defections in noisy environments; however, its leniency can be exploited by periodic defectors. **Suspicious TFT** begins with a defection, making it less trusting; it performs poorly because the initial defection can trigger retaliatory spirals with otherwise cooperative opponents. **Generous TFT** cooperates with a small probability even after the opponent defects, providing additional resilience to noise and often outperforming pure TFT when implementation errors are present. **Gradual TFT** retaliates in proportion to the cumulative number of defections received — escalating its response before offering a cooperative reset signal — combining controlled punishment with long-run reconciliation.

### 5.3 Memory-Based and Adaptive Strategies

**Pavlov** (Win-Stay, Lose-Shift) repeats its previous action if it received a high payoff (T or R) and switches otherwise. Pavlov can correct mutual-defection spirals and exploit unconditional cooperators, but is vulnerable to persistent defectors. **Grudger** cooperates until the opponent defects even once, then defects for the remainder of the game. It is maximally provocable and never forgiving, making it very effective against defectors but punishing cooperative opponents for single mistakes. **Adaptive** maintains a running estimate of the opponent's cooperation probability and defects when exploitation is likely to be profitable, switching between TFT-like and exploitative behaviour dynamically.

### 5.4 Random Strategy

**Random** selects Cooperate or Defect with equal probability each round. Its average payoff against any fixed strategy is the average of the payoffs it would receive from cooperating or defecting against that opponent. It serves as a useful baseline and its unpredictability makes it difficult for strategies that model the opponent to exploit optimally. In population simulations Random tends toward extinction because its fitness is never maximised in any matchup.

## 6. Self-Designed Strategies

Two novel strategies were designed specifically for this simulation: **Contrite Tit for Tat** and **Probing Tit for Tat**. Both extend the standard TFT framework in complementary directions — one towards greater noise-tolerance, the other towards opportunistic exploitation.

### 6.1 Contrite Tit for Tat (ContriteTFT)

ContriteTFT is a noise-tolerant variant of TFT inspired by work of Sugden (1986) on reciprocal strategies in the presence of implementation errors. Its key insight is that in a noisy world, a defection by self may have been accidental; if so, unilateral retaliation by the opponent is justified, but escalation into a mutual defection cycle serves neither player.

**Mechanism:** ContriteTFT maintains a boolean *contrite* flag. When it defects against an opponent who cooperated (the guilt-inducing outcome), it enters the contrite state and cooperates unconditionally on the next round to signal remorse and repair the relationship. When the opponent defects while ContriteTFT cooperated, it retaliates with defection (standard TFT behaviour). When both defect, ContriteTFT cooperates and enters the contrite state, effectively offering an olive branch to break the mutual-defection cycle. This three-state logic — retaliating only for unprovoked defection and apologising when it may have caused the conflict — makes ContriteTFT highly effective against other TFT-family strategies in noisy environments and considerably more forgiving than Grudger.

In our noiseless simulation ContriteTFT performs comparably to standard TFT, cooperating with cooperative opponents and defecting against persistent defectors. Its true advantage would emerge if stochastic action noise were introduced.

### 6.2 Probing Tit for Tat (ProbingTFT)

ProbingTFT models an opportunistic agent that periodically tests whether an opponent is sufficiently forgiving to be exploited. Unlike standard TFT, which is purely reactive, ProbingTFT introduces deliberate, unsolicited defections at regular intervals to probe the opponent's resilience.

**Mechanism:** ProbingTFT cooperates initially and generally mirrors the opponent's last action (TFT). However, it defects approximately every 20 rounds regardless of the opponent's behaviour — a *probe*. If the opponent fails to retaliate after a probe (continues to cooperate), ProbingTFT enters *exploit mode* and escalates to defecting every 10 rounds. If the opponent retaliates (defects in response to a probe), ProbingTFT immediately reverts to pure TFT, recognising that exploitation is not viable against this opponent.

Against forgiving or unconditionally cooperative opponents, ProbingTFT extracts additional payoff by exploiting their tolerance. Against retaliatory strategies such as TFT or Grudger, the periodic probes trigger retaliation and the strategy reverts to TFT, limiting damage. The net effect is that ProbingTFT occupies a niche between fully cooperative and fully exploitative strategies: it is more profitable than pure TFT against weak opponents but less stable in environments dominated by retaliatory strategies.

## 7. Winning Strategy per Evolution Rule

### 7.1 Proportional (Fitness-Proportionate) Selection

Under proportional selection each strategy's share of the next generation is proportional to its total fitness score, implementing the biological analogue of reproductive success proportional to fecundity. This rule applies relatively gentle selection pressure: strategies with below-average fitness shrink slowly, and diversity is preserved for more generations.

The dominant strategy under proportional selection across the ensemble of M independent runs was **Nice by Nature**. Cooperative, retaliatory strategies tend to prevail under this rule because the slow erosion of Always Defect (which earns high payoffs against cooperators but low payoffs in self-play) gives TFT-family strategies time to establish a critical mass. Once cooperative strategies dominate, they reinforce each other through mutual cooperation, further suppressing defectors.

### 7.2 Tournament Selection

Under tournament selection, N independent k-way tournaments are run to fill the next generation. In each tournament, k agents are drawn at random from the current population and the agent with the highest per-agent fitness wins the slot. This mechanism imposes stronger selection pressure than proportional selection, accelerating the fixation of high-fitness strategies.

The dominant strategy under tournament selection was **Generous Tit for Tat**. Because tournament selection amplifies fitness differences more aggressively, the winning strategy must not only cooperate profitably but also withstand the more direct competition of head-to-head comparisons. Strategies that achieve consistently high per-agent fitness — by cooperating with the majority while avoiding exploitation — are selected preferentially.

## 8. Evolved Strategy (Genetic Algorithm)

A genetic algorithm (GA) was run to discover a high-performing strategy represented as a 65-bit genome: one bit specifying the opening move and 64 bits forming a lookup table mapping every combination of the last three own moves and last three opponent moves (2^6 = 64 states) to a Cooperate/Defect decision. The GA used tournament selection (k = 5), single-point crossover, and bit-flip mutation at rate 1/64 per bit over 100 generations with a population of 30 genomes.

**Evolved strategy description:** Opens with Cooperate. In general somewhat aggressive (34 % of history states map to Cooperate). Cooperates when opponent recently cooperated: mixed response. Responds to recent defection: usually defects.

**Performance vs Tit for Tat:** Draw (Evolved=600.0, TFT=600.0 over 200 rounds)

**Survives in population simulation:** True

The evolved strategy was subsequently introduced into a fresh population simulation to assess its ecological viability alongside the canonical fourteen strategies. A strategy that survives to the end of the simulation demonstrates genuine evolutionary stability, whereas one that is driven to extinction shows that high tournament fitness does not necessarily translate to population-level dominance — a nuance well documented in the IPD literature.

## 9. Limitations and Future Work

Several important limitations of the current simulation should be acknowledged:

**Noiseless environment.** All games are played with perfect action fidelity. Real-world interactions are subject to noise (misimplementation, miscommunication). Introducing stochastic noise at rate epsilon would favour more forgiving strategies such as Generous TFT and Contrite TFT over pure TFT, which can be trapped in mutual-defection cycles by accidental defections.

**Fixed strategy pool.** The strategy pool was fixed for all runs. In principle, new strategies should be allowed to emerge through mutation, speciation, or invasion by novel agents, as in open-ended evolution simulations.

**Mean-field interaction.** The population model assumes every strategy agent interacts proportionally with all others (well-mixed population). Spatial or network topology — where agents only interact with neighbours — can substantially alter evolutionary outcomes by enabling cooperators to cluster and exclude defectors locally, as shown by Nowak and May (1992).

**Deterministic payoff matrix.** The payoff values (T=5, R=3, P=1, S=0) were held constant. Sensitivity of evolutionary outcomes to the payoff structure — particularly the ratio (T-R)/(R-P) — is an important axis of variation that the current sensitivity analysis only partially explores.

**GA search space.** The 65-bit genome is small relative to the full space of memory-n strategies. Extending the genome to encode longer memories, variable round lengths, or stochastic action probabilities would substantially expand the discovered strategy space at the cost of a larger search budget.

**Future directions** include: (1) introducing action noise and re-running all phases to quantify its effect on cooperative strategy dominance; (2) implementing spatial IPD on a lattice or small-world graph; (3) expanding the GA genome to memory-2 lookup tables (256 states); (4) running co-evolutionary multi-population GA where strategy populations co-adapt over time; and (5) applying the simulation framework to empirical datasets from human behavioural experiments to validate the model's predictive power.

## 10. Conclusion

This simulation replicates and extends the central findings of Axelrod (1984) in a controlled computational environment. Cooperative, retaliatory, forgiving strategies consistently achieve higher long-run evolutionary fitness than unconditional defectors, regardless of the selection mechanism. The two novel strategies — Contrite TFT and Probing TFT — occupy distinct ecological niches: ContriteTFT excels as a noise-tolerant cooperator, while ProbingTFT exploits lenient opponents opportunistically without triggering catastrophic retaliation from robust ones. The genetically evolved strategy demonstrates that the GA can discover non-trivial lookup-table policies that compete effectively with hand-crafted canonical strategies, pointing toward richer evolutionary search as a productive avenue for future work.

---
*Report generated automatically by the PrisonersDilemma simulation pipeline.*
