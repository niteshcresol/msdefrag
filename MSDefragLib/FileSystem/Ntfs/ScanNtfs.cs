using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Runtime.InteropServices;
using MSDefragLib.IO;
using System.Diagnostics;

namespace MSDefragLib.FileSystem.Ntfs
{
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
            set
            {
                Bytes = BitConverter.GetBytes(value);
            }

            get
            {
                return BitConverter.ToUInt64(Bytes, 0); ;
            }
        }
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
            return m_bytes[index];
        }

        public void SetValue(Int64 index, Byte value)
        {
            m_bytes[index] = value;
        }

        public void Initialize(Int64 length)
        {
            if (length != (int)length)
                throw new Exception("This implementation does not support byte arrays with a length bigger than 32 bits");
            m_bytes = new Byte[length];
        }

        public ByteArray ToByteArray(Int64 index, Int64 length)
        {
            ByteArray ba = new ByteArray();
            ba.m_bytes = new Byte[length];
            Array.Copy(m_bytes, index, ba.m_bytes, 0, length);

            return ba;
        }

        //TODO: check if this matters: offset is truncated to 32 bits
        public UInt16Array ToUInt16Array(Int64 index, Int64 length)
        {
            UInt16Array ba = new UInt16Array();
            ba.Initialize(length / 2);
            int jj = 0;
            for (int ii = 0; ii < length; ii += 2)
            {
                ba.SetValue(jj++, BitConverter.ToUInt16(m_bytes, (int)index + ii));
            }
            return ba;
        }

        //TODO: check if this matters: offset is truncated to 32 bits
        public Byte ToByte(ref Int64 offset)
        {
            Byte retValue = m_bytes[(int)offset];
            offset += sizeof(Byte);
            return retValue;
        }

        public Boolean ToBoolean(ref Int64 offset)
        {
            return (ToByte(ref offset) != 0);
        }

        //TODO: check if this matters: offset is truncated to 32 bits
        public UInt16 ToUInt16(ref Int64 offset)
        {
            UInt16 retValue = BitConverter.ToUInt16(m_bytes, (int)offset);
            offset += sizeof(UInt16);
            return retValue;
        }

        //TODO: check if this matters: offset is truncated to 32 bits
        public UInt32 ToUInt32(ref Int64 offset)
        {
            UInt32 retValue = BitConverter.ToUInt32(m_bytes, (int)offset);
            offset += sizeof(UInt32);
            return retValue;
        }

        //TODO: check if this matters: offset is truncated to 32 bits
        public UInt64 ToUInt64(ref Int64 offset)
        {
            UInt64 retValue = BitConverter.ToUInt64(m_bytes, (int)offset);
            offset += sizeof(UInt64);
            return retValue;
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
                throw new Exception("Bad index or length!");

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
            return m_words[index];
        }

        public void SetValue(Int64 index, UInt16 value)
        {
            m_words[index] = value;
        }

        public void Initialize(Int64 length)
        {
            m_words = new UInt16[length];
        }
    }

    public class MSScanNtfsEventArgs : EventArgs
    {
        public UInt32 m_level;
        public String m_message;

        public MSScanNtfsEventArgs(UInt32 level, String message)
        {
            m_level = level;
            m_message = message;
        }
    }

    class ScanNtfs
    {
        const UInt64 MFTBUFFERSIZE = 256 * 1024;
        const UInt64 VIRTUALFRAGMENT = UInt64.MaxValue;

        private MSDefragLib m_msDefragLib;

        public ScanNtfs(MSDefragLib lib)
        {
            m_msDefragLib = lib;
        }

        public delegate void ShowDebugHandler(object sender, EventArgs e);

        public event ShowDebugHandler ShowDebugEvent;

        protected virtual void OnShowDebug(EventArgs e)
        {
            if (ShowDebugEvent != null)
            {
                ShowDebugEvent(this, e);
            }
        }

        public void ShowDebug(UInt32 level, String output)
        {
            MSScanNtfsEventArgs e = new MSScanNtfsEventArgs(level, output);

            if (level < 6)
                OnShowDebug(e);
        }

        /// <summary>
        /// Fixup the raw MFT data that was read from disk. Return true if everything is ok,
        /// false if the MFT data is corrupt (this can also happen when we have read a
        /// record past the end of the MFT, maybe it has shrunk while we were processing).
        /// 
        /// - To protect against disk failure, the last 2 bytes of every sector in the MFT are
        ///   not stored in the sector itself, but in the "Usa" array in the header (described
        ///   by UsaOffset and UsaCount). The last 2 bytes are copied into the array and the
        ///   Update Sequence Number is written in their place.
        ///
        /// - The Update Sequence Number is stored in the first item (item zero) of the "Usa"
        ///   array.
        ///
        /// - The number of bytes per sector is defined in the $Boot record.
        ///
        /// </summary>
        /// <param name="DiskInfo"></param>
        /// <param name="Buffer"></param>
        /// <param name="BufLength"></param>
        /// <returns></returns>
        Boolean FixupRawMftdata(
                    NtfsDiskInfoStructure DiskInfo,
                    ByteArray Buffer,
                    UInt64 BufLength)
        {
            UInt16Array BufferW;
            UInt16Array UpdateSequenceArray;
            Int64 Index;
            Int64 Increment;

            UInt16 i;

            /* Sanity check. */
            Debug.Assert(Buffer != null);

            String recordType = "";

            for (Index = 0; Index < 4; Index++)
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
            NtfsRecordHeader RecordHeader = new NtfsRecordHeader(Buffer, ref tempOffset);

            UpdateSequenceArray = Buffer.ToUInt16Array(RecordHeader.UsaOffset, Buffer.GetLength() - RecordHeader.UsaOffset);

            Increment = (Int64)(DiskInfo.BytesPerSector / sizeof(UInt16));

            Index = Increment - 1;

            for (i = 1; i < RecordHeader.UsaCount; i++)
            {
                /* Check if we are inside the buffer. */
                if (Index * sizeof(UInt16) >= (Int64)BufLength)
                {
                    ShowDebug(0, "Warning: USA data indicates that data is missing, the MFT may be corrupt.");

                    return false;
                }

                /* Check if the last 2 bytes of the sector contain the Update Sequence Number.
                 * If not then return FALSE. */
                if (BufferW.GetValue(Index) - UpdateSequenceArray.GetValue(0) != 0)
                {
                    ShowDebug(0, "Error: USA fixup word is not equal to the Update Sequence Number, the MFT may be corrupt.");

                    return false;
                }

                /* Replace the last 2 bytes in the sector with the value from the Usa array. */
                BufferW.SetValue(Index, UpdateSequenceArray.GetValue(i));

                Index += Increment;
            }

            Buffer = BufferW.ToByteArray(0, BufferW.GetLength());

            return true;
        }

        /// <summary>
        /// Read the data that is specified in a RunData list from disk into memory,
        /// skipping the first Offset bytes. Return a malloc'ed buffer with the data,
        /// or null if error.
        /// </summary>
        /// <param name="DiskInfo"></param>
        /// <param name="RunData"></param>
        /// <param name="RunDataLength"></param>
        /// <param name="Offset"></param>
        /// <param name="WantedLength"></param>
        /// <returns></returns>
        ByteArray ReadNonResidentData(
            NtfsDiskInfoStructure DiskInfo,
            ByteArray RunData,
            UInt64 RunDataLength,
            UInt64 Offset,                    /* Bytes to skip from begin of data. */
            UInt64 WantedLength)              /* Number of bytes to read. */
        {
            UInt64 Index;

            ByteArray Buffer = new ByteArray();
            Buffer.Initialize((Int64)WantedLength);

            UInt64 Lcn;
            UInt64 Vcn;

            int RunOffsetSize;
            int RunLengthSize;

            UlongBytes RunOffset = new UlongBytes();
            UlongBytes RunLength = new UlongBytes();

            UInt64 ExtentVcn;
            UInt64 ExtentLcn;
            UInt64 ExtentLength;

            //Boolean Result;

            //String s1;

            Int16 i;

            ShowDebug(6, String.Format("    Reading {0:G} bytes from offset {0:G}", WantedLength, Offset));

            /* Sanity check. */
            if ((RunData == null) || (RunDataLength == 0)) 
                throw new Exception("Sanity check failed");

            if (WantedLength >= UInt32.MaxValue)
            {
                ShowDebug(2, String.Format("    Cannot read {0:G} bytes, maximum is {1:G}.", WantedLength, UInt32.MaxValue));

                return null;
            }

            //////////////////////////////////////////////////////////////////////////
            // We have to round up the WantedLength to the nearest sector. For some
            // reason or other Microsoft has decided that raw reading from disk can
            // only be done by whole sector, even though ReadFile() accepts it's
            // parameters in bytes.
            //////////////////////////////////////////////////////////////////////////
            if (WantedLength % DiskInfo.BytesPerSector > 0)
            {
                WantedLength = WantedLength + DiskInfo.BytesPerSector - WantedLength % DiskInfo.BytesPerSector;
            }

            /* Allocate the data buffer. Clear the buffer with zero's in case of sparse
             * content.*/
            //Buffer.Initialize();

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
                    throw new Exception("implementation error");
                    ShowDebug(0, "Error: datarun is longer than buffer, the MFT may be corrupt.");

                    return null;
                }

                RunLength.Bytes = new Byte[8];

                for (i = 0; i < RunLengthSize; i++)
                {
                    RunLength.Bytes[i] = runDataValue;

                    Index++;

                    if (Index >= RunDataLength)
                    {
                        throw new Exception("implementation error");
                        ShowDebug(0, "Error: datarun is longer than buffer, the MFT may be corrupt.");

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
                        throw new Exception("implementation error");
                        ShowDebug(0, "Error: datarun is longer than buffer, the MFT may be corrupt.");

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

                /* Determine how many and which bytes we want to read. If we don't need
                 * any bytes from this extent then loop. */
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

                m_msDefragLib.m_data.Disk.ReadFromCluster(ExtentLcn, Buffer.m_bytes,
                    (Int32)(ExtentVcn - Offset), (Int32)ExtentLength);
            }

            /* Return the buffer. */
            return (Buffer);
        }

        /* Read the RunData list and translate into a list of fragments. */
        Boolean TranslateRundataToFragmentlist(
                    InodeDataStructure InodeData,
                    String StreamName,
                    AttributeType StreamType,
                    ByteArray RunData,
                    UInt64 RunDataLength,
                    UInt64 StartingVcn,
                    UInt64 Bytes)
        {
            StreamStructure Stream;

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

            /* Sanity check. */
            if ((m_msDefragLib.m_data == null) || (InodeData == null))
                throw new Exception("Sanity check failed");

            /* Find the stream in the list of streams. If not found then create a new stream. */
            for (Stream = InodeData.m_streams; Stream != null; Stream = Stream.Next)
            {
                if (Stream.StreamType.m_attributeType != StreamType.m_attributeType)
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
                    ShowDebug(6, "    Creating new stream: '" + StreamName + ":" + StreamType.GetStreamTypeName() + "'");
                }
                else
                {
                    ShowDebug(6, "    Creating new stream: ':" + StreamType.GetStreamTypeName() + "'");
                }

                Stream = new StreamStructure();

                Stream.Next = InodeData.m_streams;

                InodeData.m_streams = Stream;

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
                    ShowDebug(6, "    Appending rundata to existing stream: '" + StreamName + ":" + StreamType.GetStreamTypeName());
                }
                else
                {
                    ShowDebug(6, "    Appending rundata to existing stream: ':" + StreamType.GetStreamTypeName());
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
                    ShowDebug(2, String.Format("Error: m_iNode {0:G} already has a list of fragments. LastVcn={1:G}, StartingVCN={2:G}",
                      InodeData.m_iNode, LastFragment.NextVcn, StartingVcn));

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
                        // Exception just here to show ther is an issue
                        throw new Exception("implementation error");
                        ShowDebug(0, String.Format("Error: datarun is longer than buffer, the MFT may be corrupt. m_iNode {0:G}.",
                            InodeData.m_iNode));

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
                            throw new Exception("implementation error");
                            ShowDebug(0, String.Format("Error: datarun is longer than buffer, the MFT may be corrupt. m_iNode {0:G}.",
                                  InodeData.m_iNode));

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
                            throw new Exception("implementation error");
                            ShowDebug(0, String.Format("Error: datarun is longer than buffer, the MFT may be corrupt. m_iNode {0:G}.",
                                InodeData.m_iNode));

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
            Cleanup the m_streams data in an InodeData struct. If CleanFragments is TRUE then
            also cleanup the fragments.
        */
        void CleanupStreams(InodeDataStructure InodeData, Boolean CleanupFragments)
        {
            StreamStructure Stream;
            StreamStructure TempStream;

            FragmentListStruct Fragment;
            FragmentListStruct TempFragment;

            Stream = InodeData.m_streams;

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

            InodeData.m_streams = null;
        }

        /* Construct the full stream name from the filename, the stream name, and the stream type. */
        String ConstructStreamName(String FileName1, String FileName2, StreamStructure Stream)
        {
            String FileName;
            String StreamName;

            AttributeType StreamType = new AttributeType();

            Int32 Length;

            FileName = FileName1;

            if ((FileName == null) || (FileName.Length == 0)) FileName = FileName2;
            if ((FileName != null) && (FileName.Length == 0)) FileName = null;

            StreamName = null;
            StreamType.m_attributeType = AttributeTypeEnum.AttributeInvalid;

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
            if (((StreamName == null) || (StreamName.Length == 0)) && (StreamType == AttributeTypeEnum.AttributeData))
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
                (StreamType == AttributeTypeEnum.AttributeIndexAllocation))
            {
                if ((FileName == null) || (FileName.Length == 0)) return null;

                return FileName;
            }

            /*  
                If the StreamName is empty and the StreamType is Data then return only the
                FileName. The Data stream is the default stream of regular files. 
            */
            if (((StreamName == null) || (StreamName.Length == 0)) &&
                (StreamType.GetStreamTypeName().Length == 0))
            {
                if ((FileName == null) || (FileName.Length == 0)) return (null);

                return FileName;
            }

            Length = 3;

            if (FileName != null) Length += FileName.Length;
            if (StreamName != null) Length += StreamName.Length;

            Length = Length + StreamType.GetStreamTypeName().Length;

            if (Length == 3) return (null);

            StringBuilder p1 = new StringBuilder();
            if (!String.IsNullOrEmpty(FileName))
                p1.Append(FileName);
            p1.Append(":");

            if (!String.IsNullOrEmpty(StreamName))
                p1.Append(StreamName);
            p1.Append(":");
            p1.Append(StreamType.GetStreamTypeName());

            return p1.ToString();
        }

        /*
            Process a list of attributes and store the gathered information in the Item
            struct. Return FALSE if an error occurred.
        */
        void ProcessAttributeList(
                NtfsDiskInfoStructure DiskInfo,
                InodeDataStructure InodeData,
                ByteArray Buffer,
                UInt64 BufLength,
                int Depth)
        {
            ByteArray Buffer2 = new ByteArray();
            Buffer2.Initialize((Int64)DiskInfo.BytesPerMftRecord);

            AttributeList attributeList;

            UInt64 AttributeOffset;

            NtfsFileRecordHeader FileRecordHeader;
            FragmentListStruct Fragment;

            UInt64 RefInode;
            UInt64 BaseInode;
            UInt64 Vcn;
            UInt64 RealVcn;
            UInt64 RefInodeVcn;

            Int32 BytesRead = 0;

            //Boolean Result;

            String p1;
            //String s1;

            /* Sanity checks. */
            if ((Buffer == null) || (BufLength == 0))
                throw new Exception("Sanity check failed");

            if (Depth > 1000)
            {
                throw new Exception("implementation error");
                ShowDebug(0, "Error: infinite attribute loop, the MFT may be corrupt.");

                return;
            }

            ShowDebug(6, String.Format("Processing AttributeList for m_iNode {0:G}, {1:G} bytes", InodeData.m_iNode, BufLength));

            /* Walk through all the attributes and gather information. */
            for (AttributeOffset = 0; AttributeOffset < BufLength; AttributeOffset = AttributeOffset + attributeList.m_length)
            {
                Int64 tempOffset = (Int64)AttributeOffset;

                attributeList = new AttributeList(Buffer, ref tempOffset);

                /* Exit if no more attributes. AttributeLists are usually not closed by the
                 * 0xFFFFFFFF endmarker. Reaching the end of the buffer is therefore normal and
                 * not an error.*/
                if (AttributeOffset + 3 > BufLength) break;
                //if (Attribute.AttributeType == ATTRIBUTE_TYPE.AttributeAll) break;
                if (attributeList.m_attributeType == AttributeTypeEnum.AttributeEndOfList) break;
                if (attributeList.m_length < 3) break;
                if (AttributeOffset + attributeList.m_length > BufLength) break;

                /* Extract the referenced m_iNode. If it's the same as the calling m_iNode then 
                 * ignore (if we don't ignore then the program will loop forever, because for 
                 * some reason the info in the calling m_iNode is duplicated here...). */
                RefInode = (UInt64)attributeList.m_fileReferenceNumber.m_iNodeNumberLowPart +
                        ((UInt64)attributeList.m_fileReferenceNumber.m_iNodeNumberHighPart << 32);

                if (RefInode == InodeData.m_iNode) continue;

                /* Show debug message. */
                ShowDebug(6, "    List attribute: " + attributeList.m_attributeType.GetStreamTypeName());
                ShowDebug(6, String.Format("      m_lowestVcn = {0:G}, RefInode = {1:G}, InodeSequence = {2:G}, m_instance = {3:G}",
                      attributeList.m_lowestVcn, RefInode, attributeList.m_fileReferenceNumber.m_sequenceNumber, attributeList.m_instance));

                /* Extract the streamname. I don't know why AttributeLists can have names, and
                 * the name is not used further down. It is only extracted for debugging 
                 * purposes. */
                if (attributeList.m_nameLength > 0)
                {
                    p1 = String.Empty;

                    for (Int64 ii = 0; ii < attributeList.m_nameLength; ii++)
                    {
                        p1 += (Char)Buffer.GetValue((Int64)((Int64)AttributeOffset + attributeList.m_nameOffset + ii));
                    }

                    ShowDebug(6, "      AttributeList name = '" + p1 + "'");
                }

                /* Find the fragment in the MFT that contains the referenced m_iNode. */
                Vcn = 0;
                RealVcn = 0;
                RefInodeVcn = RefInode * DiskInfo.BytesPerMftRecord / (DiskInfo.BytesPerSector * DiskInfo.SectorsPerCluster);

                for (Fragment = InodeData.m_mftDataFragments; Fragment != null; Fragment = Fragment.Next)
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
                    ShowDebug(6, String.Format("      Error: m_iNode {0:G} is an extension of m_iNode {1:G}, but does not exist (outside the MFT).",
                            RefInode, InodeData.m_iNode));

                    continue;
                }

                UInt64 tempVcn;

                /* Fetch the record of the referenced m_iNode from disk. */
                tempVcn = (Fragment.Lcn - RealVcn) * DiskInfo.BytesPerSector *
                        DiskInfo.SectorsPerCluster + RefInode * DiskInfo.BytesPerMftRecord;

                Byte[] tempBuffer = new Byte[DiskInfo.BytesPerMftRecord];

                m_msDefragLib.m_data.Disk.ReadFromCluster(tempVcn, Buffer2.m_bytes, 0,
                    (Int32)DiskInfo.BytesPerMftRecord);

                /* Fixup the raw data. */
                if (FixupRawMftdata(DiskInfo, Buffer2, DiskInfo.BytesPerMftRecord) == false)
                {
                    ShowDebug(2, String.Format("The error occurred while processing m_iNode {0:G}", RefInode));

                    continue;
                }

                /* If the m_iNode is not in use then skip. */
                tempOffset = 0;

                FileRecordHeader = new NtfsFileRecordHeader(Buffer2, ref tempOffset);

                if ((FileRecordHeader.Flags & 1) != 1)
                {
                    ShowDebug(6, String.Format("      Referenced m_iNode {0:G} is not in use.", RefInode));

                    continue;
                }

                /* If the BaseInode inside the m_iNode is not the same as the calling m_iNode then skip. */
                BaseInode = (UInt64)FileRecordHeader.BaseFileRecord.m_iNodeNumberLowPart +
                        ((UInt64)FileRecordHeader.BaseFileRecord.m_iNodeNumberHighPart << 32);

                if (InodeData.m_iNode != BaseInode)
                {
                    ShowDebug(6, String.Format("      Warning: m_iNode {0:G} is an extension of m_iNode {1:G}, but thinks it's an extension of m_iNode {2:G}.",
                            RefInode, InodeData.m_iNode, BaseInode));

                    continue;
                }

                /* Process the list of attributes in the m_iNode, by recursively calling the ProcessAttributes() subroutine. */
                ShowDebug(6, String.Format("      Processing m_iNode {0:G} m_instance {1:G}", RefInode, attributeList.m_instance));

                ProcessAttributes(
                    DiskInfo,
                    InodeData,
                    Buffer2.ToByteArray(FileRecordHeader.AttributeOffset, Buffer2.GetLength() - FileRecordHeader.AttributeOffset),
                    DiskInfo.BytesPerMftRecord - FileRecordHeader.AttributeOffset,
                    attributeList.m_instance, Depth + 1);

                ShowDebug(6, String.Format("      Finished processing m_iNode {0:G} m_instance {1:G}", RefInode, attributeList.m_instance));
            }
        }

        /*
            Process a list of attributes and store the gathered information in the Item
            struct. Return FALSE if an error occurred.
        */
        Boolean ProcessAttributes(
            NtfsDiskInfoStructure DiskInfo,
            InodeDataStructure InodeData,
            ByteArray Buffer,
            UInt64 BufLength,
            UInt32 Instance,
            int Depth)
        {
            ByteArray Buffer2;

            UInt64 Buffer2Length;
            UInt32 AttributeOffset;

            Attribute attribute;
            ResidentAttribute residentAttribute;
            NonResidentAttribute nonResidentAttribute;
            StandardInformation standardInformation;
            FileNameAttribute fileNameAttribute;

            String p1;

            /* Walk through all the attributes and gather information. AttributeLists are
             * skipped and interpreted later.*/
            for (AttributeOffset = 0; AttributeOffset < BufLength; AttributeOffset += attribute.m_length)
            {
                //Attribute = (ATTRIBUTE)Buffer[AttributeOffset];
                Int64 tempOffset = (Int64)AttributeOffset;

                attribute = new Attribute(Buffer, ref tempOffset);

                if (attribute.m_attributeType == AttributeTypeEnum.AttributeEndOfList)
                {
                    break;
                }

                /* Exit the loop if end-marker. */
                if ((AttributeOffset + 4 <= BufLength) &&
                    (attribute.m_attributeType == AttributeTypeEnum.AttributeInvalid))
                {
                    break;
                }

                /* Sanity check. */
                if ((AttributeOffset + 4 > BufLength) ||
                    (attribute.m_length < 3) ||
                    (AttributeOffset + attribute.m_length > BufLength))
                {
                    throw new Exception("implementation error");
                    ShowDebug(0, String.Format("Error: attribute in m_iNode {0:G} is bigger than the data, the MFT may be corrupt.", InodeData.m_iNode));
                    ShowDebug(2, String.Format("  BufLength={0:G}, AttributeOffset={1:G}, AttributeLength={2:G}({3:X})",
                            BufLength, AttributeOffset, attribute.m_length, attribute.m_length));

                    //m_msDefragLib.ShowHex(Data, Buffer.m_bytes, BufLength);

                    return false;
                }

                /* Skip AttributeList's for now. */
                if (attribute.m_attributeType == AttributeTypeEnum.AttributeAttributeList)
                {
                    continue;
                }

                /* If the Instance does not equal the m_attributeNumber then ignore the attribute.
                 * This is used when an AttributeList is being processed and we only want a specific
                 * instance. */
                if ((Instance != UInt16.MaxValue) && (Instance != attribute.m_attributeNumber))
                {
                    continue;
                }

                /* Show debug message. */
                ShowDebug(6, String.Format("  Attribute {0:G}: {1:G}", attribute.m_attributeNumber, attribute.m_attributeType.GetStreamTypeName()));

                if (attribute.m_nonResident == false)
                {
                    tempOffset = (Int64)AttributeOffset;

                    residentAttribute = new ResidentAttribute(Buffer, ref tempOffset);

                    /* The AttributeFileName (0x30) contains the filename and the link to the parent directory. */
                    if (attribute.m_attributeType == AttributeTypeEnum.AttributeFileName)
                    {
                        tempOffset = (Int64)(AttributeOffset + residentAttribute.ValueOffset);

                        fileNameAttribute = new FileNameAttribute(Buffer, ref tempOffset);

                        InodeData.m_parentInode = fileNameAttribute.m_parentDirectory.m_iNodeNumberLowPart +
                            (((UInt32)fileNameAttribute.m_parentDirectory.m_iNodeNumberHighPart) << 32);

                        if (fileNameAttribute.m_nameLength > 0)
                        {
                            /* Extract the filename. */
                            p1 = fileNameAttribute.m_name.ToString();

                            /* Save the filename in either the Long or the Short filename. We only
                             * save the first filename, any additional filenames are hard links. They
                             * might be useful for an optimization algorithm that sorts by filename,
                             * but which of the hardlinked names should it sort? So we only store the
                             * first filename.*/
                            if (fileNameAttribute.m_nameType == 2)
                            {
                                if (InodeData.m_shortFilename == null)
                                {
                                    InodeData.m_shortFilename = p1;

                                    ShowDebug(6, String.Format("    Short filename = '{0:G}'", p1));
                                }
                            }
                            else
                            {
                                if (InodeData.m_longFilename == null)
                                {
                                    InodeData.m_longFilename = p1;

                                    ShowDebug(6, String.Format("    Long filename = '{0:G}'", p1));
                                }
                            }
                        }
                    }

                    /*  
                        The AttributeStandardInformation (0x10) contains the m_creationTime, m_lastAccessTime,
                        the m_mftChangeTime, and the file attributes.
                    */
                    if (attribute.m_attributeType == AttributeTypeEnum.AttributeStandardInformation)
                    {
                        tempOffset = (Int64)(AttributeOffset + residentAttribute.ValueOffset);

                        standardInformation = new StandardInformation(Buffer, ref tempOffset);

                        InodeData.m_creationTime = standardInformation.CreationTime;
                        InodeData.m_mftChangeTime = standardInformation.MftChangeTime;
                        InodeData.m_lastAccessTime = standardInformation.LastAccessTime;
                    }

                    /* The value of the AttributeData (0x80) is the actual data of the file. */
                    if (attribute.m_attributeType == AttributeTypeEnum.AttributeData)
                    {
                        InodeData.m_totalBytes = residentAttribute.ValueLength;
                    }
                }
                else
                {
                    tempOffset = (Int64)AttributeOffset;

                    nonResidentAttribute = new NonResidentAttribute(Buffer, ref tempOffset);

                    /* Save the length (number of bytes) of the data. */
                    if ((attribute.m_attributeType == AttributeTypeEnum.AttributeData) &&
                        (InodeData.m_totalBytes == 0))
                    {
                        InodeData.m_totalBytes = nonResidentAttribute.m_dataSize;
                    }

                    /* Extract the streamname. */
                    p1 = "";

                    Int64 ii = 0;

                    tempOffset = (Int64)(AttributeOffset + attribute.m_nameOffset);

                    for (ii = 0; ii < attribute.m_nameLength; ii++)
                    {
                        p1 += (Char)Buffer.ToUInt16(ref tempOffset);
                    }

                    /* Create a new stream with a list of fragments for this data. */
                    TranslateRundataToFragmentlist(InodeData, p1, attribute.m_attributeType,
                            Buffer.ToByteArray((Int64)(AttributeOffset + nonResidentAttribute.m_runArrayOffset), Buffer.GetLength() - (Int64)((AttributeOffset + nonResidentAttribute.m_runArrayOffset))),
                            attribute.m_length - nonResidentAttribute.m_runArrayOffset,
                            nonResidentAttribute.m_startingVcn, nonResidentAttribute.m_dataSize);

                    /* Special case: If this is the $MFT then save data. */
                    if (InodeData.m_iNode == 0)
                    {
                        if ((attribute.m_attributeType == AttributeTypeEnum.AttributeData) &&
                            (InodeData.m_mftDataFragments == null))
                        {
                            InodeData.m_mftDataFragments = InodeData.m_streams.Fragments;
                            InodeData.m_mftDataLength = nonResidentAttribute.m_dataSize;
                        }

                        if ((attribute.m_attributeType== AttributeTypeEnum.AttributeBitmap) &&
                            (InodeData.m_mftBitmapFragments == null))
                        {
                            InodeData.m_mftBitmapFragments = InodeData.m_streams.Fragments;
                            InodeData.m_mftBitmapLength = nonResidentAttribute.m_dataSize;
                        }
                    }
                }
            }

            /* Walk through all the attributes and interpret the AttributeLists. We have to
             * do this after the DATA and BITMAP attributes have been interpreted, because
             * some MFT's have an AttributeList that is stored in fragments that are
             * defined in the DATA attribute, and/or contain a continuation of the DATA or
             * BITMAP attributes.*/
            for (AttributeOffset = 0; AttributeOffset < BufLength; AttributeOffset += attribute.m_length)
            {
                Int64 tempOffset = (Int64)AttributeOffset;

                attribute = new Attribute(Buffer, ref tempOffset);

                if (attribute.m_attributeType == AttributeTypeEnum.AttributeEndOfList)
                {
                    break;
                }

                if (attribute.m_attributeType == AttributeTypeEnum.AttributeInvalid)
                {
                    break;
                }

                if (attribute.m_attributeType != AttributeTypeEnum.AttributeAttributeList)
                {
                    continue;
                }

                ShowDebug(6, String.Format("  Attribute {0:G}: {1:G}", attribute.m_attributeNumber, attribute.m_attributeType.GetStreamTypeName()));

                if (attribute.m_nonResident == false)
                {
                    Int64 tempOffset2 = (Int64)AttributeOffset;
                    residentAttribute = new ResidentAttribute(Buffer, ref tempOffset2);

                    //residentAttribute.m_attribute = attribute;

                    ProcessAttributeList(DiskInfo, InodeData,
                            Buffer.ToByteArray((Int64)(AttributeOffset + residentAttribute.ValueOffset), Buffer.GetLength() - (Int64)(AttributeOffset + residentAttribute.ValueOffset)),
                            residentAttribute.ValueLength, Depth);
                }
                else
                {
                    Int64 tempOffset3 = (Int64)AttributeOffset;
                    nonResidentAttribute = new NonResidentAttribute(Buffer, ref tempOffset3);
                    //nonResidentAttribute = new NonResidentAttribute();

                    //nonResidentAttribute.m_attribute = attribute;

                    Buffer2Length = nonResidentAttribute.m_dataSize;
                    // Buffer2Length = 512;

                    Buffer2 = ReadNonResidentData(DiskInfo,
                            Buffer.ToByteArray((Int64)(AttributeOffset + nonResidentAttribute.m_runArrayOffset), Buffer.GetLength() - (Int64)(AttributeOffset + nonResidentAttribute.m_runArrayOffset)),
                            attribute.m_length - nonResidentAttribute.m_runArrayOffset, 0, Buffer2Length);

                    ProcessAttributeList(DiskInfo, InodeData, Buffer2, Buffer2Length, Depth);
                }
            }

            return true;
        }

        Boolean InterpretMftRecord(
            NtfsDiskInfoStructure DiskInfo,
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
            NtfsFileRecordHeader FileRecordHeader;

            InodeDataStructure InodeData = new InodeDataStructure();

            ItemStruct Item;
            StreamStructure Stream;

            UInt64 BaseInode;

            /* If the record is not in use then quietly exit. */
            //FileRecordHeader = (FILE_RECORD_HEADER)Buffer;
            Int64 tempOffset = 0;
            FileRecordHeader = new NtfsFileRecordHeader(Buffer, ref tempOffset);

            if ((FileRecordHeader.Flags & 1) != 1)
            {
                ShowDebug(6, String.Format("m_iNode {0:G} is not in use.", InodeNumber));

                return false;
            }

            /*
                If the record has a BaseFileRecord then ignore it. It is used by an
                AttributeAttributeList as an extension of another m_iNode, it's not an
                m_iNode by itself. 
            */
            BaseInode = (UInt64)FileRecordHeader.BaseFileRecord.m_iNodeNumberLowPart +
            ((UInt64)FileRecordHeader.BaseFileRecord.m_iNodeNumberHighPart << 32);

            if (BaseInode != 0)
            {
                ShowDebug(6, String.Format("Ignoring m_iNode {0:G}, it's an extension of m_iNode {1:G}", InodeNumber, BaseInode));

                return true;
            }

            ShowDebug(6, String.Format("Processing m_iNode {0:G}...", InodeNumber));

            /* Show a warning if the Flags have an unknown value. */
            if ((FileRecordHeader.Flags & 252) != 0)
            {
                ShowDebug(6, String.Format("  m_iNode {0:G} has Flags = {1:G}", InodeNumber, FileRecordHeader.Flags));
            }

            /*
                I think the MFTRecordNumber should always be the InodeNumber, but it's an XP
                extension and I'm not sure about Win2K.

                Note: why is the MFTRecordNumber only 32 bit? m_iNode numbers are 48 bit.
            */
            if (FileRecordHeader.MFTRecordNumber != InodeNumber)
            {
                ShowDebug(6, String.Format("  Warning: m_iNode {0:G} contains a different MFTRecordNumber {1:G}",
                      InodeNumber, FileRecordHeader.MFTRecordNumber));
            }

            /* Sanity check. */
            if (FileRecordHeader.AttributeOffset >= BufLength)
            {
                throw new Exception("implementation error");
                ShowDebug(0, String.Format("Error: attributes in m_iNode {0:G} are outside the FILE record, the MFT may be corrupt.",
                      InodeNumber));

                return false;
            }

            if (FileRecordHeader.BytesInUse > BufLength)
            {
                throw new Exception("implementation error");
                ShowDebug(0, String.Format("Error: in m_iNode {0:G} the record is bigger than the size of the buffer, the MFT may be corrupt.",
                      InodeNumber));

                return false;
            }

            /* Initialize the InodeData struct. */
            InodeData.m_iNode = InodeNumber;                                /* The m_iNode number. */
            InodeData.m_parentInode = 5;            /* The m_iNode number of the parent directory. */
            InodeData.m_directory = false;

            if ((FileRecordHeader.Flags & 2) == 2) InodeData.m_directory = true;

            InodeData.m_longFilename = null;                                   /* Long filename. */
            InodeData.m_shortFilename = null;                       /* Short filename (8.3 DOS). */
            InodeData.m_creationTime = 0;                                 /* 1 second = 10000000 */
            InodeData.m_mftChangeTime = 0;
            InodeData.m_lastAccessTime = 0;
            InodeData.m_totalBytes = 0;                                  /* Size of the $DATA stream. */
            InodeData.m_streams = null;                                 /* List of StreamStruct. */
            InodeData.m_mftDataFragments = MftDataFragments;
            InodeData.m_mftDataLength = MftDataBytes;
            InodeData.m_mftBitmapFragments = null;
            InodeData.m_mftBitmapLength = 0;

            /* Make sure that directories are always created. */
            if (InodeData.m_directory == true)
            {
                AttributeType attributeType = new AttributeType();
                attributeType.m_attributeType = AttributeTypeEnum.AttributeIndexAllocation;
                TranslateRundataToFragmentlist(InodeData, "$I30", attributeType, null, 0, 0, 0);
            }

            /* Interpret the attributes. */
            ProcessAttributes(DiskInfo, InodeData,
                Buffer.ToByteArray(FileRecordHeader.AttributeOffset, Buffer.GetLength() - FileRecordHeader.AttributeOffset),
                BufLength - FileRecordHeader.AttributeOffset, UInt16.MaxValue, 0);

            /* Save the MftDataFragments, MftDataBytes, MftBitmapFragments, and MftBitmapBytes. */
            if (InodeNumber == 0)
            {
                MftDataFragments = InodeData.m_mftDataFragments;
                MftDataBytes = InodeData.m_mftDataLength;
                MftBitmapFragments = InodeData.m_mftBitmapFragments;
                MftBitmapBytes = InodeData.m_mftBitmapLength;
            }

            /* Create an item in the Data->ItemTree for every stream. */
            Stream = InodeData.m_streams;

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

                Item.LongFilename = ConstructStreamName(InodeData.m_longFilename, InodeData.m_shortFilename, Stream);
                Item.LongPath = null;

                Item.ShortFilename = ConstructStreamName(InodeData.m_shortFilename, InodeData.m_longFilename, Stream);
                Item.ShortPath = null;

                Item.Bytes = InodeData.m_totalBytes;

                if (Stream != null) Item.Bytes = Stream.Bytes;

                Item.Clusters = 0;

                if (Stream != null) Item.Clusters = Stream.Clusters;

                Item.CreationTime = InodeData.m_creationTime;
                Item.MftChangeTime = InodeData.m_mftChangeTime;
                Item.LastAccessTime = InodeData.m_lastAccessTime;
                Item.Fragments = null;

                if (Stream != null) Item.Fragments = Stream.Fragments;

                Item.ParentInode = InodeData.m_parentInode;
                Item.Directory = InodeData.m_directory;
                Item.Unmovable = false;
                Item.Exclude = false;
                Item.SpaceHog = false;

                /* Increment counters. */
                if (Item.Directory == true)
                {
                    m_msDefragLib.m_data.CountDirectories++;
                }

                m_msDefragLib.m_data.CountAllFiles++;

                if ((Stream != null) && (Stream.StreamType == AttributeTypeEnum.AttributeData))
                {
                    m_msDefragLib.m_data.CountAllBytes += InodeData.m_totalBytes;
                }

                if (Stream != null) m_msDefragLib.m_data.CountAllClusters += Stream.Clusters;

                if (m_msDefragLib.FragmentCount(Item) > 1)
                {
                    m_msDefragLib.m_data.CountFragmentedItems++;
                    m_msDefragLib.m_data.CountFragmentedBytes += InodeData.m_totalBytes;

                    if (Stream != null) m_msDefragLib.m_data.CountFragmentedClusters += Stream.Clusters;
                }

                /* Add the item record to the sorted item tree in memory. */
                m_msDefragLib.TreeInsert(Item);

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
                    InodeArray.SetValue(Item, (Int64)InodeNumber);
                }

                /* Draw the item on the screen. */
                //jkGui->ShowAnalyze(Data,Item);

                if (m_msDefragLib.m_data.RedrawScreen == 0)
                {
                    m_msDefragLib.ColorizeItem(Item, 0, 0, false);
                }
                else
                {
                    m_msDefragLib.ShowDiskmap();
                }

                if (Stream != null) Stream = Stream.Next;

            } while (Stream != null);

            /* Cleanup and return true. */
            CleanupStreams(InodeData, false);

            return true;
        }

        //////////////////////////////////////////////////////////////////////////
        //
        // Load the MFT into a list of ItemStruct records in memory.
        //
        //////////////////////////////////////////////////////////////////////////
        public Boolean AnalyzeNtfsVolume()
        {
            NtfsDiskInfoStructure DiskInfo = new NtfsDiskInfoStructure();

            ByteArray Buffer = new ByteArray();

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

            Byte[] BitmapMasks = { 1, 2, 4, 8, 16, 32, 64, 128 };

            Boolean Result = false;

            DateTime Time;

            Int64 StartTime = 0;
            Int64 EndTime = 0;

            UInt64 u1 = 0;

            Buffer.Initialize((Int64)MFTBUFFERSIZE);

            // Read the boot block from the disk.
            FS.IBootSector bootSector = m_msDefragLib.m_data.Disk.BootSector;

            // Test if the boot block is an NTFS boot block.
            if (bootSector.Filesystem != FS.Filesystem.NTFS)
            {
                ShowDebug(2, "This is not an NTFS disk (different cookie).");

                return false;
            }

            /* Extract data from the bootblock. */
            DiskInfo.BytesPerSector = bootSector.BytesPerSector;
            DiskInfo.SectorsPerCluster = bootSector.SectorsPerCluster;

            DiskInfo.TotalSectors = bootSector.TotalSectors;
            DiskInfo.MftStartLcn = bootSector.Mft1StartLcn;
            DiskInfo.Mft2StartLcn = bootSector.Mft2StartLcn;

            UInt64 ClustersPerMftRecord = bootSector.ClustersPerMftRecord;
            DiskInfo.ClustersPerIndexRecord = bootSector.ClustersPerIndexRecord;

            if (ClustersPerMftRecord >= 128)
            {
                DiskInfo.BytesPerMftRecord = (UInt64)(1 << (256 - (Int16)ClustersPerMftRecord));
            }
            else
            {
                DiskInfo.BytesPerMftRecord = ClustersPerMftRecord * DiskInfo.BytesPerSector * DiskInfo.SectorsPerCluster;
            }

            m_msDefragLib.m_data.BytesPerCluster = DiskInfo.BytesPerSector * DiskInfo.SectorsPerCluster;

            if (DiskInfo.SectorsPerCluster > 0)
            {
                m_msDefragLib.m_data.TotalClusters = DiskInfo.TotalSectors / DiskInfo.SectorsPerCluster;
            }

            ShowDebug(0, "This is an NTFS disk.");

            ShowDebug(2, String.Format("  Disk cookie: {0:X}", bootSector.OemId));
            ShowDebug(2, String.Format("  BytesPerSector: {0:G}", DiskInfo.BytesPerSector));
            ShowDebug(2, String.Format("  TotalSectors: {0:G}", DiskInfo.TotalSectors));
            ShowDebug(2, String.Format("  SectorsPerCluster: {0:G}", DiskInfo.SectorsPerCluster));

            ShowDebug(2, String.Format("  SectorsPerTrack: {0:G}", bootSector.SectorsPerTrack));
            ShowDebug(2, String.Format("  NumberOfHeads: {0:G}", bootSector.NumberOfHeads));
            ShowDebug(2, String.Format("  MftStartLcn: {0:G}", DiskInfo.MftStartLcn));
            ShowDebug(2, String.Format("  Mft2StartLcn: {0:G}", DiskInfo.Mft2StartLcn));
            ShowDebug(2, String.Format("  BytesPerMftRecord: {0:G}", DiskInfo.BytesPerMftRecord));
            ShowDebug(2, String.Format("  ClustersPerIndexRecord: {0:G}", DiskInfo.ClustersPerIndexRecord));

            ShowDebug(2, String.Format("  MediaType: {0:X}", bootSector.MediaType));

            ShowDebug(2, String.Format("  VolumeSerialNumber: {0:X}", bootSector.Serial));

            /* 
                Calculate the size of first 16 Inodes in the MFT. The Microsoft defragmentation
                API cannot move these inodes.
            */
            m_msDefragLib.m_data.Disk.MftLockedClusters = DiskInfo.BytesPerSector * DiskInfo.SectorsPerCluster / DiskInfo.BytesPerMftRecord;

            /*
                Read the $MFT record from disk into memory, which is always the first record in
                the MFT.
            */
            UInt64 tempLcn = DiskInfo.MftStartLcn * DiskInfo.BytesPerSector * DiskInfo.SectorsPerCluster;
            //Trans.QuadPart         = DiskInfo.MftStartLcn * DiskInfo.BytesPerSector * DiskInfo.SectorsPerCluster;

            m_msDefragLib.m_data.Disk.ReadFromCluster(tempLcn, Buffer.m_bytes, 0,
                (Int32)DiskInfo.BytesPerMftRecord);

            /* Fixup the raw data from disk. This will also test if it's a valid $MFT record. */
            if (FixupRawMftdata(DiskInfo, Buffer, DiskInfo.BytesPerMftRecord) == false)
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

            Result = InterpretMftRecord(DiskInfo, null, 0, 0,
                ref MftDataFragments, ref MftDataBytes, ref MftBitmapFragments, ref MftBitmapBytes,
                Buffer, DiskInfo.BytesPerMftRecord);

            if ((Result == false) ||
                (MftDataFragments == null) || (MftDataBytes == 0) ||
                (MftBitmapFragments == null) || (MftBitmapBytes == 0))
            {
                ShowDebug(2, "Fatal error, cannot process this disk.");

                m_msDefragLib.DeleteItemTree(m_msDefragLib.m_data.ItemTree);

                m_msDefragLib.m_data.ItemTree = null;

                m_msDefragLib.m_data.Disk.Close();
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

                    UInt64 numClusters = Fragment.NextVcn - Vcn;
                    Int32 numBytes = (Int32)(numClusters * DiskInfo.BytesPerSector * DiskInfo.SectorsPerCluster);
                    Int32 startIndex = (Int32)(RealVcn * DiskInfo.BytesPerSector * DiskInfo.SectorsPerCluster);

                    ShowDebug(6, String.Format("    Reading {0:G} clusters ({1:G} bytes) from LCN={2:G}", numClusters, numBytes, Fragment.Lcn));

                    m_msDefragLib.m_data.Disk.ReadFromCluster(tempLcn, MftBitmap.m_bytes,
                        startIndex, numBytes);

                    RealVcn += Fragment.NextVcn - Vcn;
                }

                Vcn = Fragment.NextVcn;
            }

            //////////////////////////////////////////////////////////////////////////
            //
            //    Construct an array of all the items in memory, indexed by m_iNode.
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

                m_msDefragLib.DeleteItemTree(m_msDefragLib.m_data.ItemTree);

                m_msDefragLib.m_data.ItemTree = null;

                return false;
            }

            InodeArray.SetValue(m_msDefragLib.m_data.ItemTree, 0);

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

            m_msDefragLib.m_data.PhaseDone = 0;
            m_msDefragLib.m_data.PhaseTodo = 0;

            Time = DateTime.Now;

            StartTime = Time.ToFileTime();

            for (InodeNumber = 1; InodeNumber < MaxInode; InodeNumber++)
            {
                Byte val = MftBitmap.GetValue((Int64)(InodeNumber >> 3));
                Boolean mask = ((val & BitmapMasks[InodeNumber % 8]) == 0);

                if (mask == false) continue;

                m_msDefragLib.m_data.PhaseTodo++;
            }

            for (InodeNumber = 1; InodeNumber < MaxInode; InodeNumber++)
            {
                //if (Data.Running != true) break;

                Int64 tempOffset = (Int64)(InodeNumber >> 3);

                /*  Ignore the m_iNode if the bitmap says it's not in use. */
                if ((MftBitmap.ToByte(ref tempOffset) & BitmapMasks[InodeNumber % 8]) == 0)
                {
                    ShowDebug(6, String.Format("m_iNode {0:G} is not in use.", InodeNumber));

                    continue;
                }

                /* Update the progress counter. */
                m_msDefragLib.m_data.PhaseDone++;

                /* Read a block of inode's into memory. */
                if (InodeNumber >= BlockEnd)
                {
                    /* Slow the program down to the percentage that was specified on the command line. */
                    m_msDefragLib.SlowDown();

                    BlockStart = InodeNumber;
                    BlockEnd = BlockStart + MFTBUFFERSIZE / DiskInfo.BytesPerMftRecord;

                    if (BlockEnd > MftBitmapBytes * 8) BlockEnd = MftBitmapBytes * 8;

                    while (Fragment != null)
                    {
                        /* Calculate m_iNode at the end of the fragment. */
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

                        if (Fragment != null)
                        {
                            ShowDebug(6, String.Format("  Extent Lcn={0:G}, RealVcn={1:G}, Size={2:G}",
                                  Fragment.Lcn, RealVcn, Fragment.NextVcn - Vcn));
                        }
                    }

                    if (Fragment == null) break;
                    if (BlockEnd >= u1) BlockEnd = u1;

                    tempLcn = (Fragment.Lcn - RealVcn) * DiskInfo.BytesPerSector *
                            DiskInfo.SectorsPerCluster + BlockStart * DiskInfo.BytesPerMftRecord;

                    //Trans.QuadPart = (Fragment.Lcn - RealVcn) * DiskInfo.BytesPerSector *
                    //        DiskInfo.SectorsPerCluster + BlockStart * DiskInfo.BytesPerMftRecord;

                    ShowDebug(6, String.Format("Reading block of {0:G} Inodes from MFT into memory, {1:G} bytes from LCN={2:G}",
                          BlockEnd - BlockStart, ((BlockEnd - BlockStart) * DiskInfo.BytesPerMftRecord),
                          tempLcn / (DiskInfo.BytesPerSector * DiskInfo.SectorsPerCluster)));

                    m_msDefragLib.m_data.Disk.ReadFromCluster(tempLcn,
                        Buffer.m_bytes, 0, (Int32)((BlockEnd - BlockStart) * DiskInfo.BytesPerMftRecord));
                }

                /* Fixup the raw data of this m_iNode. */
                if (FixupRawMftdata(DiskInfo,
                        Buffer.ToByteArray((Int64)((InodeNumber - BlockStart) * DiskInfo.BytesPerMftRecord), Buffer.GetLength() - (Int64)((InodeNumber - BlockStart) * DiskInfo.BytesPerMftRecord)),
                    //(ByteArray)Buffer.m_bytes.GetValue((Int64)((InodeNumber - BlockStart) * DiskInfo.BytesPerMftRecord),Buffer.m_bytes.m_length - 1),
                        DiskInfo.BytesPerMftRecord) == false)
                {
                    ShowDebug(2, String.Format("The error occurred while processing m_iNode {0:G} (max {0:G})",
                            InodeNumber, MaxInode));

                    continue;
                }

                /* Interpret the m_iNode's attributes. */
                Result = InterpretMftRecord(DiskInfo, InodeArray, InodeNumber, MaxInode,
                        ref MftDataFragments, ref MftDataBytes, ref MftBitmapFragments, ref MftBitmapBytes,
                        Buffer.ToByteArray(
                            (Int64)((InodeNumber - BlockStart) * DiskInfo.BytesPerMftRecord),
                            (Int64)DiskInfo.BytesPerMftRecord),
                    //(ByteArray)Buffer.m_bytes.GetValue(
                    //    (Int64)((InodeNumber - BlockStart) * DiskInfo.BytesPerMftRecord),
                    //    (Int64)((InodeNumber - BlockStart) * DiskInfo.BytesPerMftRecord + DiskInfo.BytesPerMftRecord)),
                    //Buffer[(InodeNumber - BlockStart) * DiskInfo.BytesPerMftRecord],
                        DiskInfo.BytesPerMftRecord);

                if (m_msDefragLib.m_data.PhaseDone % 50 == 0)
                    ShowDebug(1, "Done: " + m_msDefragLib.m_data.PhaseDone + "/" + m_msDefragLib.m_data.PhaseTodo);
            }

            Time = DateTime.Now;

            EndTime = Time.ToFileTime();

            if (EndTime > StartTime)
            {
                ShowDebug(2, String.Format("  Analysis speed: {0:G} items per second",
                      (Int64)MaxInode * 1000 / (EndTime - StartTime)));
            }

            if (m_msDefragLib.m_data.Running != RunningState.RUNNING)
            {
                m_msDefragLib.DeleteItemTree(m_msDefragLib.m_data.ItemTree);

                m_msDefragLib.m_data.ItemTree = null;

                m_msDefragLib.m_data.Disk.Close();
                return false;
            }

            /* Setup the ParentDirectory in all the items with the info in the InodeArray. */
            for (Item = m_msDefragLib.TreeSmallest(m_msDefragLib.m_data.ItemTree); Item != null; Item = m_msDefragLib.TreeNext(Item))
            {
                Item.ParentDirectory = (ItemStruct)InodeArray.GetValue((Int64)Item.ParentInode);

                if (Item.ParentInode == 5) Item.ParentDirectory = null;
            }

            m_msDefragLib.m_data.Disk.Close();
            return true;
        }
    }
}
