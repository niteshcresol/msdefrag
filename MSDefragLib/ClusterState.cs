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
            dirty = true;
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
                    dirty = true;
                }

                state = value;
            }
        }

        private Boolean dirty;
        public Boolean Dirty
        {
            get { return dirty; }
            set { dirty = value; }
        }

        private Boolean masterFileTable;
        public Boolean MasterFileTable
        {
            get { return masterFileTable; }
            set { masterFileTable = value; }
        }

        private Boolean currentyUsed;
        public Boolean CurrentlyUsed
        {
            get { return currentyUsed; }
            set { currentyUsed = value; }
        }

        private Boolean spaceHog;
        public Boolean SpaceHog
        {
            get { return spaceHog; }
            set { spaceHog = value; }
        }
    }
}
