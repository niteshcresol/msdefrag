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
        }

        //HACK: for refactoring only
        public Stream _LIST
        { get; set; }

        public IList<Stream> Streams
        { get; set; }
    }
}
