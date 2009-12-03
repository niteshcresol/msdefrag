using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSDefragLib.FileSystem.Ntfs
{
    public class StreamList : IEnumerable<Stream>
    {
        public StreamList()
        {
            _streams = new List<Stream>();
        }

        public void Add(Stream newStream)
        {
            _streams.Insert(0, newStream);
        }

        private IList<Stream> _streams;

        #region IEnumerable<Stream> Members

        public IEnumerator<Stream> GetEnumerator()
        {
            return _streams.GetEnumerator();
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
