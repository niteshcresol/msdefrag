using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSDefrag
{
    class GuiSettings
    {
        public GuiSettings(UInt16 sqSize)
        {
            SquareSize = sqSize;
        }

        public UInt16 SquareSize;
    }
}
