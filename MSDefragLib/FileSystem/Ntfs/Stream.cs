using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSDefragLib.FileSystem.Ntfs
{
    /// <summary>
    /// Class describing Stream
    /// 
    /// The NTFS scanner will construct an ItemStruct list in memory, but needs some
    /// extra information while constructing it. The following structs wrap the ItemStruct
    /// into a new struct with some extra info, discarded when the ItemStruct list is
    /// ready.
    /// 
    /// A single Inode can contain multiple streams of data. Every stream has it's own
    /// list of fragments. The name of a stream is the same as the filename plus two
    /// extensions separated by colons:
    /// 
    /// filename:"stream name":"stream type"
    /// 
    /// For example:
    ///   myfile.dat:stream1:$DATA
    ///   
    /// The "stream name" is an empty string for the default stream, which is the data
    /// of regular files. The "stream type" is one of the following strings:
    ///    0x10      $STANDARD_INFORMATION
    ///    0x20      $ATTRIBUTE_LIST
    ///    0x30      $FILE_NAME
    ///    0x40  NT  $VOLUME_VERSION
    ///    0x40  2K  $OBJECT_ID
    ///    0x50      $SECURITY_DESCRIPTOR
    ///    0x60      $VOLUME_NAME
    ///    0x70      $VOLUME_INFORMATION
    ///    0x80      $DATA
    ///    0x90      $INDEX_ROOT
    ///    0xA0      $INDEX_ALLOCATION
    ///    0xB0      $BITMAP
    ///    0xC0  NT  $SYMBOLIC_LINK
    ///    0xC0  2K  $REPARSE_POINT
    ///    0xD0      $EA_INFORMATION
    ///    0xE0      $EA
    ///    0xF0  NT  $PROPERTY_SET
    ///   0x100  2K  $LOGGED_UTILITY_STREAM
    /// </summary>
    public class Stream
    {
        public String StreamName;               /* "stream name" */

        public AttributeType StreamType;       /* "stream type" */

        /* The fragments of the stream. */
        public FragmentList Fragments { get; private set; }

        public UInt64 Clusters;                 /* Total number of clusters. */
        public UInt64 Bytes;                    /* Total number of bytes. */

        public Stream()
        {
            Fragments = new FragmentList();
        }
    }
}
