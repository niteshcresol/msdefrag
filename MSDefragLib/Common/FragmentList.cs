using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSDefragLib.Common
{
    public class FragmentList
    {
        public FragmentList()
        {
        }

        public IList<Fragment> Fragments
        { get; private set; }
    }
}
