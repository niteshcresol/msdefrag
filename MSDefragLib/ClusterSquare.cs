using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSDefragLib
{
    public class ClusterSquare
    {
        public ClusterSquare(Int32 squareIndex, UInt64 clusterBegin, UInt64 clusterEnd)
        {
            m_squareIndex = squareIndex;
            m_color = eClusterState.Free;
            m_clusterBeginIndex = clusterBegin;
            m_clusterEndIndex = clusterEnd;

            m_colors = new Int32[(Int32)eClusterState.MaxValue];

            m_isDirty = true;
        }

        private eClusterState GetMaxSquareColor()
        {
            if (m_colors[(Int32)eClusterState.Busy] > 0)
            {
                return eClusterState.Busy;
            }

            if (m_colors[(Int32)eClusterState.Mft] > 0)
            {
                return eClusterState.Mft;
            }

            if (m_colors[(Int32)eClusterState.Unmovable] > 0)
            {
                return eClusterState.Unmovable;
            }

            if (m_colors[(Int32)eClusterState.Fragmented] > 0)
            {
                return eClusterState.Fragmented;
            }

            if (m_colors[(Int32)eClusterState.Unfragmented] > 0)
            {
                return eClusterState.Unfragmented;
            }

            if (m_colors[(Int32)eClusterState.SpaceHog] > 0)
            {
                return eClusterState.SpaceHog;
            }

            if (m_colors[(Int32)eClusterState.Allocated] > 0)
            {
                return eClusterState.Allocated;
            }

            return eClusterState.Free;
        }

        public void SetMaxColor()
        {
            Int32 oldColor = (Int32)m_color;

            m_color = GetMaxSquareColor();

            if ((Int32)m_color != oldColor)
            {
                m_isDirty = true;
            }
        }

        public Boolean m_isDirty;
        public Int32 m_squareIndex;
        public eClusterState m_color;

        public UInt64 m_clusterBeginIndex;
        public UInt64 m_clusterEndIndex;

        public Int32[] m_colors = null;
    }

}
