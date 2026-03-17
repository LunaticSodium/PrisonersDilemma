// MainScene.cs — Godot scene script for the main simulation UI.
// This file is excluded from the standalone dotnet build (see csproj).
// It is compiled by Godot when running with the Godot engine.
//
// When invoked headlessly with --run-tests, it delegates to BehaviourTests.
// When invoked headlessly without --run-tests, it runs the full simulation.
using System;
using System.Threading.Tasks;
using Godot;
using PrisonersDilemma.Tests;

namespace PrisonersDilemma.Scenes
{
    /// <summary>
    /// Root scene node. Handles both interactive UI and headless simulation/test modes.
    /// </summary>
    public partial class MainScene : Node
    {
        private Button? _runButton;
        private Button? _abortButton;
        private ProgressBar? _progressBar;
        private RichTextLabel? _resultPanel;
        private HSlider? _nSlider;
        private HSlider? _gSlider;
        private bool _running = false;

        /// <inheritdoc/>
        public override void _Ready()
        {
            // Retrieve UI nodes (may be null in headless/test mode)
            _runButton    = GetNodeOrNull<Button>("UI/VBox/ButtonRow/RunButton");
            _abortButton  = GetNodeOrNull<Button>("UI/VBox/ButtonRow/AbortButton");
            _progressBar  = GetNodeOrNull<ProgressBar>("UI/VBox/ProgressBar");
            _resultPanel  = GetNodeOrNull<RichTextLabel>("UI/VBox/ResultPanel");
            _nSlider      = GetNodeOrNull<HSlider>("UI/VBox/ParamPanel/NSlider");
            _gSlider      = GetNodeOrNull<HSlider>("UI/VBox/ParamPanel/GSlider");

            if (_runButton != null)
                _runButton.Pressed += OnRunPressed;
            if (_abortButton != null)
                _abortButton.Pressed += OnAbortPressed;

            // Check for headless mode arguments
            var userArgs = OS.GetCmdlineUserArgs();
            bool runTests = false;
            bool headlessSim = false;
            foreach (var arg in userArgs)
            {
                if (arg == "--run-tests") runTests = true;
                if (arg == "--headless-sim") headlessSim = true;
            }

            if (runTests)
            {
                GD.Print("=== Running headless tests ===");
                int result = BehaviourTests.RunAll();
                GD.Print($"Test result code: {result}");
                GetTree().Quit(result);
                return;
            }

            if (headlessSim || DisplayServer.GetName() == "headless")
            {
                GD.Print("=== Running headless simulation ===");
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await Program.RunSimulationAsync();
                        GD.Print("Headless simulation complete.");
                    }
                    catch (Exception ex)
                    {
                        GD.PrintErr($"Simulation error: {ex.Message}");
                    }
                    finally
                    {
                        CallDeferred("_QuitHeadless", 0);
                    }
                });
            }
        }

        /// <summary>Quit the engine (called from async context via CallDeferred).</summary>
        private void _QuitHeadless(int code)
        {
            GetTree().Quit(code);
        }

        private void OnRunPressed()
        {
            if (_running) return;
            _running = true;
            int n = (int)(_nSlider?.Value ?? 200);
            int g = (int)(_gSlider?.Value ?? 100);
            GD.Print($"Starting simulation: N={n}, G={g}");
            if (_resultPanel != null)
                _resultPanel.Text = "[b]Simulation running...[/b]";
            if (_progressBar != null)
                _progressBar.Value = 0;

            _ = Task.Run(async () =>
            {
                await Program.RunSimulationAsync(n: n, generations: g,
                    progressCallback: (gen, total) =>
                    {
                        CallDeferred(nameof(UpdateProgress), gen, total);
                    });
                CallDeferred(nameof(OnSimulationComplete));
            });
        }

        private void OnAbortPressed()
        {
            _running = false;
            GD.Print("Simulation aborted.");
            if (_resultPanel != null)
                _resultPanel.Text = "[color=red]Simulation aborted.[/color]";
        }

        /// <summary>Update progress bar (called on main thread).</summary>
        private void UpdateProgress(int gen, int total)
        {
            if (_progressBar != null)
                _progressBar.Value = (double)gen / total * 100.0;
        }

        /// <summary>Called when simulation completes.</summary>
        private void OnSimulationComplete()
        {
            _running = false;
            GD.Print("Simulation complete.");
            if (_resultPanel != null)
                _resultPanel.Text = "[b][color=green]Simulation complete! Check Output/ for results.[/color][/b]";
            if (_progressBar != null)
                _progressBar.Value = 100;
        }
    }
}
