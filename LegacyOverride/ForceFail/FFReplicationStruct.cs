using System;

namespace LEGACY.LegacyOverride.ForceFail
{
    public struct FFReplicationStruct
    {
        public bool enabled = false;
        public bool checkP1 = false;
        public bool checkP2 = false;
        public bool checkP3 = false;
        public bool checkP4 = false;

        public FFReplicationStruct()
        {

        }

        public FFReplicationStruct(FFReplicationStruct o)
        {
            o.enabled = enabled;
            o.checkP1 = checkP1;
            o.checkP2 = checkP2;
            o.checkP3 = checkP3;
            o.checkP4 = checkP4;
        }

        public FFReplicationStruct(bool enabled, bool checkP1, bool checkP2, bool checkP3, bool checkP4)
        {
            this.enabled = enabled;
            this.checkP1 = checkP1;
            this.checkP2 = checkP2;
            this.checkP3 = checkP3;       
            this.checkP4 = checkP4;
        }
    }

}
