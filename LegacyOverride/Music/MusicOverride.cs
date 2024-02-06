using System.Collections.Generic;

namespace LEGACY.LegacyOverride.Music
{
    public class MusicOverride
    {
        public string WorldEventObjectFilter { get; set; } = string.Empty;

        public uint StartID { get; set; }

        public uint StopID { get; set; } = 0;
        
        public uint VolumeRTPC { get; set; } = 0;

        public float Duration { get; set; } = -1.0f; // looped sound

        public MusicOverride() { }

        public MusicOverride(string MusicName, uint StartID, uint StopID, uint VolumeRTPC, float Duration = -1.0f)
        {
            this.WorldEventObjectFilter = MusicName;
            this.StartID = StartID;
            this.StopID = StopID;
            this.VolumeRTPC = VolumeRTPC;
            this.Duration = Duration;
        }
    }
}