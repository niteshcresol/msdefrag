using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace MSDefragLib
{
    class NTFS_BOOT_SECTOR
    {
        public UInt16 bytesPerSector;
        public Byte sectorPerCluster;
        public UInt16 reserved;
        public Byte[] reserved2 = new Byte[3];
        public UInt16 reserved3;
        public Byte mediaDescriptor;
        public UInt16 reserved4;
        public UInt16 unused;
        public UInt16 unused2;
        public UInt32 unused3;
        public UInt32 reserved5;
        public UInt32 unused4;
        public UInt64 totalSectors;
        public UInt64 mftLCN;
        public UInt64 mft2LCN;
        public Byte clustersPerMftRecord;
        public Byte[] unused5 = new Byte[3];
        public Byte clustersPerIndexBuffer;
        public Byte[] unused6 = new Byte[3];
        public UInt64 volumeSerialNumber;
        public UInt32 unused7;
    }

    class INODE_REFERENCE
    {
        public UInt32 InodeNumberLowPart;
        public UInt16 InodeNumberHighPart;

        public UInt16 SequenceNumber;
    };

    class NTFS_RECORD_HEADER
    {
        public UInt32 Type;                     /* File type, for example 'FILE' */

        public UInt16 UsaOffset;                /* Offset to the Update Sequence Array */
        public UInt16 UsaCount;                 /* Size in words of Update Sequence Array */

        public UInt64 Lsn;                      /* $LogFile Sequence Number (LSN) */
    };

    class FILE_RECORD_HEADER
    {
        public NTFS_RECORD_HEADER RecHdr;

        public UInt16 SequenceNumber;                  /* Sequence number */
        public UInt16 LinkCount;                       /* Hard link count */

        public UInt16 AttributeOffset;          /* Offset to the first Attribute */
        public UInt16 Flags;                    /* Flags. bit 1 = in use, bit 2 = directory, bit 4 & 8 = unknown. */

        public UInt32 BytesInUse;               /* Real size of the FILE record */

        public UInt32 BytesAllocated;                  /* Allocated size of the FILE record */

        public INODE_REFERENCE BaseFileRecord;  /* File reference to the base FILE record */

        public UInt16 NextAttributeNumber;             /* Next Attribute Id */
        public UInt16 Padding;                         /* Align to 4 UCHAR boundary (XP) */

        public UInt32 MFTRecordNumber;          /* Number of this MFT Record (XP) */

        public UInt16 UpdateSeqNum;                    /*  */
    };

    enum ATTRIBUTE_TYPE
    {
        AttributeInvalid = 0x00,                /* Not defined by Windows */
        AttributeStandardInformation = 0x10,
        AttributeAttributeList = 0x20,
        AttributeFileName = 0x30,
        AttributeObjectId = 0x40,
        AttributeSecurityDescriptor = 0x50,
        AttributeVolumeName = 0x60,
        AttributeVolumeInformation = 0x70,
        AttributeData = 0x80,
        AttributeIndexRoot = 0x90,
        AttributeIndexAllocation = 0xA0,
        AttributeBitmap = 0xB0,
        AttributeReparsePoint = 0xC0,           /* Reparse Point = Symbolic link */
        AttributeEAInformation = 0xD0,
        AttributeEA = 0xE0,
        AttributePropertySet = 0xF0,
        AttributeLoggedUtilityStream = 0x100,
        AttributeAll = 0xFF
    };

    class ATTRIBUTE
    {
        public ATTRIBUTE_TYPE AttributeType;
        public UInt32 Length;
        public Boolean Nonresident;
        public Byte NameLength;
        public UInt16 NameOffset;
        public UInt16 Flags;                    /* 0x0001 = Compressed, 0x4000 = Encrypted, 0x8000 = Sparse */
        public UInt16 AttributeNumber;
    };

    class RESIDENT_ATTRIBUTE
    {
        public ATTRIBUTE Attribute;
        public UInt32 ValueLength;
        public UInt16 ValueOffset;
        public UInt16 Flags;                           // 0x0001 = Indexed
    };

    class NONRESIDENT_ATTRIBUTE
    {
        public ATTRIBUTE Attribute;
        public UInt64 StartingVcn;
        public UInt64 LastVcn;
        public UInt16 RunArrayOffset;
        public Byte CompressionUnit;
        public List<Byte> AlignmentOrReserved/*[5]*/;
        public UInt64 AllocatedSize;
        public UInt64 DataSize;
        public UInt64 InitializedSize;
        public UInt64 CompressedSize;                  // Only when compressed
    };

    struct STANDARD_INFORMATION
    {
        public UInt64 CreationTime;
        public UInt64 FileChangeTime;
        public UInt64 MftChangeTime;
        public UInt64 LastAccessTime;
        public UInt32 FileAttributes;                  /* READ_ONLY=0x01, HIDDEN=0x02, SYSTEM=0x04, VOLUME_ID=0x08, ARCHIVE=0x20, DEVICE=0x40 */
        public UInt32 MaximumVersions;
        public UInt32 VersionNumber;
        public UInt32 ClassId;
        public UInt32 OwnerId;                         // NTFS 3.0 only
        public UInt32 SecurityId;                      // NTFS 3.0 only
        public UInt64 QuotaCharge;                     // NTFS 3.0 only
        public UInt64 Usn;                             // NTFS 3.0 only
    };

    class ATTRIBUTE_LIST
    {
        public ATTRIBUTE_TYPE AttributeType;
        public UInt16 Length;
        public Byte NameLength;
        public Byte NameOffset;
        public UInt64 LowestVcn;
        public INODE_REFERENCE FileReferenceNumber;
        public UInt16 Instance;
        public UInt16[] AlignmentOrReserved/*[3]*/;
    };

    class FILENAME_ATTRIBUTE
    {
        public INODE_REFERENCE ParentDirectory;
        public UInt64 CreationTime;
        public UInt64 ChangeTime;
        public UInt64 LastWriteTime;
        public UInt64 LastAccessTime;
        public UInt64 AllocatedSize;
        public UInt64 DataSize;
        public UInt32 FileAttributes;
        public UInt32 AlignmentOrReserved;
        public Byte NameLength;
        public Byte NameType;                   /* NTFS=0x01, DOS=0x02 */
        public String Name/*[1]*/;
    };

    /*
       The NTFS scanner will construct an ItemStruct list in memory, but needs some
       extra information while constructing it. The following structs wrap the ItemStruct
       into a new struct with some extra info, discarded when the ItemStruct list is
       ready.

       A single Inode can contain multiple streams of data. Every stream has it's own
       list of fragments. The name of a stream is the same as the filename plus two
       extensions separated by colons:
             filename:"stream name":"stream type"

       For example:
             myfile.dat:stream1:$DATA

       The "stream name" is an empty string for the default stream, which is the data
       of regular files. The "stream type" is one of the following strings:
          0x10      $STANDARD_INFORMATION
          0x20      $ATTRIBUTE_LIST
          0x30      $FILE_NAME
          0x40  NT  $VOLUME_VERSION
          0x40  2K  $OBJECT_ID
          0x50      $SECURITY_DESCRIPTOR
          0x60      $VOLUME_NAME
          0x70      $VOLUME_INFORMATION
          0x80      $DATA
          0x90      $INDEX_ROOT
          0xA0      $INDEX_ALLOCATION
          0xB0      $BITMAP
          0xC0  NT  $SYMBOLIC_LINK
          0xC0  2K  $REPARSE_POINT
          0xD0      $EA_INFORMATION
          0xE0      $EA
          0xF0  NT  $PROPERTY_SET
          0x100 2K  $LOGGED_UTILITY_STREAM
    */

    class StreamStruct
    {
	    public StreamStruct Next;

	    public String StreamName;               /* "stream name" */

        public ATTRIBUTE_TYPE StreamType;       /* "stream type" */

	    public FragmentListStruct Fragments;    /* The fragments of the stream. */

	    public UInt64 Clusters;                 /* Total number of clusters. */
	    public UInt64 Bytes;                    /* Total number of bytes. */
    };
    
    class InodeDataStruct
    {
        public UInt64 Inode;                    /* The Inode number. */
        public UInt64 ParentInode;              /* The Inode number of the parent directory. */

        public Boolean Directory;               /* true: it's a directory. */

        public String LongFilename;             /* Long filename. */
        public String ShortFilename;            /* Short filename (8.3 DOS). */

        public UInt64 Bytes;                    /* Total number of bytes. */
        public UInt64 CreationTime;             /* 1 second = 10000000 */
        public UInt64 MftChangeTime;
        public UInt64 LastAccessTime;

        public StreamStruct Streams;            /* List of StreamStruct. */
        public FragmentListStruct MftDataFragments;   /* The Fragments of the $MFT::$DATA stream. */

        public UInt64 MftDataBytes;             /* Length of the $MFT::$DATA. */

        public FragmentListStruct MftBitmapFragments; /* The Fragments of the $MFT::$BITMAP stream. */

        public UInt64 MftBitmapBytes;           /* Length of the $MFT::$BITMAP. */
    };

    class NtfsDiskInfoStruct
    {
        public NtfsDiskInfoStruct()
        {
            buffers = new Buffers();
        }

        public UInt64 BytesPerSector;
        public UInt64 SectorsPerCluster;
        public UInt64 TotalSectors;
        public UInt64 MftStartLcn;
        public UInt64 Mft2StartLcn;
        public UInt64 BytesPerMftRecord;
        public UInt64 ClustersPerIndexRecord;

        Buffers buffers;
    };

    class Buffers
    {
        public Buffers()
        {
            Buffer = new List<Byte>();
        }

        List<Byte> Buffer;
        UInt64 Offset;
        int Age;
    } ;

    class UlongBytes
    {
        public Byte[] Bytes = new Byte[8];

        public UInt64 Value
        {

            set {
                for (int ii = 0; ii < 8; ii++)
                {
                    Bytes[ii] = 0;
                }
            }

            get
            {

                UInt64 val = 0;

                for (int i = 7; i >= 0; i--)
                {
                     val = (val << 8) | Bytes[i];
                }

                return val;
            }
        }
        /*
                struct
                {
                    BYTE Bytes[8];
                };
			
                LONG64 Value;
        */
    };

    class ByteArray
    {
        public Byte[] m_bytes;

        public Int64 GetLength()
        {
            return m_bytes.Length;
        }

        public Byte GetValue(Int64 index)
        {
            Byte val = 0;

            if (index < m_bytes.Length)
            {
                val = m_bytes[index];
            }

            return val;
        }

        public void SetValue(Int64 index, Byte value)
        {
            if (index < m_bytes.Length)
            {
                m_bytes[index] = value;
            }
        }

        public void Initialize(Int64 length)
        {
            m_bytes = new Byte[length];
        }

        public ByteArray ToByteArray(Int64 index, Int64 length)
        {
            ByteArray ba = new ByteArray();

            ba.m_bytes = new Byte[length];

            if (m_bytes.Length < index || m_bytes.Length < index + length)
                return ba;

            for (int ii = 0; ii < length; ii++)
            {
                ba.SetValue(ii, GetValue(index + ii));
            }

            return ba;
        }

        public UInt16Array ToUInt16Array(Int64 index, Int64 length)
        {
            UInt16Array ba = new UInt16Array();

            //ba.m_words = new UInt16[length / 2];
            ba.Initialize(length / 2);

            if (m_bytes.Length < index || m_bytes.Length < index + length)
                return ba;

            int jj = 0;

            for (int ii = 0; ii < length; )
            {
                Byte val = GetValue(index + ii++);
                Byte val2 = GetValue(index + ii++);

                ba.SetValue(jj++, (UInt16)(val2 << (1 << 3) | val));
            }

            return ba;
        }

        public Byte ToByte(ref Int64 offset)
        {
            Byte retValue = 0;

            for (Int64 ii = sizeof(Byte) - 1; ii >= 0; ii--)
            {
                Byte val = GetValue(offset + ii);
                
                retValue = (Byte)((retValue << (sizeof(Byte) << 3)) | val);
            }

            offset += sizeof(Byte);

            return retValue;
        }

        public Boolean ToBoolean(ref Int64 offset)
        {
            Boolean retValue = false;

            Byte val = ToByte(ref offset);

            retValue = (val != 0);

            return retValue;
        }

        public UInt16 ToUInt16(ref Int64 offset)
        {
            UInt16 retValue = 0;

            for (Int64 ii = sizeof(UInt16) - 1; ii >= 0; ii--)
            {
                Byte val = GetValue(offset + ii);

                retValue = (UInt16)((retValue << (sizeof(Byte) << 3)) | val);
            }

            offset += sizeof(UInt16);

            return retValue;
        }

        public UInt32 ToUInt32(ref Int64 offset)
        {
            UInt32 retValue = 0;

            for (Int64 ii = sizeof(UInt32) - 1; ii >= 0; ii--)
            {
                Byte val = GetValue(offset + ii);

                retValue = (retValue << (sizeof(Byte) << 3)) | val;
            }

            offset += sizeof(UInt32);

            return retValue;
        }

        public UInt64 ToUInt64(ref Int64 offset)
        {
            UInt64 retValue = 0;

            for (Int64 ii = sizeof(UInt64) - 1; ii >= 0; ii--)
            {
                Byte val = GetValue(offset + ii);

                retValue = (retValue << (sizeof(Byte) << 3)) | val;
            }

            offset += sizeof(UInt64);

            return retValue;
        }

        public NTFS_RECORD_HEADER ToNTFS_RECORD_HEADER(ref Int64 offset)
        {
            NTFS_RECORD_HEADER nrh = new NTFS_RECORD_HEADER();

            nrh.Type = ToUInt32(ref offset);
            nrh.UsaOffset = ToUInt16(ref offset);
            nrh.UsaCount = ToUInt16(ref offset);
            nrh.Lsn = ToUInt64(ref offset);

            return nrh;
        }

        public INODE_REFERENCE ToINODE_REFERENCE(ref Int64 offset)
        {
            INODE_REFERENCE ir = new INODE_REFERENCE();

            ir.InodeNumberLowPart = ToUInt32(ref offset);
            ir.InodeNumberHighPart = ToUInt16(ref offset);
            ir.SequenceNumber = ToUInt16(ref offset);

            return ir;
        }

        public FILE_RECORD_HEADER ToFILE_RECORD_HEADER(ref Int64 offset)
        {
            FILE_RECORD_HEADER frh = new FILE_RECORD_HEADER();

            frh.RecHdr = ToNTFS_RECORD_HEADER(ref offset);

            frh.SequenceNumber = ToUInt16(ref offset);
            frh.LinkCount = ToUInt16(ref offset);
            frh.AttributeOffset = ToUInt16(ref offset);
            frh.Flags = ToUInt16(ref offset);
            frh.BytesInUse = ToUInt32(ref offset);
            frh.BytesAllocated = ToUInt32(ref offset);

            frh.BaseFileRecord = ToINODE_REFERENCE(ref offset);

            frh.NextAttributeNumber = ToUInt16(ref offset);
            frh.Padding = ToUInt16(ref offset);
            frh.MFTRecordNumber = ToUInt32(ref offset);
            frh.UpdateSeqNum = ToUInt16(ref offset);

            return frh;
        }

        public ATTRIBUTE_TYPE ToATTRIBUTE_TYPE(ref Int64 offset)
        {
            ATTRIBUTE_TYPE at = new ATTRIBUTE_TYPE();

            UInt32 val = ToUInt32(ref offset); // TODO: Check if this is correct

            switch (val)
            {
                case 0x00: at = ATTRIBUTE_TYPE.AttributeInvalid; break;
                case 0x10: at = ATTRIBUTE_TYPE.AttributeStandardInformation; break;
                case 0x20: at = ATTRIBUTE_TYPE.AttributeAttributeList; break;
                case 0x30: at = ATTRIBUTE_TYPE.AttributeFileName; break;
                case 0x40: at = ATTRIBUTE_TYPE.AttributeObjectId; break;
                case 0x50: at = ATTRIBUTE_TYPE.AttributeSecurityDescriptor; break;
                case 0x60: at = ATTRIBUTE_TYPE.AttributeVolumeName; break;
                case 0x70: at = ATTRIBUTE_TYPE.AttributeVolumeInformation; break;
                case 0x80: at = ATTRIBUTE_TYPE.AttributeData; break;
                case 0x90: at = ATTRIBUTE_TYPE.AttributeIndexRoot; break;
                case 0xA0: at = ATTRIBUTE_TYPE.AttributeIndexAllocation; break;
                case 0xB0: at = ATTRIBUTE_TYPE.AttributeBitmap; break;
                case 0xC0: at = ATTRIBUTE_TYPE.AttributeReparsePoint; break;
                case 0xD0: at = ATTRIBUTE_TYPE.AttributeEAInformation; break;
                case 0xE0: at = ATTRIBUTE_TYPE.AttributeEA; break;
                case 0xF0: at = ATTRIBUTE_TYPE.AttributePropertySet; break;
                case 0x100: at = ATTRIBUTE_TYPE.AttributeLoggedUtilityStream; break;
                case 0xFF: at = ATTRIBUTE_TYPE.AttributeAll; break;
            }

            return at;
        }

        public ATTRIBUTE ToATTRIBUTE(ref Int64 offset)
        {
            ATTRIBUTE attr = new ATTRIBUTE();

            attr.AttributeType = ToATTRIBUTE_TYPE(ref offset);

            attr.Length = ToUInt32(ref offset);
            attr.Nonresident = ToBoolean(ref offset);
            attr.NameLength = ToByte(ref offset);
            attr.NameOffset = ToUInt16(ref offset);
            attr.Flags = ToUInt16(ref offset);
            attr.AttributeNumber = ToUInt16(ref offset);

            return attr;
        }

        public RESIDENT_ATTRIBUTE ToRESIDENT_ATTRIBUTE(ref Int64 offset)
        {
            RESIDENT_ATTRIBUTE attr = new RESIDENT_ATTRIBUTE();

            attr.Attribute = ToATTRIBUTE(ref offset);
            attr.ValueLength = ToUInt32(ref offset);
            attr.ValueOffset = ToUInt16(ref offset);
            attr.Flags = ToUInt16(ref offset);

            return attr;
        }

        public NONRESIDENT_ATTRIBUTE ToNONRESIDENT_ATTRIBUTE(ref Int64 offset)
        {
            NONRESIDENT_ATTRIBUTE attr = new NONRESIDENT_ATTRIBUTE();

            attr.Attribute = ToATTRIBUTE(ref offset);
            attr.StartingVcn = ToUInt64(ref offset);
            attr.LastVcn = ToUInt64(ref offset);
            attr.RunArrayOffset = ToUInt16(ref offset);
            attr.CompressionUnit = ToByte(ref offset);
            attr.AlignmentOrReserved = new List<Byte>();
            attr.AlignmentOrReserved.Add(ToByte(ref offset));
            attr.AlignmentOrReserved.Add(ToByte(ref offset));
            attr.AlignmentOrReserved.Add(ToByte(ref offset));
            attr.AlignmentOrReserved.Add(ToByte(ref offset));
            attr.AlignmentOrReserved.Add(ToByte(ref offset));
            attr.AllocatedSize = ToUInt64(ref offset);
            attr.DataSize = ToUInt64(ref offset);
            attr.InitializedSize = ToUInt64(ref offset);
            attr.CompressedSize = ToUInt64(ref offset);

            return attr;
        }

        public FILENAME_ATTRIBUTE ToFILENAME_ATTRIBUTE(ref Int64 offset)
        {
            FILENAME_ATTRIBUTE fa = new FILENAME_ATTRIBUTE();

            fa.ParentDirectory = ToINODE_REFERENCE(ref offset);
            fa.CreationTime = ToUInt64(ref offset);
            fa.ChangeTime = ToUInt64(ref offset);
            fa.LastWriteTime = ToUInt64(ref offset);
            fa.LastAccessTime = ToUInt64(ref offset);
            fa.AllocatedSize = ToUInt64(ref offset);
            fa.DataSize = ToUInt64(ref offset);
            fa.FileAttributes = ToUInt32(ref offset);
            fa.AlignmentOrReserved = ToUInt32(ref offset);
            fa.NameLength = ToByte(ref offset);
            fa.NameType = ToByte(ref offset);
            fa.Name = "";

            for (int ii = 0; ii < fa.NameLength; ii++)
            {
                fa.Name += (Char)ToUInt16(ref offset); // TODO: Check this
            }

            return fa;
        }

        public STANDARD_INFORMATION ToSTANDARD_INFORMATION(ref Int64 offset)
        {
            STANDARD_INFORMATION si = new STANDARD_INFORMATION();

            si.CreationTime = ToUInt64(ref offset);
            si.FileChangeTime = ToUInt64(ref offset);
            si.MftChangeTime = ToUInt64(ref offset);
            si.LastAccessTime = ToUInt64(ref offset);
            si.FileAttributes = ToUInt32(ref offset);
            si.MaximumVersions = ToUInt32(ref offset);
            si.VersionNumber = ToUInt32(ref offset);
            si.ClassId = ToUInt32(ref offset);
            si.OwnerId = ToUInt32(ref offset);
            si.SecurityId = ToUInt32(ref offset);
            si.QuotaCharge = ToUInt64(ref offset);
            si.Usn = ToUInt64(ref offset);

            return si;
        }
/*

        public ATTRIBUTE_TYPE AttributeType;
        public UInt16 Length;
        public Byte NameLength;
        public Byte NameOffset;
        public UInt64 LowestVcn;
        public INODE_REFERENCE FileReferenceNumber;
        public UInt16 Instance;
        List<UInt16> AlignmentOrReserved/ *[3]* /;

*/
        public ATTRIBUTE_LIST ToATTRIBUTE_LIST(ref Int64 offset)
        {
            ATTRIBUTE_LIST al = new ATTRIBUTE_LIST();

            al.AttributeType = ToATTRIBUTE_TYPE(ref offset);
            al.Length = ToUInt16(ref offset);
            al.NameLength = ToByte(ref offset);
            al.NameOffset = ToByte(ref offset);
            al.LowestVcn = ToUInt64(ref offset);
            al.FileReferenceNumber = ToINODE_REFERENCE(ref offset);
            al.Instance = ToUInt16(ref offset);
            al.AlignmentOrReserved = new UInt16[3];
            return al;
        }

        public NTFS_BOOT_SECTOR ToNTFS_BOOT_SECTOR(ref Int64 offset)
        {
            NTFS_BOOT_SECTOR nfs = new NTFS_BOOT_SECTOR();

            nfs.bytesPerSector = ToUInt16(ref offset);
            nfs.sectorPerCluster = ToByte(ref offset);
            nfs.reserved = ToUInt16(ref offset);
            nfs.reserved2[0] = ToByte(ref offset);
            nfs.reserved2[1] = ToByte(ref offset);
            nfs.reserved2[2] = ToByte(ref offset);
            nfs.reserved3 = ToUInt16(ref offset);
            nfs.mediaDescriptor = ToByte(ref offset);
            nfs.reserved4 = ToUInt16(ref offset);
            nfs.unused = ToUInt16(ref offset);
            nfs.unused2 = ToUInt16(ref offset);
            nfs.unused3 = ToUInt32(ref offset);
            nfs.reserved5 = ToUInt32(ref offset);
            nfs.unused4 = ToUInt32(ref offset);
            nfs.totalSectors = ToUInt64(ref offset);
            nfs.mftLCN = ToUInt64(ref offset);
            nfs.mft2LCN = ToUInt64(ref offset);
            nfs.clustersPerMftRecord = ToByte(ref offset);
            nfs.unused5[0] = ToByte(ref offset);
            nfs.unused5[1] = ToByte(ref offset);
            nfs.unused5[2] = ToByte(ref offset);
            nfs.clustersPerIndexBuffer = ToByte(ref offset);
            nfs.unused6[0] = ToByte(ref offset);
            nfs.unused6[1] = ToByte(ref offset);
            nfs.unused6[2] = ToByte(ref offset);
            nfs.volumeSerialNumber = ToUInt64(ref offset);
            nfs.unused7 = ToUInt32(ref offset);

            return nfs;
        }
    }

    class UInt16Array
    {
        private UInt16[] m_words;

        public ByteArray ToByteArray(Int64 index, Int64 length)
        {
            ByteArray ba = new ByteArray();

            //ba.m_bytes = new Byte[length << 1 + 1];
            ba.Initialize(length << 1 + 1);

            if (m_words.Length < index || m_words.Length < index + length)
                return ba;

            int jj = 0;

            for (int ii = 0; ii < length; ii++)
            {
                UInt16 val = GetValue(index + ii);

                ba.SetValue(jj++, (Byte)(val & Byte.MaxValue));
                ba.SetValue(jj++, (Byte)(val >> (1 >> 3)));
            }

            return ba;
        }

        public Int64 GetLength()
        {
            return m_words.Length;
        }

        public UInt16 GetValue(Int64 index)
        {
            UInt16 val = 0;

            if (index < m_words.Length)
            {
                val = m_words[index];
            }

            return val;
        }

        public void SetValue(Int64 index, UInt16 value)
        {
            if (index < m_words.Length)
            {
                m_words[index] = value;
            }
        }

        public void Initialize(Int64 length)
        {
            m_words = new UInt16[length];
        }
    }

    class MSScanNtfs
    {
        const UInt64 MFTBUFFERSIZE = 256 * 1024;
        const UInt64 VIRTUALFRAGMENT = UInt64.MaxValue;

        [StructLayout(LayoutKind.Sequential)]
        private struct OVERLAPPED
        {
            IntPtr Internal;
            IntPtr InternalHigh;
            public UInt32 Offset;
            public UInt32 OffsetHigh;
            public UIntPtr hEvent;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ULARGE_INTEGER
        {
            public UInt32 LowPart;
            public UInt32 HighPart;
        };

        private MSDefragLib m_msDefragLib;

        public MSScanNtfs(MSDefragLib lib)
        {
            m_msDefragLib = lib;
        }

        public delegate void ShowDebugHandler(object sender, EventArgs e);

        public event ShowDebugHandler ShowDebugEvent;

        protected virtual void OnShowDebug(EventArgs e)
        {
            if (ShowDebugEvent != null)
                ShowDebugEvent(this, e);
        }

        String m_lastMessage;

        public void ShowDebug(int level, String output)
        {
            m_lastMessage = output;

            //if (level < 6)
            //    System.Console.WriteLine(output);
            EventArgs e = new EventArgs();

            OnShowDebug(EventArgs.Empty);
        }

        public String GetLastMessage()
        {
            return m_lastMessage;
        }

        String StreamTypeNames(ATTRIBUTE_TYPE StreamType)
        {
	        switch (StreamType)
	        {
                case ATTRIBUTE_TYPE.AttributeStandardInformation:
                    return ("$STANDARD_INFORMATION");
                case ATTRIBUTE_TYPE.AttributeAttributeList:
                    return ("$ATTRIBUTE_LIST");
                case ATTRIBUTE_TYPE.AttributeFileName:
                    return ("$FILE_NAME");
                case ATTRIBUTE_TYPE.AttributeObjectId:
                    return ("$OBJECT_ID");
                case ATTRIBUTE_TYPE.AttributeSecurityDescriptor:
                    return ("$SECURITY_DESCRIPTOR");
                case ATTRIBUTE_TYPE.AttributeVolumeName:
                    return ("$VOLUME_NAME");
                case ATTRIBUTE_TYPE.AttributeVolumeInformation:
                    return ("$VOLUME_INFORMATION");
                case ATTRIBUTE_TYPE.AttributeData:
                    return ("$DATA");
                case ATTRIBUTE_TYPE.AttributeIndexRoot:
                    return ("$INDEX_ROOT");
                case ATTRIBUTE_TYPE.AttributeIndexAllocation:
                    return ("$INDEX_ALLOCATION");
                case ATTRIBUTE_TYPE.AttributeBitmap:
                    return ("$BITMAP");
                case ATTRIBUTE_TYPE.AttributeReparsePoint:
                    return ("$REPARSE_POINT");
                case ATTRIBUTE_TYPE.AttributeEAInformation:
                    return ("$EA_INFORMATION");
                case ATTRIBUTE_TYPE.AttributeEA:
                    return ("$EA");
                case ATTRIBUTE_TYPE.AttributePropertySet:
                    return ("$PROPERTY_SET");               /* guess, not documented */
                case ATTRIBUTE_TYPE.AttributeLoggedUtilityStream:
                    return ("$LOGGED_UTILITY_STREAM");
	            default:
                    return("");
	        }
        }

        /*
            Fixup the raw MFT data that was read from disk. Return TRUE if everything is ok,
            FALSE if the MFT data is corrupt (this can also happen when we have read a
            record past the end of the MFT, maybe it has shrunk while we were processing).

            - To protect against disk failure, the last 2 bytes of every sector in the MFT are
            not stored in the sector itself, but in the "Usa" array in the header (described
            by UsaOffset and UsaCount). The last 2 bytes are copied into the array and the
            Update Sequence Number is written in their place.

            -   The Update Sequence Number is stored in the first item (item zero) of the "Usa"
                array.

            -   The number of bytes per sector is defined in the $Boot record.
        */
        Boolean FixupRawMftdata(
                    MSDefragDataStruct Data,
                    NtfsDiskInfoStruct DiskInfo,
                    ByteArray Buffer,
                    UInt64 BufLength)
        {
	        NTFS_RECORD_HEADER RecordHeader;

	        UInt16Array BufferW;
            UInt16Array UpdateSequenceArray;
	        Int64 Index;
	        Int64 Increment;

	        UInt16 i;

	        /* Sanity check. */
	        if (Buffer == null) return false;

            String recordType = "";

            for (Index = 0; Index < 4; Index++ )
            {
                recordType += Convert.ToChar(Buffer.GetValue(Index));
            }

            /* If this is not a FILE record then return FALSE. */
            if (recordType.CompareTo("FILE") != 0)
            {
                ShowDebug(2, "This is not a valid MFT record, it does not begin with FILE (maybe trying to read past the end?).");

                //m_msDefragLib.ShowHex(Data, Buffer.m_bytes, BufLength);

                return false;
            }

            /*
                Walk through all the sectors and restore the last 2 bytes with the value
                from the Usa array. If we encounter bad sector data then return with FALSE. 
            */
            BufferW = Buffer.ToUInt16Array(0, Buffer.GetLength());

            Int64 tempOffset = 0;
            RecordHeader = Buffer.ToNTFS_RECORD_HEADER(ref tempOffset);

            UpdateSequenceArray = Buffer.ToUInt16Array(RecordHeader.UsaOffset, Buffer.GetLength() - RecordHeader.UsaOffset);

            Increment = (Int64)(DiskInfo.BytesPerSector / sizeof(UInt16));

            Index = Increment - 1;

            for (i = 1; i < RecordHeader.UsaCount; i++)
            {
                /* Check if we are inside the buffer. */
                if (Index * sizeof(UInt16) >= (Int64)BufLength)
                {
                    ShowDebug(2, "Warning: USA data indicates that data is missing, the MFT may be corrupt.");

                    return false;
                }

                /*
                    Check if the last 2 bytes of the sector contain the Update Sequence Number.
                    If not then return FALSE.
                */
                if (BufferW.GetValue(Index) - UpdateSequenceArray.GetValue(0) != 0)
                {
                    ShowDebug(2, "Error: USA fixup word is not equal to the Update Sequence Number, the MFT may be corrupt.");

                    return false;
                }

                /* Replace the last 2 bytes in the sector with the value from the Usa array. */
                BufferW.SetValue(Index, UpdateSequenceArray.GetValue(i));

                Index += Increment;
	        }

            Buffer = BufferW.ToByteArray(0, BufferW.GetLength());

	        return true;
        }

        /*
            Read the data that is specified in a RunData list from disk into memory,
            skipping the first Offset bytes. Return a malloc'ed buffer with the data,
            or null if error.

            Note: The caller must free() the buffer.
        */
        ByteArray ReadNonResidentData(
			MSDefragDataStruct Data,
			NtfsDiskInfoStruct DiskInfo,
			ByteArray RunData,
			UInt64 RunDataLength,
			UInt64 Offset,                    /* Bytes to skip from begin of data. */
			UInt64 WantedLength)              /* Number of bytes to read. */
        {
	        UInt64 Index;

	        ByteArray Buffer = new ByteArray();
            //ByteArray Buffer = new Byte[WantedLength];

	        UInt64 Lcn;
	        UInt64 Vcn;

	        int RunOffsetSize;
	        int RunLengthSize;

            UlongBytes RunOffset = new UlongBytes();
            UlongBytes RunLength = new UlongBytes();

	        UInt64 ExtentVcn;
	        UInt64 ExtentLcn;
	        UInt64 ExtentLength;

	        IOWrapper.OVERLAPPED gOverlapped = new IOWrapper.OVERLAPPED();

	        ULARGE_INTEGER Trans;

            Int32 BytesRead = 0;

            //Boolean Result;

            //String s1;

	        Int16 i;

            //JKDefragGui *jkGui = JKDefragGui::getInstance();

            ShowDebug(6, String.Format("    Reading {0:G} bytes from offset {0:G}", WantedLength, Offset));

	        /* Sanity check. */
            if ((RunData == null) || (RunDataLength == 0)) return null;

	        if (WantedLength >= UInt32.MaxValue)
	        {
                ShowDebug(2, String.Format("    Cannot read {0:G} bytes, maximum is {1:G}.", WantedLength, UInt32.MaxValue));

		        return null;
	        }

	        /* 
                We have to round up the WantedLength to the nearest sector. For some
	            reason or other Microsoft has decided that raw reading from disk can
	            only be done by whole sector, even though ReadFile() accepts it's
	            parameters in bytes.
            */
	        if (WantedLength % DiskInfo.BytesPerSector > 0)
	        {
		        WantedLength = WantedLength + DiskInfo.BytesPerSector - WantedLength % DiskInfo.BytesPerSector;
	        }

	        /*
                Allocate the data buffer. Clear the buffer with zero's in case of sparse
	            content.
            */
//            Buffer.Initialize();

	        /* Walk through the RunData and read the requested data from disk. */
	        Index = 0;
	        Lcn = 0;
	        Vcn = 0;

            Byte runDataValue = 0;

            while ((runDataValue = (Byte)RunData.GetValue((Int64)Index)) != 0)
	        {
		        /* Decode the RunData and calculate the next Lcn. */
                RunLengthSize = (runDataValue & 0x0F);
                RunOffsetSize = ((runDataValue & 0xF0) >> 4);

		        Index++;

		        if (Index >= RunDataLength)
		        {
                    ShowDebug(2, "Error: datarun is longer than buffer, the MFT may be corrupt.");
		
			        return null;
		        }

		        RunLength.Bytes = new Byte[8];

		        for (i = 0; i < RunLengthSize; i++)
		        {
                    RunLength.Bytes[i] = runDataValue;
		
			        Index++;

			        if (Index >= RunDataLength)
			        {
                        ShowDebug(2, "Error: datarun is longer than buffer, the MFT may be corrupt.");
			
					    return null;
			        }
		        }

		        RunOffset.Bytes = new Byte[8];

		        for (i = 0; i < RunOffsetSize; i++)
		        {
                    RunOffset.Bytes[i] = runDataValue;
		
			        Index++;

			        if (Index >= RunDataLength)
			        {
                        ShowDebug(2, "Error: datarun is longer than buffer, the MFT may be corrupt.");
			
				        return null;
			        }
		        }

                if (RunOffset.Bytes[i - 1] >= 0x80)
                {
                    while (i < 8) RunOffset.Bytes[i++] = 0xFF;
                }

		        Lcn += RunOffset.Value;
		        Vcn += RunLength.Value;

		        /* Ignore virtual extents. */
		        if (RunOffset.Bytes == null) continue;

		        /* I don't think the RunLength can ever be zero, but just in case. */
		        if (RunLength.Bytes == null) continue;

		        /*  
                    Determine how many and which bytes we want to read. If we don't need
		            any bytes from this extent then loop. 
                */

		        ExtentVcn = (Vcn - RunLength.Value) * DiskInfo.BytesPerSector * DiskInfo.SectorsPerCluster;
                ExtentLcn = Lcn * DiskInfo.BytesPerSector * DiskInfo.SectorsPerCluster;

		        ExtentLength = RunLength.Value * DiskInfo.BytesPerSector * DiskInfo.SectorsPerCluster;

		        if (Offset >= ExtentVcn + ExtentLength) continue;

		        if (Offset > ExtentVcn)
		        {
			        ExtentLcn = ExtentLcn + Offset - ExtentVcn;
			        ExtentLength = ExtentLength - (Offset - ExtentVcn);
			        ExtentVcn = Offset;
		        }

		        if (Offset + WantedLength <= ExtentVcn) continue;

		        if (Offset + WantedLength < ExtentVcn + ExtentLength)
		        {
			        ExtentLength = Offset + WantedLength - ExtentVcn;
		        }
		
		        if (ExtentLength == 0) continue;

		        /* Read the data from the disk. If error then return FALSE. */

                ShowDebug(6, String.Format("    Reading {0:G} bytes from Lcn={1:G} into offset={2:G}",
                    ExtentLength, ExtentLcn / (DiskInfo.BytesPerSector * DiskInfo.SectorsPerCluster),
                	ExtentVcn - Offset));

                Trans.HighPart = (UInt32)(ExtentLcn >> 32);
                Trans.LowPart = (UInt32)(ExtentLcn & UInt32.MaxValue);

		        gOverlapped.Offset     = Trans.LowPart;
		        gOverlapped.OffsetHigh = Trans.HighPart;
		        gOverlapped.hEvent     = UIntPtr.Zero;

                Byte[] Buffer2 = new Byte[ExtentLength];

                BytesRead = IOWrapper.Read(Data.Disk.VolumeHandle, Buffer.m_bytes, (Int32)(ExtentVcn - Offset), (Int32)ExtentLength, gOverlapped);
                //Result = IOWrapper.ReadFile(Data.Disk.VolumeHandle, Buffer2, ExtentLength, BytesRead, gOverlapped);
                //Result = IOWrapper.ReadFile(Data.Disk.VolumeHandle, Buffer[ExtentVcn - Offset], ExtentLength, BytesRead, gOverlapped);

                if (BytesRead <= 0)
                {
                    String errorMessage = m_msDefragLib.SystemErrorStr(Marshal.GetLastWin32Error());

                    ShowDebug(2, "Error while reading disk: " + errorMessage);

                    return null;
                }
	        }

	        /* Return the buffer. */
	        return(Buffer);
        }

        /* Read the RunData list and translate into a list of fragments. */
        Boolean TranslateRundataToFragmentlist(
			        MSDefragDataStruct Data,
			        InodeDataStruct InodeData,
			        String StreamName,
			        ATTRIBUTE_TYPE StreamType,
			        ByteArray RunData,
			        UInt64 RunDataLength,
			        UInt64 StartingVcn,
			        UInt64 Bytes)
        {
	        StreamStruct Stream;

    	    UInt32 Index;

	        UInt64 Lcn;
	        UInt64 Vcn;

	        int RunOffsetSize;
	        int RunLengthSize;

            UlongBytes RunOffset = new UlongBytes();
            UlongBytes RunLength = new UlongBytes();

	        FragmentListStruct NewFragment;
	        FragmentListStruct LastFragment;

	        int i;

            //	JKDefragGui *jkGui = JKDefragGui::getInstance();

	        /* Sanity check. */
	        if ((Data == null) || (InodeData == null)) return false;

	        /* Find the stream in the list of streams. If not found then create a new stream. */
	        for (Stream = InodeData.Streams; Stream != null; Stream = Stream.Next)
	        {
                if (Stream.StreamType != StreamType)
                {
                    continue;
                }

                if ((StreamName == null) && (Stream.StreamName == null))
                {
                    break;
                }

                if ((StreamName != null) && (Stream.StreamName != null) &&
                    (Stream.StreamName.CompareTo(StreamName) == 0))
                {
                    break;
                }
	        }

            if (Stream == null)
	        {
                if (StreamName != null)
		        {
                    ShowDebug(6, "    Creating new stream: '" + StreamName + ":" + StreamTypeNames(StreamType) + "'");
		        }
		        else
		        {
                    ShowDebug(6, "    Creating new stream: ':" + StreamTypeNames(StreamType) + "'");
		        }

		        Stream = new StreamStruct();

		        Stream.Next = InodeData.Streams;

		        InodeData.Streams = Stream;

		        Stream.StreamName = null;

		        if ((StreamName != null) && (StreamName.Length > 0))
		        {
			        Stream.StreamName = StreamName;
		        }

		        Stream.StreamType = StreamType;
		        Stream.Fragments = null;
		        Stream.Clusters = 0;
		        Stream.Bytes = Bytes;
	        }
	        else
	        {
		        if (StreamName != null)
		        {
                    ShowDebug(6, "    Appending rundata to existing stream: '" + StreamName + ":" + StreamTypeNames(StreamType));
		        }
		        else
		        {
                    ShowDebug(6, "    Appending rundata to existing stream: ':" + StreamTypeNames(StreamType));
		        }

		        if (Stream.Bytes == 0) Stream.Bytes = Bytes;
	        }

	        /* If the stream already has a list of fragments then find the last fragment. */
	        LastFragment = Stream.Fragments;

            if (LastFragment != null)
	        {
		        while (LastFragment.Next != null) LastFragment = LastFragment.Next;
	
		        if (StartingVcn != LastFragment.NextVcn)
		        {
                    ShowDebug(2, String.Format("Error: Inode {0:G} already has a list of fragments. LastVcn={1:G}, StartingVCN={2:G}",
                      InodeData.Inode, LastFragment.NextVcn, StartingVcn));

			        return false;
		        }
	        }

	        /* Walk through the RunData and add the extents. */
	        Index = 0;

	        Lcn = 0;

	        Vcn = StartingVcn;

            if (RunData != null)
            {
                Int64 tempOffset = Index;

                while (RunData.ToByte(ref tempOffset) != 0)
                {
                    tempOffset = Index;
                    Byte runDataValue = RunData.ToByte(ref tempOffset);

                    /* Decode the RunData and calculate the next Lcn. */
                    RunLengthSize = (runDataValue & 0x0F);
                    RunOffsetSize = ((runDataValue & 0xF0) >> 4);

                    Index++;

                    if (Index >= RunDataLength)
                    {
                        ShowDebug(2, String.Format("Error: datarun is longer than buffer, the MFT may be corrupt. Inode {0:G}.",
                            InodeData.Inode));

                        return false;
                    }

                    //RunLength = new UlongBytes();
                    RunLength.Value = 0;

                    for (i = 0; i < RunLengthSize; i++)
                    {
                        tempOffset = Index;
                        Byte tempDataValue = RunData.ToByte(ref tempOffset);

                        if (i < 8)
                        {
                            RunLength.Bytes[i] = tempDataValue;
                        }

                        Index++;

                        if (Index >= RunDataLength)
                        {
                            ShowDebug(2, String.Format("Error: datarun is longer than buffer, the MFT may be corrupt. Inode {0:G}.",
                                  InodeData.Inode));

                            return false;
                        }
                    }

                    //RunOffset = new UlongBytes();
                    RunOffset.Value = 0;

                    for (i = 0; i < RunOffsetSize; i++)
                    {
                        tempOffset = Index;
                        Byte tempDataValue = RunData.ToByte(ref tempOffset);

                        if (i < RunOffset.Bytes.Length)
                        {
                            RunOffset.Bytes[i] = tempDataValue;
                        }

                        Index++;

                        if (Index >= RunDataLength)
                        {
                            ShowDebug(2, String.Format("Error: datarun is longer than buffer, the MFT may be corrupt. Inode {0:G}.",
                                InodeData.Inode));

                            return false;
                        }
                    }

                    if ((i < 8) && (i > 0) && (RunOffset.Bytes[i - 1] >= 0x80))
                    {
                        while (i < 8) RunOffset.Bytes[i++] = 0Xff;
                    }

                    Lcn += RunOffset.Value;

                    Vcn += RunLength.Value;

                    /* Show debug message. */
                    if (RunOffset.Value != 0)
                    {
                        ShowDebug(6, String.Format("    Extent: Lcn={0:G}, Vcn={1:G}, NextVcn={2:G}", Lcn, Vcn - RunLength.Value, Vcn));
                    }
                    else
                    {
                        ShowDebug(6, String.Format("    Extent (virtual): Vcn={0:G}, NextVcn={1:G}", Vcn - RunLength.Value, Vcn));
                    }

                    /* 
                        Add the size of the fragment to the total number of clusters.
                        There are two kinds of fragments: real and virtual. The latter do not
                        occupy clusters on disk, but are information used by compressed
                        and sparse files. 
                    */

                    if (RunOffset.Value != 0)
                    {
                        Stream.Clusters += RunLength.Value;
                    }

                    /* Add the extent to the Fragments. */
                    NewFragment = new FragmentListStruct();

                    if (NewFragment == null)
                    {
                        ShowDebug(2, "Error: malloc() returned null.");

                        return false;
                    }

                    NewFragment.Lcn = Lcn;

                    if (RunOffset.Value == 0) NewFragment.Lcn = VIRTUALFRAGMENT;

                    NewFragment.NextVcn = Vcn;
                    NewFragment.Next = null;

                    if (Stream.Fragments == null)
                    {
                        Stream.Fragments = NewFragment;
                    }
                    else
                    {
                        if (LastFragment != null) LastFragment.Next = NewFragment;
                    }

                    LastFragment = NewFragment;
                }
            }

	        return true;
        }

        /*
            Cleanup the Streams data in an InodeData struct. If CleanFragments is TRUE then
            also cleanup the fragments.
        */
        void CleanupStreams(InodeDataStruct InodeData, Boolean CleanupFragments)
        {
	        StreamStruct Stream;
	        StreamStruct TempStream;

	        FragmentListStruct Fragment;
	        FragmentListStruct TempFragment;

	        Stream = InodeData.Streams;

	        while (Stream != null)
	        {
		        if (CleanupFragments == true)
		        {
			        Fragment = Stream.Fragments;

			        while (Fragment != null)
			        {
				        TempFragment = Fragment;
				        Fragment = Fragment.Next;

				        TempFragment = null;
			        }
		        }

		        TempStream = Stream;
		        Stream = Stream.Next;

		        TempStream = null;
	        }

	        InodeData.Streams = null;
        }

        /* Construct the full stream name from the filename, the stream name, and the stream type. */
        String ConstructStreamName(String FileName1, String FileName2, StreamStruct Stream)
        {
	        String FileName;
	        String StreamName;

	        ATTRIBUTE_TYPE StreamType;

	        Int32 Length;

	        String p1;

	        FileName = FileName1;

	        if ((FileName == null) || (FileName.Length == 0)) FileName = FileName2;
            if ((FileName != null) && (FileName.Length == 0)) FileName = null;

            StreamName = null;
            StreamType = ATTRIBUTE_TYPE.AttributeInvalid;

            if (Stream != null)
	        {
		        StreamName = Stream.StreamName;

                if ((StreamName != null) && (StreamName.Length == 0)) StreamName = null;

		        StreamType = Stream.StreamType;
	        }

	        /*  
                If the StreamName is empty and the StreamType is Data then return only the
	            FileName. The Data stream is the default stream of regular files.
            */
            if (((StreamName == null) || (StreamName.Length == 0)) && (StreamType == ATTRIBUTE_TYPE.AttributeData))
	        {
                if ((FileName == null) || (FileName.Length == 0)) return (null);

		        return FileName;
	        }

	        /*  
                If the StreamName is "$I30" and the StreamType is AttributeIndexAllocation then
	            return only the FileName. This must be a directory, and the Microsoft defragmentation
            	API will automatically select this stream.
            */
            if ((StreamName != null) &&
		        (StreamName.CompareTo("$I30") == 0) &&
                (StreamType == ATTRIBUTE_TYPE.AttributeIndexAllocation))
	        {
                if ((FileName == null) || (FileName.Length == 0)) return null;
	
		        return FileName;
	        }

	        /*  
                If the StreamName is empty and the StreamType is Data then return only the
	            FileName. The Data stream is the default stream of regular files. 
            */
            if (((StreamName == null) || (StreamName.Length == 0)) &&
		        (StreamTypeNames(StreamType).Length == 0))
	        {
                if ((FileName == null) || (FileName.Length == 0)) return (null);
	
		        return FileName;
	        }

	        Length = 3;

            if (FileName != null) Length += FileName.Length;
            if (StreamName != null) Length += StreamName.Length;

	        Length = Length + StreamTypeNames(StreamType).Length;

            if (Length == 3) return (null);

	        p1 = "";

            if (FileName != null) p1 += FileName;

	        p1 += ":";

            if (StreamName != null) p1 += StreamName;

	        p1 += ":";
	        p1 += StreamTypeNames(StreamType);

	        return p1;
        }

        /*
            Process a list of attributes and store the gathered information in the Item
            struct. Return FALSE if an error occurred.
        */
        void ProcessAttributeList(
				MSDefragDataStruct Data,
				NtfsDiskInfoStruct DiskInfo,
				InodeDataStruct InodeData,
				ByteArray Buffer,
				UInt64 BufLength,
				int Depth)
        {
	        ByteArray Buffer2 = new ByteArray();

	        ATTRIBUTE_LIST Attribute;

	        UInt64 AttributeOffset;

	        FILE_RECORD_HEADER FileRecordHeader;
	        FragmentListStruct Fragment;

	        UInt64 RefInode;
	        UInt64 BaseInode;
	        UInt64 Vcn;
	        UInt64 RealVcn;
	        UInt64 RefInodeVcn;

	        IOWrapper.OVERLAPPED gOverlapped = new IOWrapper.OVERLAPPED();

	        ULARGE_INTEGER Trans;

	        Int32 BytesRead = 0;

            //Boolean Result;

	        String p1;
            //String s1;

            //JKDefragGui *jkGui = JKDefragGui::getInstance();

	        /* Sanity checks. */
            if ((Buffer == null) || (BufLength == 0)) return;

	        if (Depth > 1000)
	        {
                ShowDebug(2, "Error: infinite attribute loop, the MFT may be corrupt.");
	
		        return;
	        }

            ShowDebug(6, String.Format("    Processing AttributeList for Inode {0:G}, {1:G} bytes", InodeData.Inode, BufLength));

	        /* Walk through all the attributes and gather information. */
	        for (AttributeOffset = 0; AttributeOffset < BufLength; AttributeOffset = AttributeOffset + Attribute.Length)
	        {
                Int64 tempOffset = (Int64)AttributeOffset;
		        Attribute = (ATTRIBUTE_LIST)Buffer.ToATTRIBUTE_LIST(ref tempOffset);

		        /*  
                    Exit if no more attributes. AttributeLists are usually not closed by the
		            0xFFFFFFFF endmarker. Reaching the end of the buffer is therefore normal and
		            not an error.
                */
		        if (AttributeOffset + 3 > BufLength) break;
		        if (Attribute.AttributeType == ATTRIBUTE_TYPE.AttributeAll) break;
		        if (Attribute.Length < 3) break;
		        if (AttributeOffset + Attribute.Length > BufLength) break;

		        /*
                    Extract the referenced Inode. If it's the same as the calling Inode then ignore
		            (if we don't ignore then the program will loop forever, because for some
		            reason the info in the calling Inode is duplicated here...).
                */
		        RefInode = (UInt64)Attribute.FileReferenceNumber.InodeNumberLowPart +
				        ((UInt64)Attribute.FileReferenceNumber.InodeNumberHighPart << 32);

		        if (RefInode == InodeData.Inode) continue;

		        /* Show debug message. */
                ShowDebug(6, "    List attribute: " + StreamTypeNames(Attribute.AttributeType));
                ShowDebug(6, String.Format("      LowestVcn = {0:G}, RefInode = {1:G}, InodeSequence = {2:G}, Instance = {3:G}",
                      Attribute.LowestVcn, RefInode, Attribute.FileReferenceNumber.SequenceNumber, Attribute.Instance));

		        /*
                    Extract the streamname. I don't know why AttributeLists can have names, and
		            the name is not used further down. It is only extracted for debugging purposes.
		        */
		        if (Attribute.NameLength > 0)
		        {
                    p1 = String.Empty;

                    //p1 = Buffer[AttributeOffset + Attribute.NameOffset];

                    for (Int64 ii = 0; ii < Attribute.NameLength; ii++)
                    {
                        p1 += (Char)Buffer.GetValue((Int64)((Int64)AttributeOffset + Attribute.NameOffset + ii));
                    }

                    //wcsncpy_s(p1,Attribute->NameLength + 1,
                    //        (WCHAR *)&Buffer[AttributeOffset + Attribute->NameOffset],Attribute->NameLength);

                    ShowDebug(6, "      AttributeList name = '" + p1 + "'");
		        }

		        /* Find the fragment in the MFT that contains the referenced Inode. */
		        Vcn = 0;
		        RealVcn = 0;
		        RefInodeVcn = RefInode * DiskInfo.BytesPerMftRecord / (DiskInfo.BytesPerSector * DiskInfo.SectorsPerCluster);

                for (Fragment = InodeData.MftDataFragments; Fragment != null; Fragment = Fragment.Next)
		        {
			        if (Fragment.Lcn != VIRTUALFRAGMENT)
			        {
				        if ((RefInodeVcn >= RealVcn) && (RefInodeVcn < RealVcn + Fragment.NextVcn - Vcn))
				        {
					        break;
				        }
				
				        RealVcn = RealVcn + Fragment.NextVcn - Vcn;
			        }

			        Vcn = Fragment.NextVcn;
		        }

                if (Fragment == null)
		        {
                    ShowDebug(6, String.Format("      Error: Inode {0:G} is an extension of Inode {1:G}, but does not exist (outside the MFT).",
                     		RefInode, InodeData.Inode));

			        continue;
		        }

                UInt64 tempVcn;


		        /* Fetch the record of the referenced Inode from disk. */
		        tempVcn = (Fragment.Lcn - RealVcn) * DiskInfo.BytesPerSector *
				        DiskInfo.SectorsPerCluster + RefInode * DiskInfo.BytesPerMftRecord;

                Trans.HighPart = (UInt32)(tempVcn >> 32 & UInt32.MaxValue);
                Trans.LowPart = (UInt32)(tempVcn & UInt32.MaxValue);

		        gOverlapped.Offset     = Trans.LowPart;
		        gOverlapped.OffsetHigh = Trans.HighPart;
                gOverlapped.hEvent = UIntPtr.Zero;

                Byte[] tempBuffer = new Byte[DiskInfo.BytesPerMftRecord];

                BytesRead = IOWrapper.Read(Data.Disk.VolumeHandle, Buffer2.m_bytes, 0, (Int32)DiskInfo.BytesPerMftRecord, gOverlapped);
                //iResult = IOWrapper.ReadFile(Data.Disk.VolumeHandle, tempBuffer, DiskInfo.BytesPerMftRecord, BytesRead, gOverlapped);
                //Result = IOWrapper.ReadFile(Data.Disk.VolumeHandle, Buffer2, DiskInfo.BytesPerMftRecord, BytesRead, gOverlapped);

                if (BytesRead != (Int32)DiskInfo.BytesPerMftRecord)
                //if ((Result == false) || (BytesRead != DiskInfo.BytesPerMftRecord))
		        {
                    String errorMessage = m_msDefragLib.SystemErrorStr(Marshal.GetLastWin32Error());

                    ShowDebug(2, String.Format("      Error while reading Inode {0:G}: " + errorMessage, RefInode));

			        return;
		        }

		        /* Fixup the raw data. */
		        if (FixupRawMftdata(Data, DiskInfo, Buffer2, DiskInfo.BytesPerMftRecord) == false)
		        {
                    ShowDebug(2, String.Format("The error occurred while processing Inode {0:G}", RefInode));
	
			        continue;
		        }

		        /* If the Inode is not in use then skip. */
                //FileRecordHeader = (FILE_RECORD_HEADER)Buffer2;
                tempOffset = 0;
                FileRecordHeader = Buffer2.ToFILE_RECORD_HEADER(ref tempOffset);

		        if ((FileRecordHeader.Flags & 1) != 1)
		        {
                    ShowDebug(6, String.Format("      Referenced Inode {0:G} is not in use.", RefInode));
		
			        continue;
		        }

		        /* If the BaseInode inside the Inode is not the same as the calling Inode then skip. */
		        BaseInode = (UInt64)FileRecordHeader.BaseFileRecord.InodeNumberLowPart +
				        ((UInt64)FileRecordHeader.BaseFileRecord.InodeNumberHighPart << 32);

		        if (InodeData.Inode != BaseInode)
		        {
                    ShowDebug(6, String.Format("      Warning: Inode {0:G} is an extension of Inode {1:G}, but thinks it's an extension of Inode {2:G}.",
                     		RefInode, InodeData.Inode, BaseInode));
		
			        continue;
		        }

		        /* Process the list of attributes in the Inode, by recursively calling the ProcessAttributes() subroutine. */
                ShowDebug(6, String.Format("      Processing Inode {0:G} Instance {1:G}", RefInode, Attribute.Instance));

		        ProcessAttributes(
                    Data,
                    DiskInfo,
                    InodeData,
                    Buffer2.ToByteArray(FileRecordHeader.AttributeOffset, Buffer2.GetLength() - FileRecordHeader.AttributeOffset),
				    DiskInfo.BytesPerMftRecord - FileRecordHeader.AttributeOffset,
				    Attribute.Instance,Depth + 1);

                ShowDebug(6, String.Format("      Finished processing Inode {0:G} Instance {1:G}", RefInode, Attribute.Instance));
	        }
        }

        /*
            Process a list of attributes and store the gathered information in the Item
            struct. Return FALSE if an error occurred.
        */
        Boolean ProcessAttributes(
			MSDefragDataStruct Data,
			NtfsDiskInfoStruct DiskInfo,
			InodeDataStruct InodeData,
			ByteArray Buffer,
			UInt64 BufLength,
			UInt32 Instance,
			int Depth)
        {
	        ByteArray Buffer2;

	        UInt64 Buffer2Length;
	        UInt32 AttributeOffset;

	        ATTRIBUTE Attribute;
	        RESIDENT_ATTRIBUTE ResidentAttribute = new RESIDENT_ATTRIBUTE();
	        NONRESIDENT_ATTRIBUTE NonResidentAttribute = new NONRESIDENT_ATTRIBUTE();
	        STANDARD_INFORMATION StandardInformation;
	        FILENAME_ATTRIBUTE FileNameAttribute;

	        String p1;

            //JKDefragGui *jkGui = JKDefragGui::getInstance();

	        /*  
                Walk through all the attributes and gather information. AttributeLists are
	            skipped and interpreted later. 
            */
	        for (AttributeOffset = 0; AttributeOffset < BufLength; AttributeOffset = AttributeOffset + Attribute.Length)
	        {
                //Attribute = (ATTRIBUTE)Buffer[AttributeOffset];
                Int64 tempOffset = (Int64)AttributeOffset;
                Attribute = Buffer.ToATTRIBUTE(ref tempOffset);

		        /* Exit the loop if end-marker. */
		        if ((AttributeOffset + 4 <= BufLength) && (Attribute.AttributeType == ATTRIBUTE_TYPE.AttributeInvalid)) break;

		        /* Sanity check. */
		        if ((AttributeOffset + 4 > BufLength) ||
			        (Attribute.Length < 3) ||
			        (AttributeOffset + Attribute.Length > BufLength))
		        {
                    ShowDebug(2, String.Format("Error: attribute in Inode {0:G} is bigger than the data, the MFT may be corrupt.", InodeData.Inode));
                    ShowDebug(2, String.Format("  BufLength={0:G}, AttributeOffset={1:G}, AttributeLength={2:G}({3:X})",
                     		BufLength, AttributeOffset, Attribute.Length, Attribute.Length));

                    //m_msDefragLib.ShowHex(Data, Buffer.m_bytes, BufLength);

			        return false;
		        }

		        /* Skip AttributeList's for now. */
		        if (Attribute.AttributeType == ATTRIBUTE_TYPE.AttributeAttributeList) continue;

		        /*  
                    If the Instance does not equal the AttributeNumber then ignore the attribute.
		            This is used when an AttributeList is being processed and we only want a specific
		            instance.
                */
		        if ((Instance != UInt16.MaxValue) && (Instance != Attribute.AttributeNumber)) continue;

		        /* Show debug message. */
                ShowDebug(6, String.Format("  Attribute {0:G}: {1:G}", Attribute.AttributeNumber, StreamTypeNames(Attribute.AttributeType)));

		        if (Attribute.Nonresident == false)
		        {
                    tempOffset = (Int64)AttributeOffset;
                    ResidentAttribute = Buffer.ToRESIDENT_ATTRIBUTE(ref tempOffset);

			        /* The AttributeFileName (0x30) contains the filename and the link to the parent directory. */
			        if (Attribute.AttributeType == ATTRIBUTE_TYPE.AttributeFileName)
			        {
                        tempOffset = (Int64)(AttributeOffset + ResidentAttribute.ValueOffset);

                        FileNameAttribute = Buffer.ToFILENAME_ATTRIBUTE(ref tempOffset);
                        //FileNameAttribute = (FILENAME_ATTRIBUTE)Buffer[AttributeOffset + ResidentAttribute.ValueOffset];

				        InodeData.ParentInode = FileNameAttribute.ParentDirectory.InodeNumberLowPart +
						    (((UInt32)FileNameAttribute.ParentDirectory.InodeNumberHighPart) << 32);

				        if (FileNameAttribute.NameLength > 0)
				        {
					        /* Extract the filename. */
                            p1 = FileNameAttribute.Name.ToString();

					        /*
                                Save the filename in either the Long or the Short filename. We only
					            save the first filename, any additional filenames are hard links. They
					            might be useful for an optimization algorithm that sorts by filename,
					            but which of the hardlinked names should it sort? So we only store the
					            first filename.
                            */
					        if (FileNameAttribute.NameType == 2)
					        {
                                if (InodeData.ShortFilename == null)
						        {
							        InodeData.ShortFilename = p1;

                                    ShowDebug(6, String.Format("    Short filename = '{0:G}'", p1));
						        }
					        }
					        else
					        {
                                if (InodeData.LongFilename == null)
						        {
							        InodeData.LongFilename = p1;

                                    ShowDebug(6, String.Format("    Long filename = '{0:G}'", p1));
						        }
					        }
				        }
			        }

			        /*  
                        The AttributeStandardInformation (0x10) contains the CreationTime, LastAccessTime,
			            the MftChangeTime, and the file attributes.
                    */
			        if (Attribute.AttributeType == ATTRIBUTE_TYPE.AttributeStandardInformation)
			        {
                        tempOffset = (Int64)(AttributeOffset + ResidentAttribute.ValueOffset);
                        StandardInformation = Buffer.ToSTANDARD_INFORMATION(ref tempOffset);
			
				        InodeData.CreationTime = StandardInformation.CreationTime;
				        InodeData.MftChangeTime = StandardInformation.MftChangeTime;
				        InodeData.LastAccessTime = StandardInformation.LastAccessTime;
			        }

			        /* The value of the AttributeData (0x80) is the actual data of the file. */
			        if (Attribute.AttributeType == ATTRIBUTE_TYPE.AttributeData)
			        {
				        InodeData.Bytes = ResidentAttribute.ValueLength;
			        }
		        }
		        else
		        {
                    tempOffset = (Int64)AttributeOffset;
                    NonResidentAttribute = Buffer.ToNONRESIDENT_ATTRIBUTE(ref tempOffset);

			        /* Save the length (number of bytes) of the data. */
			        if ((Attribute.AttributeType == ATTRIBUTE_TYPE.AttributeData) && (InodeData.Bytes == 0))
			        {
				        InodeData.Bytes = NonResidentAttribute.DataSize;
			        }

			        /* Extract the streamname. */
                    p1 = "";

                    Int64 ii = 0;

                    tempOffset = (Int64)(AttributeOffset + Attribute.NameOffset);

                    for (ii = 0; ii < Attribute.NameLength; ii++)
                    {
                        p1 += (Char)Buffer.ToUInt16(ref tempOffset);
                    }

			        /* Create a new stream with a list of fragments for this data. */
			        TranslateRundataToFragmentlist(Data, InodeData, p1, Attribute.AttributeType,
                            Buffer.ToByteArray((Int64)(AttributeOffset + NonResidentAttribute.RunArrayOffset), Buffer.GetLength() - (Int64)((AttributeOffset + NonResidentAttribute.RunArrayOffset))),
					        Attribute.Length - NonResidentAttribute.RunArrayOffset,
					        NonResidentAttribute.StartingVcn, NonResidentAttribute.DataSize);

			        /* Special case: If this is the $MFT then save data. */
			        if (InodeData.Inode == 0)
			        {
                        if ((Attribute.AttributeType == ATTRIBUTE_TYPE.AttributeData) && (InodeData.MftDataFragments == null))
				        {
					        InodeData.MftDataFragments = InodeData.Streams.Fragments;
					        InodeData.MftDataBytes = NonResidentAttribute.DataSize;
				        }

                        if ((Attribute.AttributeType == ATTRIBUTE_TYPE.AttributeBitmap) && (InodeData.MftBitmapFragments == null))
				        {
					        InodeData.MftBitmapFragments = InodeData.Streams.Fragments;
					        InodeData.MftBitmapBytes = NonResidentAttribute.DataSize;
				        }
			        }
		        }
	        }

	        /*  
                Walk through all the attributes and interpret the AttributeLists. We have to
	            do this after the DATA and BITMAP attributes have been interpreted, because
	            some MFT's have an AttributeList that is stored in fragments that are
	            defined in the DATA attribute, and/or contain a continuation of the DATA or
	            BITMAP attributes.
            */
	        for (AttributeOffset = 0; AttributeOffset < BufLength; AttributeOffset = AttributeOffset + Attribute.Length)
	        {
                Int64 tempOffset = (Int64)AttributeOffset;
                Attribute = Buffer.ToATTRIBUTE(ref tempOffset);
                //Attribute = (ATTRIBUTE)Buffer[AttributeOffset];

		        if (Attribute.AttributeType == ATTRIBUTE_TYPE.AttributeInvalid) break;
		        if (Attribute.AttributeType != ATTRIBUTE_TYPE.AttributeAttributeList) continue;

                ShowDebug(6, String.Format("  Attribute {0:G}: {1:G}", Attribute.AttributeNumber, StreamTypeNames(Attribute.AttributeType)));

		        if (Attribute.Nonresident == false)
		        {
			        ResidentAttribute.Attribute = Attribute;
		
			        ProcessAttributeList(Data,DiskInfo,InodeData,
                            Buffer.ToByteArray((Int64)(AttributeOffset + ResidentAttribute.ValueOffset), Buffer.GetLength() - (Int64)(AttributeOffset + ResidentAttribute.ValueOffset)),
					        ResidentAttribute.ValueLength,Depth);
		        }
		        else
		        {
			        NonResidentAttribute.Attribute = Attribute;
			        Buffer2Length = NonResidentAttribute.DataSize;

			        Buffer2 = ReadNonResidentData(Data,DiskInfo,
                            Buffer.ToByteArray((Int64)(AttributeOffset + NonResidentAttribute.RunArrayOffset), Buffer.GetLength() - (Int64)(AttributeOffset + NonResidentAttribute.RunArrayOffset)),
					        Attribute.Length - NonResidentAttribute.RunArrayOffset, 0, Buffer2Length);

			        ProcessAttributeList(Data,DiskInfo,InodeData,Buffer2,Buffer2Length,Depth);
		        }
	        }

	        return true;
        }

        Boolean InterpretMftRecord(
            MSDefragDataStruct Data,
            NtfsDiskInfoStruct DiskInfo,
            Array InodeArray,
            UInt64 InodeNumber,
            UInt64 MaxInode,
            ref FragmentListStruct MftDataFragments,
            ref UInt64 MftDataBytes,
            ref FragmentListStruct MftBitmapFragments,
            ref UInt64 MftBitmapBytes,
            ByteArray Buffer,
            UInt64 BufLength)
        {
	        FILE_RECORD_HEADER FileRecordHeader;

	        InodeDataStruct InodeData = new InodeDataStruct();

	        ItemStruct Item;
	        StreamStruct Stream;

	        UInt64 BaseInode;

	        /* If the record is not in use then quietly exit. */
            //FileRecordHeader = (FILE_RECORD_HEADER)Buffer;
            Int64 tempOffset = 0;
            FileRecordHeader = Buffer.ToFILE_RECORD_HEADER(ref tempOffset);

	        if ((FileRecordHeader.Flags & 1) != 1)
	        {
                ShowDebug(6, String.Format("Inode {0:G} is not in use.", InodeNumber));

		        return false;
	        }

	        /*
                If the record has a BaseFileRecord then ignore it. It is used by an
	            AttributeAttributeList as an extension of another Inode, it's not an
	            Inode by itself. 
            */
	        BaseInode = (UInt64)FileRecordHeader.BaseFileRecord.InodeNumberLowPart +
			((UInt64)FileRecordHeader.BaseFileRecord.InodeNumberHighPart << 32);

	        if (BaseInode != 0)
	        {
                ShowDebug(6, String.Format("Ignoring Inode {0:G}, it's an extension of Inode {1:G}", InodeNumber, BaseInode));

		        return true;
	        }

            ShowDebug(6, String.Format("Processing Inode {0:G}...", InodeNumber));

	        /* Show a warning if the Flags have an unknown value. */
	        if ((FileRecordHeader.Flags & 252) != 0)
	        {
                ShowDebug(6, String.Format("  Inode {0:G} has Flags = {1:G}", InodeNumber, FileRecordHeader.Flags));
	        }

	        /*
                I think the MFTRecordNumber should always be the InodeNumber, but it's an XP
	            extension and I'm not sure about Win2K.

	            Note: why is the MFTRecordNumber only 32 bit? Inode numbers are 48 bit.
            */
	        if (FileRecordHeader.MFTRecordNumber != InodeNumber)
	        {
                ShowDebug(6, String.Format("  Warning: Inode {0:G} contains a different MFTRecordNumber {1:G}",
                      InodeNumber, FileRecordHeader.MFTRecordNumber));
	        }

	        /* Sanity check. */
	        if (FileRecordHeader.AttributeOffset >= BufLength)
	        {
                ShowDebug(2, String.Format("Error: attributes in Inode {0:G} are outside the FILE record, the MFT may be corrupt.",
                      InodeNumber));

		        return false;
	        }

	        if (FileRecordHeader.BytesInUse > BufLength)
	        {
                ShowDebug(2, String.Format("Error: in Inode {0:G} the record is bigger than the size of the buffer, the MFT may be corrupt.",
                      InodeNumber));
	
		        return false;
	        }

	        /* Initialize the InodeData struct. */
	        InodeData.Inode = InodeNumber;                                /* The Inode number. */
	        InodeData.ParentInode = 5;            /* The Inode number of the parent directory. */
	        InodeData.Directory = false;

	        if ((FileRecordHeader.Flags & 2) == 2) InodeData.Directory = true;

	        InodeData.LongFilename = null;                                   /* Long filename. */
	        InodeData.ShortFilename = null;                       /* Short filename (8.3 DOS). */
	        InodeData.CreationTime = 0;                                 /* 1 second = 10000000 */
	        InodeData.MftChangeTime = 0;
	        InodeData.LastAccessTime = 0;
	        InodeData.Bytes = 0;                                  /* Size of the $DATA stream. */
	        InodeData.Streams = null;                                 /* List of StreamStruct. */
	        InodeData.MftDataFragments = MftDataFragments;
	        InodeData.MftDataBytes = MftDataBytes;
	        InodeData.MftBitmapFragments = null;
	        InodeData.MftBitmapBytes = 0;

	        /* Make sure that directories are always created. */
	        if (InodeData.Directory == true)
	        {
		        TranslateRundataToFragmentlist(Data, InodeData, "$I30", ATTRIBUTE_TYPE.AttributeIndexAllocation, null, 0, 0, 0);
	        }

	        /* Interpret the attributes. */
	        ProcessAttributes(Data,DiskInfo, InodeData,
                Buffer.ToByteArray(FileRecordHeader.AttributeOffset, Buffer.GetLength() - FileRecordHeader.AttributeOffset),
		        BufLength - FileRecordHeader.AttributeOffset, UInt16.MaxValue, 0);

	        /* Save the MftDataFragments, MftDataBytes, MftBitmapFragments, and MftBitmapBytes. */
	        if (InodeNumber == 0)
	        {
		        MftDataFragments = InodeData.MftDataFragments;
		        MftDataBytes = InodeData.MftDataBytes;
		        MftBitmapFragments = InodeData.MftBitmapFragments;
		        MftBitmapBytes = InodeData.MftBitmapBytes;
	        }

	        /* Create an item in the Data->ItemTree for every stream. */
	        Stream = InodeData.Streams;

            do
	        {
		        /* Create and fill a new item record in memory. */
                Item = new ItemStruct();

		        if (Item == null)
		        {
                    ShowDebug(2, "Error: Could not allocate memory.");

			        CleanupStreams(InodeData, true);

			        return false;
		        }

		        Item.LongFilename = ConstructStreamName(InodeData.LongFilename, InodeData.ShortFilename, Stream);
		        Item.LongPath = null;

		        Item.ShortFilename = ConstructStreamName(InodeData.ShortFilename, InodeData.LongFilename, Stream);
		        Item.ShortPath = null;

		        Item.Bytes = InodeData.Bytes;

		        if (Stream != null) Item.Bytes = Stream.Bytes;

		        Item.Clusters = 0;

		        if (Stream != null) Item.Clusters = Stream.Clusters;

		        Item.CreationTime = InodeData.CreationTime;
		        Item.MftChangeTime = InodeData.MftChangeTime;
		        Item.LastAccessTime = InodeData.LastAccessTime;
		        Item.Fragments = null;

		        if (Stream != null) Item.Fragments = Stream.Fragments;

		        Item.ParentInode = InodeData.ParentInode;
		        Item.Directory = InodeData.Directory;
		        Item.Unmovable = false;
		        Item.Exclude = false;
		        Item.SpaceHog = false;

		        /* Increment counters. */
		        if (Item.Directory == true)
		        {
			        Data.CountDirectories = Data.CountDirectories + 1;
		        }

		        Data.CountAllFiles ++;

		        if ((Stream != null) && (Stream.StreamType == ATTRIBUTE_TYPE.AttributeData))
		        {
			        Data.CountAllBytes = Data.CountAllBytes + InodeData.Bytes;
		        }

		        if (Stream != null) Data.CountAllClusters = Data.CountAllClusters + Stream.Clusters;

                if (m_msDefragLib.FragmentCount(Item) > 1)
                {
                    Data.CountFragmentedItems ++;
                    Data.CountFragmentedBytes += InodeData.Bytes;

                    if (Stream != null) Data.CountFragmentedClusters = Data.CountFragmentedClusters + Stream.Clusters;
                }

                /* Add the item record to the sorted item tree in memory. */
                m_msDefragLib.TreeInsert(Data, Item);

		        /*
                    Also add the item to the array that is used to construct the full pathnames.

		            NOTE:
                    If the array already contains an entry, and the new item has a shorter
		            filename, then the entry is replaced. This is needed to make sure that
		            the shortest form of the name of directories is used. 
                */

                ItemStruct InodeItem = null;
        
                if (InodeArray != null && InodeNumber < MaxInode)
                {
                    InodeItem = (ItemStruct)InodeArray.GetValue((Int64)InodeNumber);
                }

                String InodeLongFilename = "";

                if (InodeItem != null)
                {
                    InodeLongFilename = InodeItem.LongFilename;
                }

		        if (InodeLongFilename.CompareTo(Item.LongFilename) > 0)
		        {
			        InodeArray.SetValue(Item,(Int64)InodeNumber);
		        }

		        /* Draw the item on the screen. */
                //jkGui->ShowAnalyze(Data,Item);

                if (Data.RedrawScreen == 0)
                {
                    m_msDefragLib.ColorizeItem(Data, Item, 0, 0, false);
                }
                else
                {
                    m_msDefragLib.ShowDiskmap(Data);
                }

//                m_msDefragLib.ColorizeItem(Data, Item, 0, 0, false);

		        if (Stream != null) Stream = Stream.Next;

	        } while (Stream != null);

	        /* Cleanup and return true. */
	        CleanupStreams(InodeData, false);

	        return true;
        }

        public Boolean ReadBootDiskRecord(MSDefragDataStruct Data)
        {
            Int32 NTFS_BOOT_SECTOR_SIZE = 512;
            ByteArray Buffer = new ByteArray();
            IOWrapper.OVERLAPPED gOverlapped = new IOWrapper.OVERLAPPED();
            Int32 BytesRead = 0;

            Buffer.Initialize(NTFS_BOOT_SECTOR_SIZE);

            gOverlapped.Offset = 0;
            gOverlapped.OffsetHigh = 0;
            gOverlapped.hEvent = UIntPtr.Zero;

            Data.Disk.VolumeHandle = IOWrapper.OpenVolume("C:");

            BytesRead = IOWrapper.Read(Data.Disk.VolumeHandle, Buffer.m_bytes, 0, NTFS_BOOT_SECTOR_SIZE, gOverlapped);

            if (BytesRead != NTFS_BOOT_SECTOR_SIZE)
            {
                String errorMessage = m_msDefragLib.SystemErrorStr(Marshal.GetLastWin32Error());

                ShowDebug(2, String.Format("Error while reading bootblock: {0:G}", errorMessage));

                return false;
            }

            IOWrapper.CloseHandle(Data.Disk.VolumeHandle);

            Int64 tempOffset = 11;

            NTFS_BOOT_SECTOR nbs = Buffer.ToNTFS_BOOT_SECTOR(ref tempOffset);

            return true;
        }

        //////////////////////////////////////////////////////////////////////////
        //
        // Load the MFT into a list of ItemStruct records in memory.
        //
        //////////////////////////////////////////////////////////////////////////
        public Boolean AnalyzeNtfsVolume(MSDefragDataStruct Data)
        {
            ReadBootDiskRecord(Data);

	        NtfsDiskInfoStruct DiskInfo = new NtfsDiskInfoStruct();

	        ByteArray Buffer = new ByteArray();

	        IOWrapper.OVERLAPPED gOverlapped = new IOWrapper.OVERLAPPED();

	        ULARGE_INTEGER Trans;

	        Int32 BytesRead = 0;

	        FragmentListStruct MftDataFragments;
            FragmentListStruct MftBitmapFragments;

	        UInt64 MftDataBytes = 0;
	        UInt64 MftBitmapBytes = 0;
	        UInt64 MaxMftBitmapBytes = 0;

	        ByteArray MftBitmap = new ByteArray();

	        FragmentListStruct Fragment = null;

            ItemStruct[] InodeArray = null;// new ItemStruct[1335952 + 100000];

            UInt64 MaxInode = 0;

	        ItemStruct Item = null;

	        UInt64 Vcn = 0;
	        UInt64 RealVcn = 0;
	        UInt64 InodeNumber = 0;
	        UInt64 BlockStart = 0;
	        UInt64 BlockEnd = 0;

	        Byte[] BitmapMasks = {1,2,4,8,16,32,64,128};

	        Boolean Result = false;

	        UInt64 ClustersPerMftRecord = 0;

            DateTime Time;

	        Int64 StartTime = 0;
	        Int64 EndTime = 0;

	        UInt64 u1 = 0;

            //////////////////////////////////////////////////////////////////////////
            //
	        // Read the boot block from the disk.
            //
            //////////////////////////////////////////////////////////////////////////
            
	        gOverlapped.Offset     = 0;
	        gOverlapped.OffsetHigh = 0;
            gOverlapped.hEvent = UIntPtr.Zero;
            
            Data.Disk.VolumeHandle = IOWrapper.OpenVolume("C:");

            Buffer.Initialize((Int64)MFTBUFFERSIZE);

            BytesRead = IOWrapper.Read(Data.Disk.VolumeHandle, Buffer.m_bytes, 0, 512, gOverlapped);

            if (BytesRead != 512)
	        {
                String errorMessage = m_msDefragLib.SystemErrorStr(Marshal.GetLastWin32Error());

                ShowDebug(2, String.Format("Error while reading bootblock: {0:G}", errorMessage));

		        return false;
	        }

            //////////////////////////////////////////////////////////////////////////
            //
	        // Test if the boot block is an NTFS boot block.
            //
            //////////////////////////////////////////////////////////////////////////
            
            Int64 tempOffset = 3;

            if (Buffer.ToUInt64(ref tempOffset) != 0x202020205346544E)
	        {
                ShowDebug(2, "This is not an NTFS disk (different cookie).");

		        return false;
	        }

	        /* Extract data from the bootblock. */
	        Data.Disk.Type = DiskType.NTFS;

            DiskInfo.BytesPerSector = Buffer.ToUInt16(ref tempOffset);

	        /* Still to do: check for impossible values. */
            DiskInfo.SectorsPerCluster = Buffer.ToUInt64(ref tempOffset);

            tempOffset = 40;
            DiskInfo.TotalSectors = Buffer.ToUInt64(ref tempOffset);
            DiskInfo.MftStartLcn = Buffer.ToUInt64(ref tempOffset);
            DiskInfo.Mft2StartLcn = Buffer.ToUInt64(ref tempOffset);
            ClustersPerMftRecord = Buffer.ToUInt32(ref tempOffset);

	        if (ClustersPerMftRecord >= 128)
	        {
                DiskInfo.BytesPerMftRecord = (UInt64)(1 << (256 - (Int16)ClustersPerMftRecord));
	        }
	        else 
	        {
		        DiskInfo.BytesPerMftRecord = ClustersPerMftRecord * DiskInfo.BytesPerSector * DiskInfo.SectorsPerCluster;
	        }

            DiskInfo.ClustersPerIndexRecord = Buffer.ToUInt32(ref tempOffset);

	        Data.BytesPerCluster = DiskInfo.BytesPerSector * DiskInfo.SectorsPerCluster;

	        if (DiskInfo.SectorsPerCluster > 0)
	        {
		        Data.TotalClusters = DiskInfo.TotalSectors / DiskInfo.SectorsPerCluster;
	        }

            ShowDebug(0, "This is an NTFS disk.");

            tempOffset = 3;

            ShowDebug(2, String.Format("  Disk cookie: {0:X}", Buffer.ToUInt64(ref tempOffset)));
            ShowDebug(2, String.Format("  BytesPerSector: {0:G}",DiskInfo.BytesPerSector));
            ShowDebug(2, String.Format("  TotalSectors: {0:G}", DiskInfo.TotalSectors));
            ShowDebug(2, String.Format("  SectorsPerCluster: {0:G}", DiskInfo.SectorsPerCluster));

            tempOffset = 24;
            ShowDebug(2, String.Format("  SectorsPerTrack: {0:G}", Buffer.ToUInt16(ref tempOffset)));
            ShowDebug(2, String.Format("  NumberOfHeads: {0:G}", Buffer.ToUInt16(ref tempOffset)));
            ShowDebug(2, String.Format("  MftStartLcn: {0:G}", DiskInfo.MftStartLcn));
            ShowDebug(2, String.Format("  Mft2StartLcn: {0:G}", DiskInfo.Mft2StartLcn));
            ShowDebug(2, String.Format("  BytesPerMftRecord: {0:G}", DiskInfo.BytesPerMftRecord));
            ShowDebug(2, String.Format("  ClustersPerIndexRecord: {0:G}", DiskInfo.ClustersPerIndexRecord));

            tempOffset = 21;
            ShowDebug(2, String.Format("  MediaType: {0:X}", Buffer.ToByte(ref tempOffset)));

            tempOffset = 72;
            ShowDebug(2, String.Format("  VolumeSerialNumber: {0:X}", Buffer.ToUInt64(ref tempOffset)));

	        /* 
                Calculate the size of first 16 Inodes in the MFT. The Microsoft defragmentation
	            API cannot move these inodes.
            */
	        Data.Disk.MftLockedClusters = DiskInfo.BytesPerSector * DiskInfo.SectorsPerCluster / DiskInfo.BytesPerMftRecord;

	        /*
                Read the $MFT record from disk into memory, which is always the first record in
	            the MFT.
            */
            UInt64 tempLcn = DiskInfo.MftStartLcn * DiskInfo.BytesPerSector * DiskInfo.SectorsPerCluster;
            //Trans.QuadPart         = DiskInfo.MftStartLcn * DiskInfo.BytesPerSector * DiskInfo.SectorsPerCluster;

            Trans.HighPart = (UInt32)(tempLcn >> 32);
            Trans.LowPart = (UInt32)(tempLcn & UInt32.MaxValue);

            gOverlapped.Offset     = Trans.LowPart;
	        gOverlapped.OffsetHigh = Trans.HighPart;
    
            gOverlapped.hEvent = UIntPtr.Zero;

            BytesRead = IOWrapper.Read(Data.Disk.VolumeHandle, Buffer.m_bytes, 0, (Int32)DiskInfo.BytesPerMftRecord, gOverlapped);

            if (BytesRead != (Int32)DiskInfo.BytesPerMftRecord)
	        {
                String errorMessage = m_msDefragLib.SystemErrorStr(Marshal.GetLastWin32Error());

                ShowDebug(2, "Error while reading first MFT record: " + errorMessage);

		        return false;
	        }

	        /* Fixup the raw data from disk. This will also test if it's a valid $MFT record. */
	        if (FixupRawMftdata(Data, DiskInfo, Buffer, DiskInfo.BytesPerMftRecord) == false)
	        {
		        return false;
	        }

	        /*
                Extract data from the MFT record and put into an Item struct in memory. If
	            there was an error then exit. 
            */
	        MftDataBytes = 0;
            MftDataFragments = null;
	        MftBitmapBytes = 0;
            MftBitmapFragments = null;

            Result = InterpretMftRecord(Data, DiskInfo, null, 0, 0, 
                ref MftDataFragments, ref MftDataBytes, ref MftBitmapFragments, ref MftBitmapBytes,
                Buffer, DiskInfo.BytesPerMftRecord);

	        if ((Result == false) ||
                (MftDataFragments == null) || (MftDataBytes == 0) ||
                (MftBitmapFragments == null) || (MftBitmapBytes == 0))
	        {
                ShowDebug(2, "Fatal error, cannot process this disk.");

                m_msDefragLib.DeleteItemTree(Data.ItemTree);

                Data.ItemTree = null;

                IOWrapper.CloseHandle(Data.Disk.VolumeHandle);
                
                return false;
	        }

            ShowDebug(6, String.Format("MftDataBytes = {0:G}, MftBitmapBytes = {0:G}", MftDataBytes, MftBitmapBytes));

	        /*
                Read the complete $MFT::$BITMAP into memory.

	            NOTE:
             
                The allocated size of the bitmap is a multiple of the cluster size. This
	            is only to make it easier to read the fragments, the extra bytes are not used.
            */
            ShowDebug(6, "Reading $MFT::$BITMAP into memory");

	        Vcn = 0;
	        MaxMftBitmapBytes = 0;

            for (Fragment = MftBitmapFragments; Fragment != null; Fragment = Fragment.Next)
	        {
		        if (Fragment.Lcn != VIRTUALFRAGMENT)
		        {
			        MaxMftBitmapBytes += (Fragment.NextVcn - Vcn) * DiskInfo.BytesPerSector * DiskInfo.SectorsPerCluster;
		        }

		        Vcn = Fragment.NextVcn;
	        }

	        if (MaxMftBitmapBytes < MftBitmapBytes) MaxMftBitmapBytes = MftBitmapBytes;

            MftBitmap.Initialize((Int64)MaxMftBitmapBytes);

            if (MftBitmap == null)
	        {
                ShowDebug(2, "Error: Could not allocate memory.");

                m_msDefragLib.DeleteItemTree(Data.ItemTree);

                Data.ItemTree = null;

                IOWrapper.CloseHandle(Data.Disk.VolumeHandle);
                
                return false;
	        }

	        Vcn = 0;
	        RealVcn = 0;

            ShowDebug(6, "Reading $MFT::$BITMAP into memory");

            for (Fragment = MftBitmapFragments; Fragment != null; Fragment = Fragment.Next)
	        {
		        if (Fragment.Lcn != VIRTUALFRAGMENT)
		        {
                    ShowDebug(6, String.Format("  Extent Lcn={0:G}, RealVcn={1:G}, Size={2:G}",
                          Fragment.Lcn, RealVcn, Fragment.NextVcn - Vcn));

                    tempLcn = Fragment.Lcn * DiskInfo.BytesPerSector * DiskInfo.SectorsPerCluster;
//			        Trans.QuadPart = Fragment.Lcn * DiskInfo.BytesPerSector * DiskInfo.SectorsPerCluster;

                    Trans.HighPart = (UInt32)((UInt32)(tempLcn >> 32) & UInt32.MaxValue);
                    Trans.LowPart = (UInt32)((UInt32)tempLcn & UInt32.MaxValue);

                    gOverlapped.Offset = Trans.LowPart;
                    gOverlapped.OffsetHigh = Trans.HighPart;
                    gOverlapped.hEvent = UIntPtr.Zero;

                    UInt64 numClusters = Fragment.NextVcn - Vcn;
                    Int32 numBytes = (Int32)(numClusters * DiskInfo.BytesPerSector * DiskInfo.SectorsPerCluster);
                    Int32 startIndex = (Int32)(RealVcn * DiskInfo.BytesPerSector * DiskInfo.SectorsPerCluster);

                    ShowDebug(6, String.Format("    Reading {0:G} clusters ({1:G} bytes) from LCN={2:G}", numClusters, numBytes, Fragment.Lcn));

                    BytesRead = IOWrapper.Read(
                        Data.Disk.VolumeHandle, 
                        MftBitmap.m_bytes, 
                        startIndex, 
                        numBytes,gOverlapped);

                    if (BytesRead != numBytes)
			        {
                        String errorMessage = m_msDefragLib.SystemErrorStr(Marshal.GetLastWin32Error());

                        ShowDebug(2, errorMessage);

                        m_msDefragLib.DeleteItemTree(Data.ItemTree);

                        Data.ItemTree = null;

                        IOWrapper.CloseHandle(Data.Disk.VolumeHandle);
                        
                        return false;
			        }

			        RealVcn += Fragment.NextVcn - Vcn;
		        }

		        Vcn = Fragment.NextVcn;
	        }

	        //////////////////////////////////////////////////////////////////////////
	        //
            //    Construct an array of all the items in memory, indexed by Inode.
            //
            //    NOTE:
            //     
            //    The maximum number of Inodes is primarily determined by the size of the
	        //    bitmap. But that is rounded up to 8 Inodes, and the MFT can be shorter. 
            //
            //////////////////////////////////////////////////////////////////////////
            
            MaxInode = MftBitmapBytes * 8;

	        if (MaxInode > MftDataBytes / DiskInfo.BytesPerMftRecord)
	        {
		        MaxInode = MftDataBytes / DiskInfo.BytesPerMftRecord;
	        }

            InodeArray = new ItemStruct[MaxInode];

            if (InodeArray == null)
	        {
                ShowDebug(2, "Error: Could not allocate memory.");

                m_msDefragLib.DeleteItemTree(Data.ItemTree);

                Data.ItemTree = null;

		        return false;
	        }

	        InodeArray.SetValue(Data.ItemTree, 0);

	        for (InodeNumber = 1; InodeNumber < MaxInode; InodeNumber++)
	        {
                InodeArray.SetValue(null, (Int64)InodeNumber);
	        }

	        /*
                Read and process all the records in the MFT. The records are read into a
	            buffer and then given one by one to the InterpretMftRecord() subroutine.
            */
	        Fragment = MftDataFragments;
	        BlockEnd = 0;
	        Vcn = 0;
	        RealVcn = 0;

	        Data.PhaseDone = 0;
	        Data.PhaseTodo = 0;

            Time = DateTime.Now;

            StartTime = Time.ToFileTime();

            for (InodeNumber = 1; InodeNumber < MaxInode; InodeNumber++)
            {
                Byte val = MftBitmap.GetValue((Int64)(InodeNumber >> 3));
                Boolean mask = ((val & BitmapMasks[InodeNumber % 8]) == 0);

                if (mask == false) continue;

                Data.PhaseTodo ++;
            }

	        for (InodeNumber = 1; InodeNumber < MaxInode; InodeNumber++)
	        {
                //if (Data.Running != true) break;

                tempOffset = (Int64)(InodeNumber >> 3);

		        /*  Ignore the Inode if the bitmap says it's not in use. */
		        if ((MftBitmap.ToByte(ref tempOffset) & BitmapMasks[InodeNumber % 8]) == 0)
		        {
                    ShowDebug(6, String.Format("Inode {0:G} is not in use.", InodeNumber));

			        continue;
		        }

		        /* Update the progress counter. */
		        Data.PhaseDone ++;

		        /* Read a block of inode's into memory. */
		        if (InodeNumber >= BlockEnd)
		        {
			        /* Slow the program down to the percentage that was specified on the command line. */
                    m_msDefragLib.SlowDown(Data);

			        BlockStart = InodeNumber;
			        BlockEnd = BlockStart + MFTBUFFERSIZE / DiskInfo.BytesPerMftRecord;

			        if (BlockEnd > MftBitmapBytes * 8) BlockEnd = MftBitmapBytes * 8;

                    while (Fragment != null)
			        {
				        /* Calculate Inode at the end of the fragment. */
				        u1 = (RealVcn + Fragment.NextVcn - Vcn) * DiskInfo.BytesPerSector *
					            DiskInfo.SectorsPerCluster / DiskInfo.BytesPerMftRecord;

				        if (u1 > InodeNumber) break;

				        do
				        {
                            ShowDebug(6, "Skipping to next extent");

					        if (Fragment.Lcn != VIRTUALFRAGMENT) RealVcn += Fragment.NextVcn - Vcn;

					        Vcn = Fragment.NextVcn;
					        Fragment = Fragment.Next;

                            if (Fragment == null) break;
				        } while (Fragment.Lcn == VIRTUALFRAGMENT);

                        ShowDebug(6, String.Format("  Extent Lcn={0:G}, RealVcn={1:G}, Size={2:G}",
                              Fragment.Lcn, RealVcn, Fragment.NextVcn - Vcn));
			        }

                    if (Fragment == null) break;
			        if (BlockEnd >= u1) BlockEnd = u1;

                    tempLcn = (Fragment.Lcn - RealVcn) * DiskInfo.BytesPerSector *
                            DiskInfo.SectorsPerCluster + BlockStart * DiskInfo.BytesPerMftRecord;

                    Trans.HighPart = (UInt32)(tempLcn >> 32);
                    Trans.LowPart = (UInt32)(tempLcn & UInt32.MaxValue);
                    //Trans.QuadPart = (Fragment.Lcn - RealVcn) * DiskInfo.BytesPerSector *
                    //        DiskInfo.SectorsPerCluster + BlockStart * DiskInfo.BytesPerMftRecord;

			        gOverlapped.Offset     = Trans.LowPart;
			        gOverlapped.OffsetHigh = Trans.HighPart;
                    gOverlapped.hEvent = UIntPtr.Zero;

                    ShowDebug(6, String.Format("Reading block of {0:G} Inodes from MFT into memory, {1:G} bytes from LCN={2:G}",
                          BlockEnd - BlockStart,((BlockEnd - BlockStart) * DiskInfo.BytesPerMftRecord),
                          tempLcn / (DiskInfo.BytesPerSector * DiskInfo.SectorsPerCluster)));

                    BytesRead = IOWrapper.Read(Data.Disk.VolumeHandle, Buffer.m_bytes, 0,
                            (Int32)((BlockEnd - BlockStart) * DiskInfo.BytesPerMftRecord),gOverlapped);

                    //Result = IOWrapper.ReadFile(Data.Disk.VolumeHandle, Buffer.m_bytes,
                    //        (BlockEnd - BlockStart) * DiskInfo.BytesPerMftRecord, BytesRead,
                    //        gOverlapped);

			        if (BytesRead != (Int32)((BlockEnd - BlockStart) * DiskInfo.BytesPerMftRecord))
                    //if ((Result == false) || (BytesRead != (BlockEnd - BlockStart) * DiskInfo.BytesPerMftRecord))
			        {
                        String errorMessage = m_msDefragLib.SystemErrorStr(Marshal.GetLastWin32Error());

                        ShowDebug(2, String.Format("Error while reading Inodes {0:G} to {1:G}: {2:G}", InodeNumber, BlockEnd - 1, errorMessage));

                        m_msDefragLib.DeleteItemTree(Data.ItemTree);

                        Data.ItemTree = null;

                        IOWrapper.CloseHandle(Data.Disk.VolumeHandle);
                        
                        return false;
			        }
		        }

		        /* Fixup the raw data of this Inode. */
		        if (FixupRawMftdata(Data, DiskInfo,
                        Buffer.ToByteArray((Int64)((InodeNumber - BlockStart) * DiskInfo.BytesPerMftRecord), Buffer.GetLength() - (Int64)((InodeNumber - BlockStart) * DiskInfo.BytesPerMftRecord)),
                        //(ByteArray)Buffer.m_bytes.GetValue((Int64)((InodeNumber - BlockStart) * DiskInfo.BytesPerMftRecord),Buffer.m_bytes.Length - 1),
			            DiskInfo.BytesPerMftRecord) == false)
		        {
                    ShowDebug(2, String.Format("The error occurred while processing Inode {0:G} (max {0:G})",
                            InodeNumber,MaxInode));
		
			        continue;
		        }

		        /* Interpret the Inode's attributes. */
		        Result = InterpretMftRecord(Data,DiskInfo,InodeArray,InodeNumber,MaxInode,
			            ref MftDataFragments, ref MftDataBytes, ref MftBitmapFragments, ref MftBitmapBytes,
                        Buffer.ToByteArray(
                            (Int64)((InodeNumber - BlockStart) * DiskInfo.BytesPerMftRecord), 
                            (Int64)DiskInfo.BytesPerMftRecord),
                        //(ByteArray)Buffer.m_bytes.GetValue(
                        //    (Int64)((InodeNumber - BlockStart) * DiskInfo.BytesPerMftRecord),
                        //    (Int64)((InodeNumber - BlockStart) * DiskInfo.BytesPerMftRecord + DiskInfo.BytesPerMftRecord)),
                        //Buffer[(InodeNumber - BlockStart) * DiskInfo.BytesPerMftRecord],
			            DiskInfo.BytesPerMftRecord);
	        }

            Time = DateTime.Now;

            EndTime = Time.ToFileTime();

	        if (EndTime > StartTime)
	        {
                ShowDebug(2, String.Format("  Analysis speed: {0:G} items per second",
                      (Int64) MaxInode * 1000 / (EndTime - StartTime)));
	        }

	        if (Data.Running != true)
	        {
                m_msDefragLib.DeleteItemTree(Data.ItemTree);

                Data.ItemTree = null;

                IOWrapper.CloseHandle(Data.Disk.VolumeHandle);

                return false;
	        }

	        /* Setup the ParentDirectory in all the items with the info in the InodeArray. */
            for (Item = m_msDefragLib.TreeSmallest(Data.ItemTree); Item != null; Item = m_msDefragLib.TreeNext(Item))
            {
                Item.ParentDirectory = (ItemStruct)InodeArray.GetValue((Int64)Item.ParentInode);

                if (Item.ParentInode == 5) Item.ParentDirectory = null;
            }

            IOWrapper.CloseHandle(Data.Disk.VolumeHandle);

	        return true;
        }
    }
}
