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
            m_color = ClusterColors.COLOREMPTY;
            m_clusterBeginIndex = clusterBegin;
            m_clusterEndIndex = clusterEnd;

            m_colors = new Int32[(Int32)ClusterColors.COLORMAX];

            m_isDirty = true;
        }

        private ClusterColors GetMaxSquareColor()
        {
            if (m_colors[(Int32)ClusterColors.COLORBUSY] > 0)
            {
                return ClusterColors.COLORBUSY;
            }

            if (m_colors[(Int32)ClusterColors.COLORMFT] > 0)
            {
                return ClusterColors.COLORMFT;
            }

            if (m_colors[(Int32)ClusterColors.COLORUNMOVABLE] > 0)
            {
                return ClusterColors.COLORUNMOVABLE;
            }

            if (m_colors[(Int32)ClusterColors.COLORFRAGMENTED] > 0)
            {
                return ClusterColors.COLORFRAGMENTED;
            }

            if (m_colors[(Int32)ClusterColors.COLORUNFRAGMENTED] > 0)
            {
                return ClusterColors.COLORUNFRAGMENTED;
            }

            if (m_colors[(Int32)ClusterColors.COLORSPACEHOG] > 0)
            {
                return ClusterColors.COLORSPACEHOG;
            }

            if (m_colors[(Int32)ClusterColors.COLORALLOCATED] > 0)
            {
                return ClusterColors.COLORALLOCATED;
            }

            return ClusterColors.COLOREMPTY;
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
        public ClusterColors m_color;

        public UInt64 m_clusterBeginIndex;
        public UInt64 m_clusterEndIndex;

        public Int32[] m_colors = null;
    }

}
