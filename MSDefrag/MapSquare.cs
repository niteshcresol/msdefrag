using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MSDefragLib;

namespace MSDefrag
{
    class MapSquare
    {
        public MapSquare(Int32 sqIndex)
        {
            squareIndex = sqIndex;
            maxClusterState = eClusterState.Free;

            numClusterStates = new Int32[(Int32)eClusterState.MaxValue];

            for (Int32 ii = 0; ii < numClusterStates.Count(); ii++ )
            {
                numClusterStates[ii] = 0;
            }

            Dirty = true;
        }

        private Boolean isDirty;
        public Boolean Dirty
        {
            set { isDirty = value; }
            get { return isDirty; }
        }

        private Int32 squareIndex;
        public Int32 SquareIndex { get { return squareIndex; } }

        public eClusterState maxClusterState;
        public Int32[] numClusterStates;
    }
}
