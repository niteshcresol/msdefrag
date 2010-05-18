using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MSDefragLib;

namespace MSDefrag
{
    class MapSquare
    {
        public MapSquare(/*Int32 sqIndex, UInt64 clusterBegin, UInt64 clusterEnd*/)
        {
            //squareIndex = sqIndex;
            maxClusterState = eClusterState.Free;
            //clusterBeginIndex = clusterBegin;
            //clusterEndIndex = clusterEnd;

            numClusterStates = new Int32[(Int32)eClusterState.MaxValue];

            for (Int32 ii = 0; ii < numClusterStates.Count(); ii++ )
            {
                numClusterStates[ii] = 0;
            }

            Dirty = true;
        }

        //private eClusterState GetMaxSquareColor()
        //{
        //    if (numClusterStates[(Int32)eClusterState.Busy] > 0)
        //    {
        //        return eClusterState.Busy;
        //    }

        //    if (numClusterStates[(Int32)eClusterState.Mft] > 0)
        //    {
        //        return eClusterState.Mft;
        //    }

        //    if (numClusterStates[(Int32)eClusterState.Unmovable] > 0)
        //    {
        //        return eClusterState.Unmovable;
        //    }

        //    if (numClusterStates[(Int32)eClusterState.Fragmented] > 0)
        //    {
        //        return eClusterState.Fragmented;
        //    }

        //    if (numClusterStates[(Int32)eClusterState.Unfragmented] > 0)
        //    {
        //        return eClusterState.Unfragmented;
        //    }

        //    if (numClusterStates[(Int32)eClusterState.SpaceHog] > 0)
        //    {
        //        return eClusterState.SpaceHog;
        //    }

        //    if (numClusterStates[(Int32)eClusterState.Allocated] > 0)
        //    {
        //        return eClusterState.Allocated;
        //    }

        //    return eClusterState.Free;
        //}

        //public void SetMaxColor()
        //{
        //    Int32 oldState = (Int32)maxClusterState;

        //    maxClusterState = GetMaxSquareColor();

        //    if ((Int32)maxClusterState != oldState)
        //    {
        //        IsDirty = true;
        //    }
        //}

        private Boolean isDirty;
        public Boolean Dirty
        {
            set { isDirty = value; }
            get { return isDirty; }
        }
        //private Int32 squareIndex;

        public eClusterState maxClusterState = eClusterState.Free;

        //private UInt64 clusterBeginIndex = 0;
        //private UInt64 clusterEndIndex;

        public Int32[] numClusterStates;

    }
}
