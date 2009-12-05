using System;
using System.IO;
using System.Diagnostics;
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
    [DebuggerDisplay("'{Name}':{Type},{Bytes}b")]
    public class Stream
    {
        public Stream(String name, AttributeType type)
        {
            Name = name;
            Type = type;
            Fragments = new FragmentList();
            Clusters = 0;
        }

        public void ParseRunData(BinaryReader runData, UInt64 startingVcn)
        {
            /* Walk through the RunData and add the extents. */
            Int64 Lcn = 0;
            UInt64 Vcn = startingVcn;
            UInt64 runLength;
            Int64 runOffset;
            while (RunData.Parse(runData, out runLength, out runOffset))
            {
                Lcn += runOffset;

                if (runOffset != 0)
                {
                    Clusters += runLength;
                }

                Fragments.Add(Lcn, Vcn, runLength, runOffset == 0);
                Vcn += runLength;
            }
        }

        /* "stream name" */
        public String Name
        { get; private set; }

        /* "stream type" */
        public AttributeType Type
        { get; private set; }

        /* The fragments of the stream. */
        public FragmentList Fragments
        { get; private set; }

        /* Total number of clusters. */
        public UInt64 Clusters
        { get; private set; }

        /* Total number of bytes. */
        public UInt64 Bytes
        { get; set; }

    }
}
