using System.Collections.Generic;

namespace PrisonersDilemma.Interfaces
{
    /// <summary>
    /// Generates a written report summarising simulation parameters and findings.
    /// </summary>
    public interface IReporter
    {
        /// <summary>
        /// Generate a Markdown report and write it to the specified path.
        /// </summary>
        /// <param name="outputPath">File path for the report (e.g. Output/report.md).</param>
        /// <param name="simulationParams">Key-value metadata about the simulation run.</param>
        /// <param name="findings">Key findings from the simulation.</param>
        void GenerateReport(
            string outputPath,
            IReadOnlyDictionary<string, string> simulationParams,
            IReadOnlyDictionary<string, string> findings);
    }
}
