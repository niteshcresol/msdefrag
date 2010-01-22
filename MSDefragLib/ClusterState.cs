using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSDefragLib
{
    public enum eClusterState : int
    {
        Free = 0,
        Allocated,
        Unfragmented,
        Unmovable,
        Fragmented,
        Busy,
        Mft,
        SpaceHog,

        MaxValue
    }
}
