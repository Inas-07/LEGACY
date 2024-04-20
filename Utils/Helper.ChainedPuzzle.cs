using ChainedPuzzles;
using SNetwork;

namespace LEGACY.Utils
{
    public static partial class Helper
    {
        private static void ResetChild(iChainedPuzzleCore ICore)
        {
            var bioCore = ICore.TryCast<CP_Bioscan_Core>();
            if (bioCore != null)
            {
                var spline = bioCore.m_spline.Cast<CP_Holopath_Spline>();
                //spline.SetSplineProgress(0);

                var scanner = bioCore.PlayerScanner.Cast<CP_PlayerScanner>();
                scanner.ResetScanProgression(0.0f);

                bioCore.Deactivate();
            }
            else
            {
                var clusterCore = ICore.TryCast<CP_Cluster_Core>();
                if(clusterCore == null)
                {
                    LegacyLogger.Error($"ResetChild: found iChainedPuzzleCore that is neither CP_Bioscan_Core nor CP_Cluster_Core...");
                    return;
                }

                var spline = clusterCore.m_spline.Cast<CP_Holopath_Spline>();

                //spline.SetSplineProgress(0);

                foreach (var child in clusterCore.m_childCores)
                {
                    ResetChild(child);
                }

                clusterCore.Deactivate();
            }
        }

        public static void ResetChainedPuzzle(ChainedPuzzleInstance chainedPuzzleInstance)
        {
            if (chainedPuzzleInstance.Data.DisableSurvivalWaveOnComplete)
            {
                chainedPuzzleInstance.m_sound = new CellSoundPlayer(chainedPuzzleInstance.m_parent.position);
            }

            foreach (var IChildCore in chainedPuzzleInstance.m_chainedPuzzleCores)
            {
                ResetChild(IChildCore);
            }

            if (SNet.IsMaster)
            {
                var oldState = chainedPuzzleInstance.m_stateReplicator.State;
                var newState = new pChainedPuzzleState()
                {
                    status = eChainedPuzzleStatus.Disabled,
                    currentSurvivalWave_EventID = oldState.currentSurvivalWave_EventID,
                    isSolved = false,
                    isActive = false,
                };
                chainedPuzzleInstance.m_stateReplicator.InteractWithState(newState, new() { type = eChainedPuzzleInteraction.Deactivate });
                //chainedPuzzleInstance.AttemptInteract(eChainedPuzzleInteraction.Deactivate);
            }
        }
    }
}

