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
            m_color = CLUSTER_COLORS.COLOREMPTY;
            m_clusterBeginIndex = clusterBegin;
            m_clusterEndIndex = clusterEnd;

            m_colors = new Int32[(Int32)CLUSTER_COLORS.COLORMAX];

            m_isDirty = true;
        }

        private CLUSTER_COLORS GetMaxSquareColor()
        {
            if (m_colors[(Int32)CLUSTER_COLORS.COLORBUSY] > 0)
            {
                return CLUSTER_COLORS.COLORBUSY;
            }

            if (m_colors[(Int32)CLUSTER_COLORS.COLORMFT] > 0)
            {
                return CLUSTER_COLORS.COLORMFT;
            }

            if (m_colors[(Int32)CLUSTER_COLORS.COLORUNMOVABLE] > 0)
            {
                return CLUSTER_COLORS.COLORUNMOVABLE;
            }

            if (m_colors[(Int32)CLUSTER_COLORS.COLORFRAGMENTED] > 0)
            {
                return CLUSTER_COLORS.COLORFRAGMENTED;
            }

            if (m_colors[(Int32)CLUSTER_COLORS.COLORUNFRAGMENTED] > 0)
            {
                return CLUSTER_COLORS.COLORUNFRAGMENTED;
            }

            if (m_colors[(Int32)CLUSTER_COLORS.COLORSPACEHOG] > 0)
            {
                return CLUSTER_COLORS.COLORSPACEHOG;
            }

            if (m_colors[(Int32)CLUSTER_COLORS.COLORALLOCATED] > 0)
            {
                return CLUSTER_COLORS.COLORALLOCATED;
            }

            return CLUSTER_COLORS.COLOREMPTY;
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
        public CLUSTER_COLORS m_color;

        public UInt64 m_clusterBeginIndex;
        public UInt64 m_clusterEndIndex;

        public Int32[] m_colors = null;
    }

}
