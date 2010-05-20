using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSDefragLib
{
    public class MapClusterState
    {
        public MapClusterState(UInt64 clusterIndex)
        {
            index = clusterIndex;
            isDirty = true;

            numClusterStates = new Int32[(Int32)eClusterState.MaxValue];

            for (Int32 ii = 0; ii < (Int32)eClusterState.MaxValue; ii++)
            {
                numClusterStates[ii] = 0;
            }
        }

        private UInt64 index;

        public UInt64 Index
        {
            get { return index; }
        }

        public eClusterState MaxState
        {
            get
            {
                if (numClusterStates[(Int32)eClusterState.Busy] > 0)
                {
                    return eClusterState.Busy;
                }

                if (numClusterStates[(Int32)eClusterState.Mft] > 0)
                {
                    return eClusterState.Mft;
                }

                if (numClusterStates[(Int32)eClusterState.Unmovable] > 0)
                {
                    return eClusterState.Unmovable;
                }

                if (numClusterStates[(Int32)eClusterState.Fragmented] > 0)
                {
                    return eClusterState.Fragmented;
                }

                if (numClusterStates[(Int32)eClusterState.Unfragmented] > 0)
                {
                    return eClusterState.Unfragmented;
                }

                if (numClusterStates[(Int32)eClusterState.SpaceHog] > 0)
                {
                    return eClusterState.SpaceHog;
                }

                if (numClusterStates[(Int32)eClusterState.Allocated] > 0)
                {
                    return eClusterState.Allocated;
                }

                return eClusterState.Free;
            }
        }

        public void AddClusterState(eClusterState state)
        {
            numClusterStates[(Int32)state]++;
        }

        public void RemoveClusterState(eClusterState state)
        {
            if (state == eClusterState.Mft || state == eClusterState.Unmovable)
            {
                return;
            }

            numClusterStates[(Int32)state]--;

            if (numClusterStates[(Int32)state] < 0)
            {
                numClusterStates[(Int32)state] = 0;
            }
        }

        public void ResetClusterStates()
        {
            for (int ii = 0; ii < (Int32)eClusterState.MaxValue; ii++)
            {
                numClusterStates[(Int32)ii] = 0;
            }
        }

        private Boolean isDirty;

        public Boolean Dirty
        {
            get { return isDirty; }
            set { isDirty = value; }
        }
        Int32[] numClusterStates;
    }
}
