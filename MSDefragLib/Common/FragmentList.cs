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

        public UInt64 Lcn
        {
            get
            {
                Fragment fragment = Fragments.
                    FirstOrDefault(x => x.Lcn == Fragment.VIRTUALFRAGMENT);
                if (fragment == null)
                    return 0;
                return fragment.Lcn;
            }
        }

        public int FragmentCount
        {
            get
            {
                int count = 0;

                UInt64 Vcn = 0;
                UInt64 NextLcn = 0;

                foreach (Fragment fragment in Fragments)
                {
                    if (fragment.Lcn != Fragment.VIRTUALFRAGMENT)
                    {
                        if ((NextLcn != 0) && (fragment.Lcn != NextLcn))
                            count++;

                        NextLcn = fragment.Lcn + fragment.NextVcn - Vcn;
                    }

                    Vcn = fragment.NextVcn;
                }

                if (NextLcn != 0)
                    count++;

                return count;
            }
        }

        public IList<Fragment> Fragments
        { get; private set; }
    }
}
