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
                spline.SetSplineProgress(0);

                var scanner = bioCore.PlayerScanner.Cast<CP_PlayerScanner>();
                scanner.ResetScanProgression(0.0f);
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
                spline.SetSplineProgress(0);

                foreach(var child in clusterCore.m_childCores)
                {
                    ResetChild(child);
                }
            }
        }

        public static void ResetChainedPuzzle(ChainedPuzzleInstance chainedPuzzleInstance)
        {
            foreach (var IChildCore in chainedPuzzleInstance.m_chainedPuzzleCores)
            {
                ResetChild(IChildCore);
            }

            if (SNet.IsMaster)
            {
                chainedPuzzleInstance.AttemptInteract(eChainedPuzzleInteraction.Deactivate);
            }
        }
    }
}

