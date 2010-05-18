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
    public class ClusterState
    {
        public ClusterState(UInt64 clusterIndex, eClusterState newState)
        {
            index = clusterIndex;
            state = newState;
            isDirty = true;
        }

        private UInt64 index;

        public UInt64 Index { get { return index; } }

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

        private Boolean isDirty;

        public Boolean Dirty {
            get { return isDirty; }
            set { isDirty = value; }
        }
    }
}
