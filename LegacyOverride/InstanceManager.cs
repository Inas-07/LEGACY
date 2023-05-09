using System.Collections.Generic;
using GameData;
using LevelGeneration;
using LEGACY.Utils;

namespace LEGACY.LegacyOverride
{
    public class InstanceManager<T>
    {
        private Dictionary<(eDimensionIndex, LG_LayerType, eLocalZoneIndex), Dictionary<T, uint>> AllInstances2Index = new();
        private Dictionary<(eDimensionIndex, LG_LayerType, eLocalZoneIndex), List<T>> AllIndex2Instance = new();

        public const uint INVALID_INSTANCE_INDEX = uint.MaxValue;

        public uint Register((eDimensionIndex, LG_LayerType, eLocalZoneIndex) globalZoneIndex, T instance)
        {
            if (instance == null) return INVALID_INSTANCE_INDEX;

            Dictionary<T, uint> instancesInZone = null;
            List<T> instanceIndexInZone = null;
            if (!AllInstances2Index.ContainsKey(globalZoneIndex))
            {
                instancesInZone = new();
                instanceIndexInZone = new();
                AllInstances2Index.Add(globalZoneIndex, instancesInZone);
                AllIndex2Instance.Add(globalZoneIndex, instanceIndexInZone);
            }
            else
            {
                instancesInZone = AllInstances2Index[globalZoneIndex];
                instanceIndexInZone = AllIndex2Instance[globalZoneIndex];
            }

            if(instancesInZone.ContainsKey(instance)) 
            {
                LegacyLogger.Error($"InstanceManager<{typeof(T)}>: trying to register duplicate instance! Skipped....");
                return INVALID_INSTANCE_INDEX;
            }

            uint instanceIndex = (uint)instancesInZone.Count; // valid index starts from 0

            instancesInZone.Add(instance, instanceIndex);
            instanceIndexInZone.Add(instance);

            return instanceIndex;
        }

        public uint Register(eDimensionIndex dimensionIndex, LG_LayerType layerType, eLocalZoneIndex localIndex, T instance) => Register((dimensionIndex, layerType, localIndex), instance);

        public uint GetIndex((eDimensionIndex, LG_LayerType, eLocalZoneIndex) globalZoneIndex, T instance)
        {
            if (!AllInstances2Index.ContainsKey(globalZoneIndex)) return INVALID_INSTANCE_INDEX;

            var zoneInstanceIndices = AllInstances2Index[globalZoneIndex];
            return zoneInstanceIndices.ContainsKey(instance) ? zoneInstanceIndices[instance] : INVALID_INSTANCE_INDEX;
        }

        public uint GetIndex(eDimensionIndex dimensionIndex, LG_LayerType layerType, eLocalZoneIndex localIndex, T instance) => GetIndex((dimensionIndex, layerType, localIndex), instance);

        public T GetInstance((eDimensionIndex, LG_LayerType, eLocalZoneIndex) globalZoneIndex, uint instanceIndex)
        {
            if (!AllIndex2Instance.ContainsKey(globalZoneIndex)) return default(T);

            var zoneInstanceIndices = AllIndex2Instance[globalZoneIndex];
            
            return instanceIndex < zoneInstanceIndices.Count ? zoneInstanceIndices[(int)instanceIndex] : default(T);
        }

        public T GetInstance(eDimensionIndex dimensionIndex, LG_LayerType layerType, eLocalZoneIndex localIndex, uint instanceIndex) => GetInstance((dimensionIndex, layerType, localIndex), instanceIndex);

        public List<T> GetInstanceInZone((eDimensionIndex, LG_LayerType, eLocalZoneIndex) globalZoneIndex) => AllIndex2Instance.ContainsKey(globalZoneIndex) ? AllIndex2Instance[globalZoneIndex] : null;

        public List<T> GetInstanceInZone(eDimensionIndex dimensionIndex, LG_LayerType layerType, eLocalZoneIndex localIndex) => GetInstanceInZone((dimensionIndex, layerType, localIndex));
    
        public IEnumerable<(eDimensionIndex, LG_LayerType, eLocalZoneIndex)> RegisteredZones() => AllIndex2Instance.Keys;

        public void Clear()
        {
            AllIndex2Instance.Clear();
            AllInstances2Index.Clear();
        }
    }
}
