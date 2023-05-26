using System.Collections.Generic;
using GameData;
using LevelGeneration;
using LEGACY.Utils;

namespace LEGACY.LegacyOverride
{
    public class InstanceManager<T> where T: Il2CppSystem.Object
    {
        private Dictionary<(eDimensionIndex, LG_LayerType, eLocalZoneIndex), Dictionary<System.IntPtr, uint>> instances2Index = new();
        private Dictionary<(eDimensionIndex, LG_LayerType, eLocalZoneIndex), List<T>> index2Instance = new();

        public const uint INVALID_INSTANCE_INDEX = uint.MaxValue;

        public uint Register((eDimensionIndex, LG_LayerType, eLocalZoneIndex) globalZoneIndex, T instance)
        {
            if (instance == null) return INVALID_INSTANCE_INDEX;

            Dictionary<System.IntPtr, uint> instancesInZone = null;
            List<T> instanceIndexInZone = null;
            if (!instances2Index.ContainsKey(globalZoneIndex))
            {
                instancesInZone = new();
                instanceIndexInZone = new();
                instances2Index[globalZoneIndex] = instancesInZone;
                index2Instance[globalZoneIndex] = instanceIndexInZone;
            }
            else
            {
                instancesInZone = instances2Index[globalZoneIndex];
                instanceIndexInZone = index2Instance[globalZoneIndex];
            }

            if(instancesInZone.ContainsKey(instance.Pointer)) 
            {
                LegacyLogger.Error($"InstanceManager<{typeof(T)}>: trying to register duplicate instance! Skipped....");
                return INVALID_INSTANCE_INDEX;
            }

            uint instanceIndex = (uint)instancesInZone.Count; // valid index starts from 0

            instancesInZone[instance.Pointer] = instanceIndex;
            instanceIndexInZone.Add(instance);

            return instanceIndex;
        }

        public uint Register(eDimensionIndex dimensionIndex, LG_LayerType layerType, eLocalZoneIndex localIndex, T instance) => Register((dimensionIndex, layerType, localIndex), instance);

        public uint GetIndex((eDimensionIndex, LG_LayerType, eLocalZoneIndex) globalZoneIndex, T instance)
        {
            if (!instances2Index.ContainsKey(globalZoneIndex)) return INVALID_INSTANCE_INDEX;

            var zoneInstanceIndices = instances2Index[globalZoneIndex];
            return zoneInstanceIndices.ContainsKey(instance.Pointer) ? zoneInstanceIndices[instance.Pointer] : INVALID_INSTANCE_INDEX;
        }

        public uint GetIndex(eDimensionIndex dimensionIndex, LG_LayerType layerType, eLocalZoneIndex localIndex, T instance) => GetIndex((dimensionIndex, layerType, localIndex), instance);

        public T GetInstance((eDimensionIndex, LG_LayerType, eLocalZoneIndex) globalZoneIndex, uint instanceIndex)
        {
            if (!index2Instance.ContainsKey(globalZoneIndex)) return default(T);

            var zoneInstanceIndices = index2Instance[globalZoneIndex];
            
            return instanceIndex < zoneInstanceIndices.Count ? zoneInstanceIndices[(int)instanceIndex] : default(T);
        }

        public T GetInstance(eDimensionIndex dimensionIndex, LG_LayerType layerType, eLocalZoneIndex localIndex, uint instanceIndex) => GetInstance((dimensionIndex, layerType, localIndex), instanceIndex);

        public List<T> GetInstanceInZone((eDimensionIndex, LG_LayerType, eLocalZoneIndex) globalZoneIndex) => index2Instance.ContainsKey(globalZoneIndex) ? index2Instance[globalZoneIndex] : null;

        public List<T> GetInstanceInZone(eDimensionIndex dimensionIndex, LG_LayerType layerType, eLocalZoneIndex localIndex) => GetInstanceInZone((dimensionIndex, layerType, localIndex));
    
        public IEnumerable<(eDimensionIndex, LG_LayerType, eLocalZoneIndex)> RegisteredZones() => index2Instance.Keys;

        public void Clear()
        {
            index2Instance.Clear();
            instances2Index.Clear();
        }
    }
}
