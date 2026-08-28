using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DeadSignal.Missions;
using UnityEngine;

namespace DeadSignal.Diagnostics
{
    public enum DebugRoutePreset
    {
        OpeningLoop,
        RequiredSalvage,
        ThreeTowerRun,
        EasternRoom,
        RequiredExtraction,
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
        RouteCentralComponents,
        AssembleCentralPayload,
        InstallCentralPayload,
        ActivateRelayTower,
        SelectWeaponOverclock,
        VentSpineBerth,
        ActivateSpineTower,
        ChargeInductionLattice,
        RouteFluxShunt,
        CompleteConvergenceCalibration,
        ResetBreakerDistribution,
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
            DebugRouteAssertion assertion = DebugRouteAssertion.None, string roomName = null, bool isBacktrack = false)
        {
            Name = name;
            Location = location;
            ArrivalRadius = arrivalRadius;
            Action = action;
            Assertion = assertion;
            RoomName = roomName;
            IsBacktrack = isBacktrack;
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
        public string RoomName { get; }
        public bool IsBacktrack { get; }
    }

    /// <summary>Owns deterministic route sequencing, progress monitoring, recovery state, and its run report.</summary>
    public sealed class DebugRouteSequencer
    {
        private const float STALL_SECONDS = 2f;
        private const float ACTION_TIMEOUT_SECONDS = 3f;
        private const float PROGRESS_EPSILON = 0.25f;
        private const int MAXIMUM_RECOVERIES = 3;

        private static readonly string[] s_missionRooms =
        {
            "Extraction Dock",
            "Central Chamber",
            "Cargo Annex",
            "Coolant Reclamation",
            "Relay Fork",
            "East Transfer Vault",
            "Relay Foundry",
            "Cooling Gantry",
            "Capacitor Spine",
            "Spine Discharge Trench",
            "Induction Gallery",
            "Flux Bypass",
            "Convergence Chamber",
            "Breaker Gallery",
            "Arc Furnace",
            "Quench Loop",
            "Room A",
            "Room B",
            "Room C"
        };

        private readonly List<DebugRouteStep> m_steps = new();
        private readonly List<Vector3> m_recordedPositions = new();
        private readonly HashSet<string> m_visitedMissionRooms = new(StringComparer.Ordinal);
        private readonly StringBuilder m_report = new();
        private string m_finishedReport;
        private int m_stepIndex;
        private int m_recoveryCount;
        private float m_stepSeconds;
        private float m_noProgressSeconds;
        private float m_bestDistance = float.PositiveInfinity;
        private float m_startSignal;
        private float m_guidanceResponseSeconds;
        private float m_maximumGuidanceResponseSeconds;
        private int m_guidanceResponseSamples;
        private int m_totalRecoveryCount;
        private int m_completedBacktrackLegs;
        private bool m_hasDistanceSample;
        private bool m_guidanceResponseRecorded;
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
        public string Report => State is DebugRouteRunState.Completed or DebugRouteRunState.Failed &&
                                !string.IsNullOrEmpty(m_finishedReport)
            ? m_finishedReport
            : m_report.ToString();
        public bool AwaitsRunOutcomeReport => State == DebugRouteRunState.Completed &&
                                              Preset is DebugRoutePreset.RequiredExtraction or DebugRoutePreset.FullExtraction;
        public bool PauseAfterEachStep { get; set; }

        public void Record(Vector3 position) => m_recordedPositions.Add(position);

        public void ClearRecording() => m_recordedPositions.Clear();

        public void RecordMissionRoomVisit(string roomName)
        {
            if (!string.IsNullOrWhiteSpace(roomName) && s_missionRooms.Contains(roomName))
            {
                m_visitedMissionRooms.Add(roomName);
            }
        }

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
            m_finishedReport = null;
            m_visitedMissionRooms.Clear();
            m_guidanceResponseSeconds = 0f;
            m_maximumGuidanceResponseSeconds = 0f;
            m_guidanceResponseSamples = 0;
            m_totalRecoveryCount = 0;
            m_completedBacktrackLegs = 0;
            m_report.AppendLine($"DEAD SIGNAL PLAYTEST ROUTE — {preset} / {mode} / {profile}");
            m_report.AppendLine($"Started {DateTime.Now:O}  Signal {signal:0.0}");
            if (preset is DebugRoutePreset.RequiredExtraction or DebugRoutePreset.FullExtraction)
            {
                RecordMissionRoomVisit("Extraction Dock");
            }
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

