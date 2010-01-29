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

    /// <summary>
    /// Structure for describing cluster
    /// </summary>
    public class ClusterStructure
    {
        public ClusterStructure(UInt64 clusterIndex, eClusterState newState)
        {
            index = clusterIndex;
            state = newState;
            isDirty = true;
        }

        public UInt64 index;

        private eClusterState state;
        public eClusterState State
        {
            get { return state; }
            set
            {
                if (state != value)
                {
                    isDirty = true;
                }

                state = value;
            }
        }
        public Boolean isDirty;
    }
}
