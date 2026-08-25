using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace DeadSignal.Diagnostics
{
    public enum DebugRoutePreset
    {
        OpeningLoop,
        RequiredSalvage,
        ThreeTowerRun,
        EasternRoom,
        FullExtraction,
        RecordedRoute
    }

    public enum DebugAutomationMode
    {
        AssistedPlaythrough,
        DeterministicValidation
    }

    public enum DebugAutomationProfile
    {
        LiveBalance,
        SafeNavigation,
        CombatValidation
    }

    public enum DebugRouteAction
    {
        None,
        ActivateCentralTower,
        CollectCache,
        SelectPrimaryOverclock,
        ActivateRelayTower,
        SelectWeaponOverclock,
        ActivateSpineTower,
        BeginStableExtraction,
        CaptureScreenshot
    }

    public enum DebugRouteAssertion
    {
        None,
        SignalAboveTwenty,
        CameraContainsPlayer,
        InteractionInRange,
        ObjectiveAdvanced
    }

    public enum DebugRouteRunState
    {
        Idle,
        Navigating,
        Verifying,
        Paused,
        Completed,
        Failed
    }

    public sealed class DebugRouteStep
    {
        public DebugRouteStep(string name, DebugLocation location, float arrivalRadius, DebugRouteAction action,
            DebugRouteAssertion assertion = DebugRouteAssertion.None)
        {
            Name = name;
            Location = location;
            ArrivalRadius = arrivalRadius;
            Action = action;
            Assertion = assertion;
        }

        public DebugRouteStep(string name, Vector3 position)
        {
            Name = name;
            CustomPosition = position;
            UsesCustomPosition = true;
            ArrivalRadius = 0.65f;
        }

        public string Name { get; }
        public DebugLocation Location { get; }
        public Vector3 CustomPosition { get; }
        public bool UsesCustomPosition { get; }
        public float ArrivalRadius { get; }
        public DebugRouteAction Action { get; }
        public DebugRouteAssertion Assertion { get; }
    }

    /// <summary>Owns deterministic route sequencing, progress monitoring, recovery state, and its run report.</summary>
    public sealed class DebugRouteSequencer
    {
        private const float STALL_SECONDS = 2f;
        private const float ACTION_TIMEOUT_SECONDS = 3f;
        private const float PROGRESS_EPSILON = 0.25f;
        private const int MAXIMUM_RECOVERIES = 3;

        private readonly List<DebugRouteStep> m_steps = new();
        private readonly List<Vector3> m_recordedPositions = new();
        private readonly StringBuilder m_report = new();
        private int m_stepIndex;
        private int m_recoveryCount;
        private float m_stepSeconds;
        private float m_noProgressSeconds;
        private float m_bestDistance = float.PositiveInfinity;
        private float m_startSignal;
        private bool m_actionIssued;

        public DebugRouteRunState State { get; private set; } = DebugRouteRunState.Idle;
        public DebugAutomationMode Mode { get; private set; }
        public DebugAutomationProfile Profile { get; private set; }
        public DebugRoutePreset Preset { get; private set; }
        public DebugRouteStep CurrentStep => m_stepIndex >= 0 && m_stepIndex < m_steps.Count ? m_steps[m_stepIndex] : null;
        public bool IsRunning => State == DebugRouteRunState.Navigating || State == DebugRouteRunState.Verifying;
        public bool IsRecovering => m_recoveryCount > 0 && m_noProgressSeconds > 0f;
        public int StepNumber => CurrentStep != null && State != DebugRouteRunState.Completed ? m_stepIndex + 1 : 0;
        public int StepCount => m_steps.Count;
        public int RecoveryCount => m_recoveryCount;
        public float StepSeconds => m_stepSeconds;
        public string Report => m_report.ToString();
        public bool PauseAfterEachStep { get; set; }

        public void Record(Vector3 position) => m_recordedPositions.Add(position);

        public void ClearRecording() => m_recordedPositions.Clear();

        public void Start(DebugRoutePreset preset, DebugAutomationMode mode, DebugAutomationProfile profile, float signal)
        {
            Preset = preset;
            Mode = mode;
            Profile = profile;
            m_startSignal = signal;
            m_steps.Clear();
            m_steps.AddRange(_createSteps(preset));
            m_stepIndex = 0;
            m_report.Clear();
            m_report.AppendLine($"DEAD SIGNAL PLAYTEST ROUTE — {preset} / {mode} / {profile}");
            m_report.AppendLine($"Started {DateTime.Now:O}  Signal {signal:0.0}");
            if (m_steps.Count == 0)
            {
                _fail("Route contains no recorded nodes.");
                return;
            }
            _beginStep();
        }

        public void Pause()
        {
            if (IsRunning)
            {
                State = DebugRouteRunState.Paused;
            }
        }

        public void Resume()
        {
            if (State == DebugRouteRunState.Paused)
            {
                State = DebugRouteRunState.Navigating;
            }
        }

        public void Abort(string reason) => _fail(reason);

        public void Skip()
        {
            if (CurrentStep == null)
            {
                return;
            }
            m_report.AppendLine($"SKIP {m_stepIndex + 1}/{m_steps.Count} {CurrentStep.Name}");
            _advance();
        }

        public void Retry()
        {
            if (CurrentStep != null)
            {
                _beginStep();
            }
        }

        public bool TickNavigation(float distance, float dt, bool blocked)
        {
            if (State != DebugRouteRunState.Navigating || CurrentStep == null)
            {
                return false;
            }

            m_stepSeconds += dt;
            if (distance + PROGRESS_EPSILON < m_bestDistance)
            {
                m_bestDistance = distance;
                m_noProgressSeconds = 0f;
            }
            else
            {
                m_noProgressSeconds += dt;
            }

            if (distance <= CurrentStep.ArrivalRadius)
            {
                State = DebugRouteRunState.Verifying;
                m_actionIssued = false;
                m_report.AppendLine($"ARRIVE {m_stepIndex + 1}/{m_steps.Count} {CurrentStep.Name} in {m_stepSeconds:0.00}s");
                return true;
            }

            if (m_noProgressSeconds >= STALL_SECONDS)
            {
                m_recoveryCount++;
                m_noProgressSeconds = 0.01f;
                m_bestDistance = distance;
                m_report.AppendLine($"RECOVER {CurrentStep.Name} attempt {m_recoveryCount} at {distance:0.00}m");
                if (m_recoveryCount > MAXIMUM_RECOVERIES)
                {
                    _fail($"Route stalled at {CurrentStep.Name}, {distance:0.00}m from target.");
                }
            }
            return false;
        }

        public bool ShouldIssueAction()
        {
            if (State != DebugRouteRunState.Verifying || m_actionIssued)
            {
                return false;
            }
            m_actionIssued = true;
            return true;
        }

        public void TickVerification(float dt, bool actionVerified, bool assertionPassed, string detail, float signal)
        {
            if (State != DebugRouteRunState.Verifying || CurrentStep == null)
            {
                return;
            }
            m_stepSeconds += dt;
            if (actionVerified && assertionPassed)
            {
                m_report.AppendLine($"PASS {CurrentStep.Name} — {detail} — Signal {signal:0.0}");
                _advance();
            }
            else if (m_stepSeconds >= ACTION_TIMEOUT_SECONDS)
            {
                _fail($"Verification failed at {CurrentStep.Name}: {detail}");
            }
        }

        public string FinishReport(float signal, int shots, int threats, Vector3 position)
        {
            if (!m_report.ToString().Contains("Final Signal"))
            {
                m_report.AppendLine($"Final Signal {signal:0.0} (Δ {signal - m_startSignal:+0.0;-0.0;0.0})");
                m_report.AppendLine($"Shots {shots}  Threats {threats}  Position {position.x:0.00},{position.z:0.00}");
            }
            return m_report.ToString();
        }

        private IEnumerable<DebugRouteStep> _createSteps(DebugRoutePreset preset)
        {
            switch (preset)
            {
                case DebugRoutePreset.OpeningLoop:
                    yield return new DebugRouteStep("Central tower", DebugLocation.CentralTower, 2f,
                        DebugRouteAction.ActivateCentralTower, DebugRouteAssertion.InteractionInRange);
                    yield return new DebugRouteStep("Extraction return", DebugLocation.Extraction, 1.5f, DebugRouteAction.CaptureScreenshot);
                    break;
                case DebugRoutePreset.RequiredSalvage:
                    yield return new DebugRouteStep("Nearest cache one", DebugLocation.CurrentObjective, 2.3f, DebugRouteAction.CollectCache);
                    yield return new DebugRouteStep("Nearest cache two", DebugLocation.CurrentObjective, 2.3f, DebugRouteAction.CollectCache);
                    yield return new DebugRouteStep("Nearest cache three", DebugLocation.CurrentObjective, 2.3f, DebugRouteAction.CollectCache);
                    break;
                case DebugRoutePreset.ThreeTowerRun:
                    yield return new DebugRouteStep("Central tower", DebugLocation.CentralTower, 2f, DebugRouteAction.ActivateCentralTower);
                    yield return new DebugRouteStep("Relay tower", DebugLocation.RelayTower, 2f, DebugRouteAction.ActivateRelayTower);
                    yield return new DebugRouteStep("Spine tower", DebugLocation.SpineTower, 2f, DebugRouteAction.ActivateSpineTower);
                    break;
                case DebugRoutePreset.EasternRoom:
                    yield return new DebugRouteStep("Relay tower", DebugLocation.RelayTower, 2f, DebugRouteAction.ActivateRelayTower);
                    yield return new DebugRouteStep("Far east", DebugLocation.FarEast, 0.8f, DebugRouteAction.CaptureScreenshot,
                        DebugRouteAssertion.CameraContainsPlayer);
                    yield return new DebugRouteStep("Optional cache", DebugLocation.CacheFour, 2.3f, DebugRouteAction.CollectCache);
                    break;
                case DebugRoutePreset.FullExtraction:
                    yield return new DebugRouteStep("Central tower", DebugLocation.CentralTower, 2f, DebugRouteAction.ActivateCentralTower);
                    yield return new DebugRouteStep("Relay tower", DebugLocation.RelayTower, 2f, DebugRouteAction.ActivateRelayTower);
                    yield return new DebugRouteStep("Piercing calibration", DebugLocation.RelayTower, 2f,
                        DebugRouteAction.SelectWeaponOverclock);
                    yield return new DebugRouteStep("Spine tower", DebugLocation.SpineTower, 2f, DebugRouteAction.ActivateSpineTower);
                    yield return new DebugRouteStep("Nearest cache one", DebugLocation.CurrentObjective, 2.3f, DebugRouteAction.CollectCache);
                    yield return new DebugRouteStep("Nearest cache two", DebugLocation.CurrentObjective, 2.3f, DebugRouteAction.CollectCache);
                    yield return new DebugRouteStep("Nearest cache three", DebugLocation.CurrentObjective, 2.3f, DebugRouteAction.CollectCache);
                    yield return new DebugRouteStep("Optional Quench cache", DebugLocation.CacheFour, 2.3f,
                        DebugRouteAction.CollectCache);
                    yield return new DebugRouteStep("Extraction", DebugLocation.Extraction, 1.5f, DebugRouteAction.BeginStableExtraction,
                        DebugRouteAssertion.SignalAboveTwenty);
                    break;
                case DebugRoutePreset.RecordedRoute:
                    for (var index = 0; index < m_recordedPositions.Count; index++)
                    {
                        yield return new DebugRouteStep($"Recorded node {index + 1}", m_recordedPositions[index]);
                    }
                    break;
            }
        }

        private void _beginStep()
        {
            State = DebugRouteRunState.Navigating;
            m_stepSeconds = 0f;
            m_noProgressSeconds = 0f;
            m_bestDistance = float.PositiveInfinity;
            m_recoveryCount = 0;
            m_actionIssued = false;
        }

        private void _advance()
        {
            m_stepIndex++;
            if (m_stepIndex >= m_steps.Count)
            {
                State = DebugRouteRunState.Completed;
                m_report.AppendLine("ROUTE COMPLETE");
                return;
            }
            if (PauseAfterEachStep)
            {
                _beginStep();
                State = DebugRouteRunState.Paused;
            }
            else
            {
                _beginStep();
            }
        }

        private void _fail(string reason)
        {
            State = DebugRouteRunState.Failed;
            m_report.AppendLine($"ROUTE FAILED — {reason}");
        }
    }
}
