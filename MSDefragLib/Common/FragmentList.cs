using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSDefragLib
{
    public class FragmentList : IEnumerable<Fragment>
    {
        public FragmentList()
        {
            _fragments = new List<Fragment>();
        }


        public void Add(Int64 lcn, UInt64 vcn, UInt64 length, Boolean isVirtual)
        {
            Debug.Assert(lcn >= 0);
            _fragments.Add(new Fragment((UInt64)lcn, vcn, length, isVirtual));
        }

        public Fragment FindContaining(UInt64 vcn)
        {
            foreach (Fragment fragment in _fragments)
            {
                if (fragment.IsLogical)
                {
                    if (fragment.NextVcn >= vcn)
                        return fragment;
                }
            }

            throw new Exception("Vcn not found for this fragment list, shall never occur");
        }


        public UInt64 Lcn
        {
            get
            {
                Fragment fragment = _fragments.FirstOrDefault(x => x.IsLogical);
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
                UInt64 nextLcn = 0;

                foreach (Fragment fragment in _fragments)
                {
                    if (fragment.IsLogical)
                    {
                        if ((nextLcn != 0) && (fragment.Lcn != nextLcn))
                            count++;
                        nextLcn = fragment.NextLcn;
                    }
                }

                if (nextLcn != 0)
                    count++;

                return count;
            }
        }

        public UInt64 TotalLength
        {
            get 
            {
                UInt64 sum = 0;
                foreach (Fragment fragment in _fragments)
                {
                    if (fragment.IsLogical)
                        sum += fragment.Length;
                }
                return sum;
            }
        }

        private IList<Fragment> _fragments;

        #region IEnumerable<Fragment> Members

        public IEnumerator<Fragment> GetEnumerator()
        {
            return _fragments.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