        public bool TickNavigation(float distance, float dt, bool blocked, bool hasCompleteNavigationRoute = false)
        {
            if (State != DebugRouteRunState.Navigating || CurrentStep == null)
            {
                return false;
            }

            m_stepSeconds += dt;
            if (!m_hasDistanceSample)
            {
                m_hasDistanceSample = true;
                m_bestDistance = distance;
            }
            else if (distance + PROGRESS_EPSILON < m_bestDistance)
            {
                m_bestDistance = distance;
                m_noProgressSeconds = 0f;
                _recordGuidanceResponse();
            }
            else if (blocked || !hasCompleteNavigationRoute)
            {
                m_noProgressSeconds += dt;
            }
            else
            {
                m_noProgressSeconds = 0f;
            }

            if (distance <= CurrentStep.ArrivalRadius)
            {
                _recordGuidanceResponse();
                State = DebugRouteRunState.Verifying;
                m_actionIssued = false;
                m_report.AppendLine($"ARRIVE {m_stepIndex + 1}/{m_steps.Count} {CurrentStep.Name} in {m_stepSeconds:0.00}s");
                return true;
            }

            if (m_noProgressSeconds >= STALL_SECONDS)
            {
                m_recoveryCount++;
                m_totalRecoveryCount++;
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

        public string FinishReport(float signal, RunMetrics metrics, bool optionalSalvageSecured, bool shortcutOpen,
            Vector3 position, RunOutcome outcome = RunOutcome.Running, int directedShots = 0, int evasionResponses = 0,
            MissionObjectiveDefinition objective = null)
        {
            var finishedReport = new StringBuilder(m_report.ToString());
            finishedReport.AppendLine($"Outcome {outcome}");
            if (objective != null)
            {
                finishedReport.AppendLine($"Objective {objective.Id}  Phase {objective.Guidance.Phase}  " +
                                          $"{objective.Guidance.Title}  Room {objective.OwningRoom}  Anchor {objective.AnchorId}");
            }
            finishedReport.AppendLine($"Final Signal {signal:0.0} (Δ {signal - m_startSignal:+0.0;-0.0;0.0})");
            finishedReport.AppendLine($"Journey {(optionalSalvageSecured ? "OPTIONAL GREED" : "REQUIRED WITHDRAWAL")}  " +
                                      $"Shortcut {(shortcutOpen ? "OPEN" : "CLOSED")}");
            finishedReport.AppendLine($"Time {metrics.ElapsedSeconds:0.00}s  Dead zone {metrics.DeadZoneSeconds:0.00}s  " +
                                      $"Combat {metrics.CombatSeconds:0.00}s  Hits {metrics.SecurityHits}  " +
                                      $"Sapper drains {metrics.SapperPulses}");
            finishedReport.AppendLine($"Shots {metrics.ShotsFired}  Purges {metrics.ThreatsPurged}  " +
                                      $"Travel spent {metrics.PassiveSignalSpent + metrics.MovementSignalSpent:0.0}  " +
                                      $"Fire spent {metrics.WeaponSignalSpent:0.0}");
            finishedReport.AppendLine($"Minimum Signal {metrics.MinimumSignal:0.0}  Peak threats {metrics.PeakThreatConcurrency}  " +
                                      $"Swarmer contacts {metrics.SwarmerContacts}  Swarmer purges {metrics.SwarmersPurged}");
            finishedReport.AppendLine($"Live policy shots {directedShots}  Evasion responses {evasionResponses}");
            var averageGuidanceResponse = m_guidanceResponseSamples > 0
                ? m_guidanceResponseSeconds / m_guidanceResponseSamples
                : 0f;
            finishedReport.AppendLine($"Guidance response proxy avg {averageGuidanceResponse:0.00}s  " +
                                      $"max {m_maximumGuidanceResponseSeconds:0.00}s  " +
                                      $"Wrong-turn proxies {m_totalRecoveryCount}  Backtrack legs {m_completedBacktrackLegs}");
            var visitedRooms = s_missionRooms.Where(m_visitedMissionRooms.Contains).ToArray();
            var absentRooms = s_missionRooms.Where(room => !m_visitedMissionRooms.Contains(room)).ToArray();
            finishedReport.AppendLine($"Objective-room coverage {visitedRooms.Length}/{s_missionRooms.Length}: " +
                                      $"{string.Join(", ", visitedRooms)}");
            finishedReport.AppendLine($"Rooms without a compatibility-route objective {absentRooms.Length}: " +
                                      $"{string.Join(", ", absentRooms)}");
            finishedReport.AppendLine($"Recovered {metrics.SignalRecovered + metrics.SalvageSignalRecovered:0.0}  " +
                                      $"Position {position.x:0.00},{position.z:0.00}");
            m_finishedReport = finishedReport.ToString();
            return m_finishedReport;
        }

        private IEnumerable<DebugRouteStep> _createSteps(DebugRoutePreset preset)
        {
            switch (preset)
            {
                case DebugRoutePreset.OpeningLoop:
                    yield return new DebugRouteStep("Central tower", DebugLocation.CentralTower, 2f,
                        DebugRouteAction.ActivateCentralTower, DebugRouteAssertion.InteractionInRange, "Central Chamber");
                    yield return new DebugRouteStep("Extraction return", DebugLocation.Extraction, 1.5f,
                        DebugRouteAction.CaptureScreenshot, roomName: "Extraction Dock", isBacktrack: true);
                    break;
                case DebugRoutePreset.RequiredSalvage:
                    yield return new DebugRouteStep("Nearest cache one", DebugLocation.CurrentObjective, 2.3f, DebugRouteAction.CollectCache);
                    yield return new DebugRouteStep("Nearest cache two", DebugLocation.CurrentObjective, 2.3f, DebugRouteAction.CollectCache);
                    yield return new DebugRouteStep("Nearest cache three", DebugLocation.CurrentObjective, 2.3f, DebugRouteAction.CollectCache);
                    break;
                case DebugRoutePreset.ThreeTowerRun:
                    yield return new DebugRouteStep("Central tower", DebugLocation.CentralTower, 2f,
                        DebugRouteAction.ActivateCentralTower, roomName: "Central Chamber");
                    yield return new DebugRouteStep("Relay tower", DebugLocation.RelayTower, 2f,
                        DebugRouteAction.ActivateRelayTower, roomName: "Relay Foundry");
                    yield return new DebugRouteStep("Spine tower", DebugLocation.SpineTower, 2f,
                        DebugRouteAction.ActivateSpineTower, roomName: "Capacitor Spine");
                    break;
                case DebugRoutePreset.EasternRoom:
                    yield return new DebugRouteStep("Relay tower", DebugLocation.RelayTower, 2f, DebugRouteAction.ActivateRelayTower);
                    yield return new DebugRouteStep("Far east", DebugLocation.FarEast, 0.8f, DebugRouteAction.CaptureScreenshot,
                        DebugRouteAssertion.CameraContainsPlayer);
                    yield return new DebugRouteStep("Optional cache", DebugLocation.CacheFour, 2.3f, DebugRouteAction.CollectCache);
                    break;
                case DebugRoutePreset.RequiredExtraction:
                case DebugRoutePreset.FullExtraction:
                    yield return new DebugRouteStep("Central tower", DebugLocation.CentralTower, 2f,
                        DebugRouteAction.ActivateCentralTower, roomName: "Central Chamber");
                    yield return new DebugRouteStep("Cargo coupling", DebugLocation.CurrentObjective, 2.3f,
                        DebugRouteAction.CollectCache, roomName: "Cargo Annex");
                    yield return new DebugRouteStep("Coolant seal", DebugLocation.CurrentObjective, 2.3f,
                        DebugRouteAction.CollectCache, roomName: "Coolant Reclamation");
                    yield return new DebugRouteStep("Relay Fork routing", DebugLocation.RelayFork, 1.7f,
                        DebugRouteAction.RouteCentralComponents, roomName: "Relay Fork");
                    yield return new DebugRouteStep("Transfer-vault assembly", DebugLocation.TransferVault, 1.7f,
                        DebugRouteAction.AssembleCentralPayload, roomName: "East Transfer Vault");
                    yield return new DebugRouteStep("Central payload installation", DebugLocation.CentralTower, 2f,
                        DebugRouteAction.InstallCentralPayload, roomName: "Central Chamber", isBacktrack: true);
                    yield return new DebugRouteStep("Relay tower", DebugLocation.RelayTower, 2f,
                        DebugRouteAction.ActivateRelayTower, roomName: "Relay Foundry");
                    yield return new DebugRouteStep("Cooling Gantry stabilization", DebugLocation.CurrentObjective, 2.3f,
                        DebugRouteAction.CollectCache, roomName: "Cooling Gantry");
                    yield return new DebugRouteStep("Foundry payload installation", DebugLocation.RelayTower, 2f,
                        DebugRouteAction.SelectWeaponOverclock, roomName: "Relay Foundry", isBacktrack: true);
                    yield return new DebugRouteStep("Capacitor Spine arrival", DebugLocation.SpineTower, 2f,
                        DebugRouteAction.CaptureScreenshot, roomName: "Capacitor Spine");
                    yield return new DebugRouteStep("Spine berth venting", DebugLocation.CurrentObjective, 1.7f,
                        DebugRouteAction.VentSpineBerth, roomName: "Spine Discharge Trench");
                    yield return new DebugRouteStep("Spine tower", DebugLocation.SpineTower, 2f,
                        DebugRouteAction.ActivateSpineTower, roomName: "Capacitor Spine");
                    yield return new DebugRouteStep("Induction lattice charging", DebugLocation.CurrentObjective, 1.7f,
                        DebugRouteAction.ChargeInductionLattice, roomName: "Induction Gallery");
                    yield return new DebugRouteStep("Flux shunt routing", DebugLocation.CurrentObjective, 1.7f,
                        DebugRouteAction.RouteFluxShunt, roomName: "Flux Bypass");
                    yield return new DebugRouteStep("Convergence calibration", DebugLocation.CurrentObjective, 1.7f,
                        DebugRouteAction.CompleteConvergenceCalibration, roomName: "Convergence Chamber");
                    yield return new DebugRouteStep("Breaker distribution reset", DebugLocation.CurrentObjective, 1.7f,
                        DebugRouteAction.ResetBreakerDistribution, roomName: "Breaker Gallery");
                    yield return new DebugRouteStep("Spine payload", DebugLocation.CurrentObjective, 2.3f,
                        DebugRouteAction.CollectCache, roomName: "Capacitor Spine");
                    if (preset == DebugRoutePreset.FullExtraction)
                    {
                        yield return new DebugRouteStep("Optional Quench cache", DebugLocation.CacheFour, 2.3f,
                            DebugRouteAction.CollectCache, roomName: "Quench Loop");
                        yield return new DebugRouteStep("Quench return to Spine", DebugLocation.SpineTower, 2f,
                            DebugRouteAction.CaptureScreenshot, roomName: "Capacitor Spine", isBacktrack: true);
                    }
                    else
                    {
                        yield return new DebugRouteStep("Spine discharge withdrawal", DebugLocation.SpineTower, 2f,
                            DebugRouteAction.CaptureScreenshot, roomName: "Capacitor Spine", isBacktrack: true);
                    }
                    yield return new DebugRouteStep("Relay powered foothold", DebugLocation.RelayTower, 2f,
                        DebugRouteAction.None, roomName: "Relay Foundry", isBacktrack: true);
                    yield return new DebugRouteStep("Central powered foothold", DebugLocation.CentralTower, 2f,
                        DebugRouteAction.None, roomName: "Central Chamber", isBacktrack: true);
                    yield return new DebugRouteStep("Extraction", DebugLocation.Extraction, 1.5f, DebugRouteAction.BeginStableExtraction,
                        DebugRouteAssertion.SignalAboveTwenty, "Extraction Dock", true);
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
            m_hasDistanceSample = false;
            m_guidanceResponseRecorded = false;
            m_recoveryCount = 0;
            m_actionIssued = false;
        }

        private void _advance()
        {
            RecordMissionRoomVisit(CurrentStep?.RoomName);
            if (CurrentStep?.IsBacktrack == true)
            {
                m_completedBacktrackLegs++;
            }
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

        private void _recordGuidanceResponse()
        {
            if (m_guidanceResponseRecorded)
            {
                return;
            }

            m_guidanceResponseRecorded = true;
            m_guidanceResponseSeconds += m_stepSeconds;
            m_maximumGuidanceResponseSeconds = Mathf.Max(m_maximumGuidanceResponseSeconds, m_stepSeconds);
            m_guidanceResponseSamples++;
        }
    }
}
