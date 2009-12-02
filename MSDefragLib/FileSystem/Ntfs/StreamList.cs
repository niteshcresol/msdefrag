using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSDefragLib.FileSystem.Ntfs
{
    public class StreamList
    {
        public StreamList()
        {
            Streams = new List<Stream>();
        }

        public IList<Stream> Streams
        { get; private set; }
    }
}
