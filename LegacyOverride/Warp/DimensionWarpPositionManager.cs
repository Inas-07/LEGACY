//using ExtraObjectiveSetup.BaseClasses;
//using LEGACY.Utils;
//using Microsoft.VisualBasic;
//using System.Collections.Generic;
//using UnityEngine;

//namespace LEGACY.LegacyOverride.SecDoorIntText
//{
//    internal class DimensionWarpPositionManager: GenericExpeditionDefinitionManager<DimensionWarpPosition>
//    {
//        public static DimensionWarpPositionManager Current { get; private set; }

//        public readonly List<PositionAndLookDir> DUMB = new();

//        protected override string DEFINITION_NAME => "DimensionWarp";

//        public List<PositionAndLookDir> GetWarpPositions(eDimensionIndex dimensionIndex, string worldEventObjectFilter)
//        {
//            if(GameStateManager.CurrentStateName != eGameStateName.InLevel
//                || !definitions.ContainsKey(RundownManager.ActiveExpedition.LevelLayoutData))
//            {
//                LegacyLogger.Error($"GetWarpPositions: Didn't find config with MainLevelLayout {RundownManager.ActiveExpedition.LevelLayoutData}");
//                return DUMB;
//            }

//            var dimWarpPositions = definitions[RundownManager.ActiveExpedition.LevelLayoutData].Definitions.Find(def => def.DimensionIndex == dimensionIndex);
//            if(dimWarpPositions == null)
//            {
//                LegacyLogger.Error($"GetWarpPositions: Didn't find config for {dimensionIndex} with MainLevelLayout {RundownManager.ActiveExpedition.LevelLayoutData}");
//                return DUMB;
//            }

//            var positions = dimWarpPositions.WarpLocations.Find(position => position.WorldEventObjectFilter == worldEventObjectFilter);
//            if (positions == null)
//            {
//                LegacyLogger.Error($"GetWarpPositions: Didn't find config for {worldEventObjectFilter} in {dimensionIndex} with MainLevelLayout {RundownManager.ActiveExpedition.LevelLayoutData}");
//                return DUMB;
//            }

//            return positions.Locations;
//        }

//        private DimensionWarpPositionManager() 
//        {
     
//        }

//        static DimensionWarpPositionManager()
//        {
//            Current = new();
//        }
//    }
//}
