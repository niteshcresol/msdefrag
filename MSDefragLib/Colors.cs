using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSDefragLib
{
    public class Colors
    {
        public Colors(ClusterColors color)
        {
            m_color = color;
            m_numColors = 0;
        }

        public ClusterColors m_color;
        public Int64 m_numColors;
    }
}
