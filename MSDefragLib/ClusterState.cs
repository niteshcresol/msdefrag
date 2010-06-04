using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSDefragLib
{
    public enum eClusterState
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
        public ClusterState(Int32 clusterIndex, eClusterState newState)
        {
            index = clusterIndex;
            state = newState;
            isDirty = true;
        }

        private Int32 index;

        public Int32 Index { get { return index; } }

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
