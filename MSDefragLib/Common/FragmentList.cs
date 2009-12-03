using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSDefragLib
{
    public class FragmentList
    {
        public FragmentList()
        {
            Fragments = new List<Fragment>();
        }

        public IList<Fragment> Fragments
        { get; private set; }
    }
}
