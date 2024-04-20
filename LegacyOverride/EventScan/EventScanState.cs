using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LEGACY.LegacyOverride.EventScan
{
    public enum EventScanState
    {
        Disabled,
        Waiting,
        Active
    }

    public struct EventScanStatus
    {
        public EventScanState Status;
    }
}
