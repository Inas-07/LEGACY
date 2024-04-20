namespace LEGACY.LegacyOverride.Music
{
    public struct MusicSyncStruct
    {
        public uint Id0;
        public uint Id1;
        public uint Id2;
        public uint Id3;
        public uint Id4;

        public MusicSyncStruct() { }

        public MusicSyncStruct(MusicSyncStruct o)
        {
            Id0 = o.Id0;
            Id1 = o.Id1;
            Id2 = o.Id2;
            Id3 = o.Id3;
            Id4 = o.Id4;
        }
    }
}
