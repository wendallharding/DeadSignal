using UnityEditor;

namespace DeadSignal.Editor
{
    public static class DeadSignalWithdrawalPursuitSetup
    {
        [MenuItem("DEAD SIGNAL/Setup/Refresh Withdrawal Pursuit")]
        public static void Refresh()
        {
            DeadSignalWardenBaySetup.EnsureAssets();
            DeadSignalSapperCradleSetup.EnsureAssets();
            DeadSignalMissionObjectiveSetup.CreateCompatibilityMissionObjectives();
        }
    }
}
