using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSDefragLib.FileSystem.Ntfs
{
    [DebuggerDisplay("Streams: {Count}")]
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

        public int Count { get { return _streams.Count; } }

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
