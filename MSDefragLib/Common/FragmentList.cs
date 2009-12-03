using System;
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

        public void Add(Fragment fragment)
        {
            _fragments.Add(fragment);
        }

        public UInt64 Lcn
        {
            get
            {
                Fragment fragment = _fragments.
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

                foreach (Fragment fragment in _fragments)
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
