using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Runtime.InteropServices;
using MSDefragLib.IO;

namespace MSDefragLib.FileSystem.Ntfs
{
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

    class Scan
    {
        const UInt64 MFTBUFFERSIZE = 256 * 1024;

        private MSDefragLib m_msDefragLib;

        public Scan(MSDefragLib lib)
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
            {
                Console.Out.WriteLine(output);
                OnShowDebug(e);
            }
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
        /// </summary>
        /// <param name="DiskInfo"></param>
        /// <param name="Buffer"></param>
        /// <param name="BufLength"></param>
        /// <returns></returns>
        Boolean FixupRawMftdata(
                    DiskInfoStructure DiskInfo,
                    ByteArray buffer,
                    UInt64 BufLength)
        {
            /* Sanity check. */
            Debug.Assert(buffer != null);

            Int64 ind = 0;
            UInt32 record = buffer.ToUInt32(ref ind);
            /* If this is not a FILE record then return FALSE. */
            if (record != 0x454c4946)
            {
                ShowDebug(2, "This is not a valid MFT record, it does not begin with FILE (maybe trying to read past the end?).");
                //m_msDefragLib.ShowHex(Data, Buffer.m_bytes, BufLength);
                return false;
            }

            /*
                Walk through all the sectors and restore the last 2 bytes with the value
                from the Usa array. If we encounter bad sector data then return with FALSE. 
            */
            UInt16Array BufferW = buffer.ToUInt16Array(0, buffer.GetLength());

            RecordHeader RecordHeader = RecordHeader.Parse(Helper.BinaryReader(buffer));
            UInt16Array UpdateSequenceArray = buffer.ToUInt16Array(RecordHeader.UsaOffset, buffer.GetLength() - RecordHeader.UsaOffset);
            Int64 Increment = (Int64)(DiskInfo.BytesPerSector / sizeof(UInt16));

            Int64 index = Increment - 1;

            for (UInt16 i = 1; i < RecordHeader.UsaCount; i++)
            {
                /* Check if we are inside the buffer. */
                if (index * sizeof(UInt16) >= (Int64)BufLength)
                {
                    ShowDebug(0, "Warning: USA data indicates that data is missing, the MFT may be corrupt.");
                    return false;
                }

                /* Check if the last 2 bytes of the sector contain the Update Sequence Number.
                 * If not then return FALSE. */
                if (BufferW.GetValue(index) != UpdateSequenceArray.GetValue(0))
                {
                    ShowDebug(0, "Error: USA fixup word is not equal to the Update Sequence Number, the MFT may be corrupt.");
                    return false;
                }

                /* Replace the last 2 bytes in the sector with the value from the Usa array. */
                BufferW.SetValue(index, UpdateSequenceArray.GetValue(i));

                index += Increment;
            }

            buffer = BufferW.ToByteArray(0, BufferW.GetLength());
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
        /// <param name="Offset">Bytes to skip from begin of data.</param>
        /// <param name="WantedLength">Number of bytes to read.</param>
        /// <returns></returns>
        ByteArray ReadNonResidentData(
                    DiskInfoStructure DiskInfo,
                    BinaryReader runData,
                    UInt64 runDataLength,
                    UInt64 Offset,
                    UInt64 WantedLength)
        {
            ByteArray Buffer = new ByteArray((Int64)WantedLength);

            UInt64 ExtentVcn;
            UInt64 ExtentLcn;
            UInt64 ExtentLength;

            ShowDebug(6, String.Format("    Reading {0:G} bytes from offset {0:G}", WantedLength, Offset));

            /* Sanity check. */
            if ((runData == null) || (runDataLength == 0)) 
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
            UInt64 Lcn = 0;
            UInt64 Vcn = 0;

            while (runData.PeekChar() != 0)
            {
                Byte runDataValue = runData.ReadByte();

                /* Decode the RunData and calculate the next Lcn. */
                int runLengthSize = (runDataValue & 0x0F);
                int runOffsetSize = ((runDataValue & 0xF0) >> 4);

                UInt64 runLength = RunData.ReadLength(runData, runLengthSize);
                Int64 runOffset = RunData.ReadOffset(runData, runOffsetSize);

                Lcn = (UInt64) ((Int64)Lcn + runOffset);
                Vcn += runLength;

                /* Ignore virtual extents. */
                if (runOffset == 0)
                    continue;

                /* I don't think the RunLength can ever be zero, but just in case. */
                if (runLength == 0)
                    continue;

                /* Determine how many and which bytes we want to read. If we don't need
                 * any bytes from this extent then loop. */
                ExtentVcn = (Vcn - runLength) * DiskInfo.BytesPerCluster;
                ExtentLcn = Lcn * DiskInfo.BytesPerCluster;

                ExtentLength = runLength * DiskInfo.BytesPerCluster;

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
                    ExtentLength, ExtentLcn / DiskInfo.BytesPerCluster,
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
                    BinaryReader runData,
                    UInt64 runDataLength,
                    UInt64 startingVcn,
                    UInt64 Bytes)
        {
            /* Sanity check. */
            if ((m_msDefragLib.m_data == null) || (InodeData == null))
                throw new Exception("Sanity check failed");

            /* Find the stream in the list of streams. If not found then create a new stream. */
            Stream foundStream = InodeData.Streams.FirstOrDefault(x => (x.Name == StreamName) && (x.Type.Type == StreamType.Type));
            if (foundStream == null)
            {
                ShowDebug(6, "    Creating new stream: '" + StreamName + ":" + StreamType.GetStreamTypeName() + "'");
                Stream newStream = new Stream(StreamName, StreamType);
                newStream.Clusters = 0;
                newStream.Bytes = Bytes;

                InodeData.Streams.Add(newStream);
                foundStream = newStream;
            }
            else
            {
                ShowDebug(6, "    Appending rundata to existing stream: '" + StreamName + ":" + StreamType.GetStreamTypeName());
                if (foundStream.Bytes == 0)
                    foundStream.Bytes = Bytes;
            }

            /* If the stream already has a list of fragments then find the last fragment. */
            Fragment lastFragment = foundStream.Fragments.LastOrDefault();
            if (lastFragment != null)
            {
                throw new NotImplementedException();
                //if (StartingVcn != lastFragment.NextVcn)
                //{
                //    ShowDebug(2, String.Format("Error: m_iNode {0:G} already has a list of fragments. LastVcn={1:G}, StartingVCN={2:G}",
                //      InodeData.m_iNode, lastFragment.NextVcn, StartingVcn));
                //    return false;
                //}
            }

            if (runData == null)
                return true;

            /* Walk through the RunData and add the extents. */
            Int64 Lcn = 0;
            UInt64 Vcn = startingVcn;

            Byte runDataValue = 0;
            while ((runDataValue = runData.ReadByte())!= 0)
            {
                /* Decode the RunData and calculate the next Lcn. */
                int runLengthSize = (runDataValue & 0x0F);
                int runOffsetSize = ((runDataValue & 0xF0) >> 4);

                UInt64 runLength = RunData.ReadLength(runData, runLengthSize);
                Int64 runOffset = RunData.ReadOffset(runData, runOffsetSize);

                Lcn += runOffset;

                if (runOffset != 0)
                {
                    foundStream.Clusters += runLength;
                }

                foundStream.Fragments.Add(Lcn, Vcn, runLength, runOffset == 0);
                Vcn += runLength;
            }
            return true;
        }

        /* Construct the full stream name from the filename, the stream name, and the stream type. */
        String ConstructStreamName(String FileName1, String FileName2, Stream Stream)
        {
            AttributeType StreamType = new AttributeType();

            String FileName = FileName1;
            if (String.IsNullOrEmpty(FileName))
                FileName = FileName2;
            if (String.IsNullOrEmpty(FileName))
                FileName = null;

            String StreamName = null;
            StreamType = new AttributeType();

            if (Stream != null)
            {
                StreamName = Stream.Name;

                if (String.IsNullOrEmpty(StreamName))
                    StreamName = null;

                StreamType = Stream.Type;
            }

            /*  
                If the StreamName is empty and the StreamType is Data then return only the
                FileName. The Data stream is the default stream of regular files.
            */
            if ((String.IsNullOrEmpty(StreamName)) && (StreamType == AttributeTypeEnum.AttributeData))
            {
                if (String.IsNullOrEmpty(FileName))
                    return null;
                return FileName;
            }

            /*  
                If the StreamName is "$I30" and the StreamType is AttributeIndexAllocation then
                return only the FileName. This must be a directory, and the Microsoft defragmentation
                API will automatically select this stream.
            */
            if ((StreamName == "$I30") &&
                (StreamType == AttributeTypeEnum.AttributeIndexAllocation))
            {
                if (String.IsNullOrEmpty(FileName))
                    return null;
                return FileName;
            }

            /*  
                If the StreamName is empty and the StreamType is Data then return only the
                FileName. The Data stream is the default stream of regular files. 
            */
            if ((String.IsNullOrEmpty(StreamName)) &&
                (StreamType.GetStreamTypeName().Length == 0))
            {
                if (String.IsNullOrEmpty(FileName))
                    return null;
                return FileName;
            }

            Int32 Length = 3;

            if (FileName != null) 
                Length += FileName.Length;
            if (StreamName != null)
                Length += StreamName.Length;

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
                DiskInfoStructure DiskInfo,
                InodeDataStructure inodeData,
                BinaryReader reader,
                UInt64 BufLength,
                int Depth)
        {
            Debug.Assert(inodeData.MftDataFragments != null);

            Int64 position = reader.BaseStream.Position;
            ByteArray Buffer2 = new ByteArray((Int64)DiskInfo.BytesPerMftRecord);

            FileRecordHeader FileRecordHeader;

            UInt64 RefInode;
            UInt64 BaseInode;
            UInt64 RealVcn;
            UInt64 RefInodeVcn;

            String p1;

            /* Sanity checks. */
            if ((reader == null) || (BufLength == 0))
                throw new Exception("Sanity check failed");

            if (Depth > 1000)
            {
                throw new Exception("implementation error");
                ShowDebug(0, "Error: infinite attribute loop, the MFT may be corrupt.");

                return;
            }

            ShowDebug(6, String.Format("Processing AttributeList for m_iNode {0:G}, {1:G} bytes", inodeData.m_iNode, BufLength));

            AttributeList attributeList = null;
            /* Walk through all the attributes and gather information. */
            for (Int64 AttributeOffset = 0; AttributeOffset < (Int64)BufLength; AttributeOffset += attributeList.Length)
            {
                reader.BaseStream.Seek(position + AttributeOffset, SeekOrigin.Begin);
                attributeList = AttributeList.Parse(reader);

                /* Exit if no more attributes. AttributeLists are usually not closed by the
                 * 0xFFFFFFFF endmarker. Reaching the end of the buffer is therefore normal and
                 * not an error.*/
                if (AttributeOffset + 3 > (Int64)BufLength) break;
                if (attributeList.Type == AttributeTypeEnum.AttributeEndOfList) break;
                if (attributeList.Length < 3) break;
                if (AttributeOffset + attributeList.Length > (Int64)BufLength) break;

                /* Extract the referenced m_iNode. If it's the same as the calling m_iNode then 
                 * ignore (if we don't ignore then the program will loop forever, because for 
                 * some reason the info in the calling m_iNode is duplicated here...). */
                RefInode = (UInt64)attributeList.m_fileReferenceNumber.m_iNodeNumberLowPart +
                        ((UInt64)attributeList.m_fileReferenceNumber.m_iNodeNumberHighPart << 32);

                if (RefInode == inodeData.m_iNode) continue;

                /* Show debug message. */
                ShowDebug(6, "    List attribute: " + attributeList.Type.GetStreamTypeName());
                ShowDebug(6, String.Format("      m_lowestVcn = {0:G}, RefInode = {1:G}, InodeSequence = {2:G}, m_instance = {3:G}",
                      attributeList.m_lowestVcn, RefInode, attributeList.m_fileReferenceNumber.m_sequenceNumber, attributeList.m_instance));

                /* Extract the streamname. I don't know why AttributeLists can have names, and
                 * the name is not used further down. It is only extracted for debugging 
                 * purposes. */
                if (attributeList.NameLength > 0)
                {
                    reader.BaseStream.Seek(position + AttributeOffset + attributeList.NameOffset, SeekOrigin.Begin);
                    p1 = Helper.ParseString(reader, attributeList.NameLength);
                    ShowDebug(6, "      AttributeList name = '" + p1 + "'");
                }

                /* Find the fragment in the MFT that contains the referenced m_iNode. */
                RealVcn = 0;
                RefInodeVcn = RefInode * DiskInfo.BytesPerMftRecord / (DiskInfo.BytesPerSector * DiskInfo.SectorsPerCluster);

                Fragment foundFragment = null;
                foreach (Fragment fragment in inodeData.MftDataFragments)
                {
                    if (fragment.IsLogical)
                    {
                        if ((RefInodeVcn >= RealVcn) && (RefInodeVcn < RealVcn + fragment.Length))
                        {
                            foundFragment = fragment;
                            break;
                        }

                        RealVcn += fragment.Length;
                    }
                }

                if (foundFragment == null)
                {
                    ShowDebug(6, String.Format("      Error: m_iNode {0:G} is an extension of m_iNode {1:G}, but does not exist (outside the MFT).",
                            RefInode, inodeData.m_iNode));

                    continue;
                }

                /* Fetch the record of the referenced m_iNode from disk. */
                UInt64 tempVcn = (foundFragment.Lcn - RealVcn) * DiskInfo.BytesPerCluster +
                    RefInode * DiskInfo.BytesPerMftRecord;

                Byte[] tempBuffer = new Byte[DiskInfo.BytesPerMftRecord];

                m_msDefragLib.m_data.Disk.ReadFromCluster(tempVcn, Buffer2.m_bytes, 0,
                    (Int32)DiskInfo.BytesPerMftRecord);

                /* Fixup the raw data. */
                if (FixupRawMftdata(DiskInfo, Buffer2, DiskInfo.BytesPerMftRecord) == false)
                {
                    ShowDebug(2, String.Format("The error occurred while processing m_iNode {0:G}", RefInode));
                    continue;
                }

                /* If the Inode is not in use then skip. */
                FileRecordHeader = FileRecordHeader.Parse(Helper.BinaryReader(Buffer2));

                if ((FileRecordHeader.Flags & 1) != 1)
                {
                    ShowDebug(6, String.Format("      Referenced m_iNode {0:G} is not in use.", RefInode));
                    continue;
                }

                /* If the BaseInode inside the m_iNode is not the same as the calling m_iNode then skip. */
                BaseInode = (UInt64)FileRecordHeader.BaseFileRecord.m_iNodeNumberLowPart +
                        ((UInt64)FileRecordHeader.BaseFileRecord.m_iNodeNumberHighPart << 32);

                if (inodeData.m_iNode != BaseInode)
                {
                    ShowDebug(6, String.Format("      Warning: m_iNode {0:G} is an extension of m_iNode {1:G}, but thinks it's an extension of m_iNode {2:G}.",
                            RefInode, inodeData.m_iNode, BaseInode));

                    continue;
                }

                /* Process the list of attributes in the m_iNode, by recursively calling the ProcessAttributes() subroutine. */
                ShowDebug(6, String.Format("      Processing m_iNode {0:G} m_instance {1:G}", RefInode, attributeList.m_instance));

                ProcessAttributes(
                    DiskInfo,
                    inodeData,
                    Helper.BinaryReader(Buffer2, FileRecordHeader.AttributeOffset),
                    DiskInfo.BytesPerMftRecord - FileRecordHeader.AttributeOffset,
                    attributeList.m_instance, Depth + 1);

                ShowDebug(6, String.Format("      Finished processing m_iNode {0:G} m_instance {1:G}", RefInode, attributeList.m_instance));
            }
        }

        /// <summary>
        /// Process a list of attributes and store the gathered information in the Item
        /// struct. Return FALSE if an error occurred.
        /// </summary>
        /// <param name="DiskInfo"></param>
        /// <param name="InodeData"></param>
        /// <param name="Buffer"></param>
        /// <param name="BufLength"></param>
        /// <param name="Instance"></param>
        /// <param name="Depth"></param>
        /// <returns></returns>
        Boolean ProcessAttributes(
            DiskInfoStructure DiskInfo,
            InodeDataStructure inodeData,
            BinaryReader reader,
            UInt64 BufLength,
            UInt32 Instance,
            int Depth)
        {
            UInt64 Buffer2Length;
            UInt32 AttributeOffset;

            Attribute attribute;
            ResidentAttribute residentAttribute;
            NonResidentAttribute nonResidentAttribute;
            StandardInformation standardInformation;
            FileNameAttribute fileNameAttribute;

            String p1;
            Int64 position = reader.BaseStream.Position;

            /* Walk through all the attributes and gather information. AttributeLists are
             * skipped and interpreted later.*/
            for (AttributeOffset = 0; AttributeOffset < BufLength; AttributeOffset += attribute.Length)
            {
                reader.BaseStream.Seek(position + AttributeOffset, SeekOrigin.Begin);
                attribute = Attribute.Parse(reader);

                if (attribute.Type == AttributeTypeEnum.AttributeEndOfList)
                {
                    break;
                }

                /* Exit the loop if end-marker. */
                if ((AttributeOffset + 4 <= BufLength) &&
                    (attribute.Type == AttributeTypeEnum.AttributeInvalid))
                {
                    break;
                }

                /* Sanity check. */
                if ((AttributeOffset + 4 > BufLength) ||
                    (attribute.Length < 3) ||
                    (AttributeOffset + attribute.Length > BufLength))
                {
                    throw new Exception("implementation error");
                    ShowDebug(0, String.Format("Error: attribute in m_iNode {0:G} is bigger than the data, the MFT may be corrupt.", inodeData.m_iNode));
                    ShowDebug(2, String.Format("  BufLength={0:G}, AttributeOffset={1:G}, AttributeLength={2:G}({3:X})",
                            BufLength, AttributeOffset, attribute.Length, attribute.Length));

                    //m_msDefragLib.ShowHex(Data, Buffer.m_bytes, BufLength);

                    return false;
                }

                /* Skip AttributeList's for now. */
                if (attribute.Type == AttributeTypeEnum.AttributeAttributeList)
                {
                    continue;
                }

                /* If the Instance does not equal the m_attributeNumber then ignore the attribute.
                 * This is used when an AttributeList is being processed and we only want a specific
                 * instance. */
                if ((Instance != UInt16.MaxValue) && (Instance != attribute.Number))
                {
                    continue;
                }

                /* Show debug message. */
                ShowDebug(6, String.Format("  Attribute {0:G}: {1:G}", attribute.Number, attribute.Type.GetStreamTypeName()));

                reader.BaseStream.Seek(position + AttributeOffset, SeekOrigin.Begin);
                if (attribute.IsNonResident == false)
                {
                    residentAttribute = ResidentAttribute.Parse(reader);
                    Int64 tempOffset = (Int64)(AttributeOffset + residentAttribute.ValueOffset);
                    reader.BaseStream.Seek(position + tempOffset, SeekOrigin.Begin);

                    /* The AttributeFileName (0x30) contains the filename and the link to the parent directory. */
                    if (attribute.Type == AttributeTypeEnum.AttributeFileName)
                    {
                        fileNameAttribute = FileNameAttribute.Parse(reader);

                        inodeData.m_parentInode = fileNameAttribute.m_parentDirectory.m_iNodeNumberLowPart +
                            (((UInt32)fileNameAttribute.m_parentDirectory.m_iNodeNumberHighPart) << 32);

                        inodeData.AddName(fileNameAttribute);
                    }

                    /*  
                        The AttributeStandardInformation (0x10) contains the m_creationTime, m_lastAccessTime,
                        the m_mftChangeTime, and the file attributes.
                    */
                    if (attribute.Type == AttributeTypeEnum.AttributeStandardInformation)
                    {
                        standardInformation = StandardInformation.Parse(reader);

                        inodeData.m_creationTime = standardInformation.CreationTime;
                        inodeData.m_mftChangeTime = standardInformation.MftChangeTime;
                        inodeData.m_lastAccessTime = standardInformation.LastAccessTime;
                    }

                    /* The value of the AttributeData (0x80) is the actual data of the file. */
                    if (attribute.Type == AttributeTypeEnum.AttributeData)
                    {
                        inodeData.m_totalBytes = residentAttribute.ValueLength;
                    }
                }
                else
                {
                    nonResidentAttribute = NonResidentAttribute.Parse(reader);

                    /* Save the length (number of bytes) of the data. */
                    if ((attribute.Type == AttributeTypeEnum.AttributeData) &&
                        (inodeData.m_totalBytes == 0))
                    {
                        inodeData.m_totalBytes = nonResidentAttribute.m_dataSize;
                    }

                    /* Extract the streamname. */
                    reader.BaseStream.Seek(position + AttributeOffset + attribute.NameOffset, SeekOrigin.Begin);
                    p1 = Helper.ParseString(reader, attribute.NameLength);

                    /* Create a new stream with a list of fragments for this data. */
                    reader.BaseStream.Seek(position + AttributeOffset + nonResidentAttribute.m_runArrayOffset, SeekOrigin.Begin);
                    TranslateRundataToFragmentlist(inodeData, p1, attribute.Type,
                        reader, attribute.Length - nonResidentAttribute.m_runArrayOffset,
                        nonResidentAttribute.m_startingVcn, nonResidentAttribute.m_dataSize);

                    /* Special case: If this is the $MFT then save data. */
                    if (inodeData.m_iNode == 0)
                    {
                        if ((attribute.Type == AttributeTypeEnum.AttributeData) &&
                            (inodeData.MftDataFragments == null))
                        {
                            inodeData.MftDataFragments = inodeData.Streams.First().Fragments;
                            inodeData.m_mftDataLength = nonResidentAttribute.m_dataSize;
                        }

                        if ((attribute.Type== AttributeTypeEnum.AttributeBitmap) &&
                            (inodeData.MftBitmapFragments == null))
                        {
                            inodeData.MftBitmapFragments = inodeData.Streams.First().Fragments;
                            inodeData.m_mftBitmapLength = nonResidentAttribute.m_dataSize;
                        }
                    }
                }
            }

            /* Walk through all the attributes and interpret the AttributeLists. We have to
             * do this after the DATA and BITMAP attributes have been interpreted, because
             * some MFT's have an AttributeList that is stored in fragments that are
             * defined in the DATA attribute, and/or contain a continuation of the DATA or
             * BITMAP attributes.*/
            for (AttributeOffset = 0; AttributeOffset < BufLength; AttributeOffset += attribute.Length)
            {
                reader.BaseStream.Seek(position + AttributeOffset, SeekOrigin.Begin);
                //HACK: temporary hack to demonstrate the usage of the binary reader
                attribute = Attribute.Parse(reader);

                if (attribute.Type == AttributeTypeEnum.AttributeEndOfList)
                {
                    break;
                }

                if (attribute.Type == AttributeTypeEnum.AttributeInvalid)
                {
                    break;
                }

                if (attribute.Type != AttributeTypeEnum.AttributeAttributeList)
                {
                    continue;
                }

                ShowDebug(6, String.Format("  Attribute {0:G}: {1:G}", attribute.Number, attribute.Type.GetStreamTypeName()));

                reader.BaseStream.Seek(position + AttributeOffset, SeekOrigin.Begin);
                if (attribute.IsNonResident == false)
                {
                    residentAttribute = ResidentAttribute.Parse(reader);

                    reader.BaseStream.Seek(position + AttributeOffset + residentAttribute.ValueOffset, SeekOrigin.Begin);
                    ProcessAttributeList(DiskInfo, inodeData, reader, residentAttribute.ValueLength, Depth);
                }
                else
                {
                    nonResidentAttribute = NonResidentAttribute.Parse(reader);

                    Buffer2Length = nonResidentAttribute.m_dataSize;
                    // Buffer2Length = 512;

                    reader.BaseStream.Seek(position + AttributeOffset + nonResidentAttribute.m_runArrayOffset, SeekOrigin.Begin);
                    ByteArray Buffer2 = ReadNonResidentData(DiskInfo, reader,
                            attribute.Length - nonResidentAttribute.m_runArrayOffset, 0, Buffer2Length);

                    ProcessAttributeList(DiskInfo, inodeData, Helper.BinaryReader(Buffer2), Buffer2Length, Depth);
                }
            }

            return true;
        }

        Boolean InterpretMftRecord(
            DiskInfoStructure DiskInfo,
            Array InodeArray,
            UInt64 InodeNumber,
            UInt64 MaxInode,
            ref FragmentList mftDataFragments,
            ref UInt64 mftDataBytes,
            ref FragmentList mftBitmapFragments,
            ref UInt64 mftBitmapBytes,
            BinaryReader reader,
            UInt64 BufLength)
        {
            /* If the record is not in use then quietly exit. */
            Int64 position = reader.BaseStream.Position;
            FileRecordHeader FileRecordHeader = FileRecordHeader.Parse(reader);

            if ((FileRecordHeader.Flags & 1) != 1)
            {
                ShowDebug(6, String.Format("Inode {0:G} is not in use.", InodeNumber));

                return false;
            }

            /* If the record has a BaseFileRecord then ignore it. It is used by an
             * AttributeAttributeList as an extension of another m_iNode, it's not an
             * Inode by itself. */
            UInt64 BaseInode = 
                (UInt64)FileRecordHeader.BaseFileRecord.m_iNodeNumberLowPart +
                ((UInt64)FileRecordHeader.BaseFileRecord.m_iNodeNumberHighPart << 32);

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

            /* I think the MFTRecordNumber should always be the InodeNumber, but it's an XP
             * extension and I'm not sure about Win2K.
             * 
             * Note: why is the MFTRecordNumber only 32 bit? Inode numbers are 48 bit.*/
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

            InodeDataStructure inodeData = new InodeDataStructure(InodeNumber);
            inodeData.IsDirectory = ((FileRecordHeader.Flags & 2) == 2);
            inodeData.MftDataFragments = mftDataFragments;
            inodeData.m_mftDataLength = mftDataBytes;

            /* Make sure that directories are always created. */
            if (inodeData.IsDirectory)
            {
                AttributeType attributeType = AttributeTypeEnum.AttributeIndexAllocation;
                TranslateRundataToFragmentlist(inodeData, "$I30", attributeType, null, 0, 0, 0);
            }

            /* Interpret the attributes. */
            reader.BaseStream.Seek(position + FileRecordHeader.AttributeOffset, SeekOrigin.Begin);
            ProcessAttributes(DiskInfo, inodeData,
                reader, BufLength - FileRecordHeader.AttributeOffset, UInt16.MaxValue, 0);

            /* Save the MftDataFragments, MftDataBytes, MftBitmapFragments, and MftBitmapBytes. */
            if (InodeNumber == 0)
            {
                mftDataFragments = inodeData.MftDataFragments;
                mftDataBytes = inodeData.m_mftDataLength;
                mftBitmapFragments = inodeData.MftBitmapFragments;
                mftBitmapBytes = inodeData.m_mftBitmapLength;
            }

            /* Create an item in the Data->ItemTree for every stream. */
            foreach (Stream stream in inodeData.Streams)
            {
                /* Create and fill a new item record in memory. */
                ItemStruct Item = new ItemStruct(stream);
                Item.LongFilename = ConstructStreamName(inodeData.m_longFilename, inodeData.m_shortFilename, stream);
                Item.LongPath = null;

                Item.ShortFilename = ConstructStreamName(inodeData.m_shortFilename, inodeData.m_longFilename, stream);
                Item.ShortPath = null;

                Item.Bytes = inodeData.m_totalBytes;

                Item.Bytes = stream.Bytes;

                Item.Clusters = 0;

                Item.Clusters = stream.Clusters;

                Item.CreationTime = inodeData.m_creationTime;
                Item.MftChangeTime = inodeData.m_mftChangeTime;
                Item.LastAccessTime = inodeData.m_lastAccessTime;

                Item.ParentInode = inodeData.m_parentInode;
                Item.Directory = inodeData.IsDirectory;
                Item.Unmovable = false;
                Item.Exclude = false;
                Item.SpaceHog = false;

                /* Increment counters. */
                if (Item.Directory == true)
                {
                    m_msDefragLib.m_data.CountDirectories++;
                }

                m_msDefragLib.m_data.CountAllFiles++;

                if (stream.Type == AttributeTypeEnum.AttributeData)
                {
                    m_msDefragLib.m_data.CountAllBytes += inodeData.m_totalBytes;
                }

                m_msDefragLib.m_data.CountAllClusters += stream.Clusters;

                if (Item.FragmentCount > 1)
                {
                    m_msDefragLib.m_data.CountFragmentedItems++;
                    m_msDefragLib.m_data.CountFragmentedBytes += inodeData.m_totalBytes;

                    if (stream != null) m_msDefragLib.m_data.CountFragmentedClusters += stream.Clusters;
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
            }

            return true;
        }

        //////////////////////////////////////////////////////////////////////////
        //
        // Load the MFT into a list of ItemStruct records in memory.
        //
        //////////////////////////////////////////////////////////////////////////
        public Boolean AnalyzeNtfsVolume()
        {
            // Read the boot block from the disk.
            FS.IBootSector bootSector = m_msDefragLib.m_data.Disk.BootSector;

            // Test if the boot block is an NTFS boot block.
            if (bootSector.Filesystem != FS.Filesystem.NTFS)
            {
                ShowDebug(2, "This is not an NTFS disk (different cookie).");

                return false;
            }

            DiskInfoStructure diskInfo = new DiskInfoStructure(bootSector);

            m_msDefragLib.m_data.BytesPerCluster = diskInfo.BytesPerCluster;

            if (diskInfo.SectorsPerCluster > 0)
            {
                m_msDefragLib.m_data.TotalClusters = diskInfo.TotalSectors / diskInfo.SectorsPerCluster;
            }

            ShowDebug(0, "This is an NTFS disk.");

            ShowDebug(2, String.Format("  Disk cookie: {0:X}", bootSector.OemId));
            ShowDebug(2, String.Format("  BytesPerSector: {0:G}", diskInfo.BytesPerSector));
            ShowDebug(2, String.Format("  TotalSectors: {0:G}", diskInfo.TotalSectors));
            ShowDebug(2, String.Format("  SectorsPerCluster: {0:G}", diskInfo.SectorsPerCluster));

            ShowDebug(2, String.Format("  SectorsPerTrack: {0:G}", bootSector.SectorsPerTrack));
            ShowDebug(2, String.Format("  NumberOfHeads: {0:G}", bootSector.NumberOfHeads));
            ShowDebug(2, String.Format("  MftStartLcn: {0:G}", diskInfo.MftStartLcn));
            ShowDebug(2, String.Format("  Mft2StartLcn: {0:G}", diskInfo.Mft2StartLcn));
            ShowDebug(2, String.Format("  BytesPerMftRecord: {0:G}", diskInfo.BytesPerMftRecord));
            ShowDebug(2, String.Format("  ClustersPerIndexRecord: {0:G}", diskInfo.ClustersPerIndexRecord));

            ShowDebug(2, String.Format("  MediaType: {0:X}", bootSector.MediaType));

            ShowDebug(2, String.Format("  VolumeSerialNumber: {0:X}", bootSector.Serial));

            /* 
                Calculate the size of first 16 Inodes in the MFT. The Microsoft defragmentation
                API cannot move these inodes.
            */
            m_msDefragLib.m_data.Disk.MftLockedClusters = diskInfo.BytesPerCluster / diskInfo.BytesPerMftRecord;

            /*
                Read the $MFT record from disk into memory, which is always the first record in
                the MFT.
            */
            UInt64 tempLcn = diskInfo.MftStartLcn * diskInfo.BytesPerCluster;

            ByteArray Buffer = new ByteArray((Int64)MFTBUFFERSIZE);

            m_msDefragLib.m_data.Disk.ReadFromCluster(tempLcn, Buffer.m_bytes, 0,
                (Int32)diskInfo.BytesPerMftRecord);

            /* Fixup the raw data from disk. This will also test if it's a valid $MFT record. */
            if (FixupRawMftdata(diskInfo, Buffer, diskInfo.BytesPerMftRecord) == false)
            {
                return false;
            }

            /*
                Extract data from the MFT record and put into an Item struct in memory. If
                there was an error then exit. 
            */
            FragmentList MftDataFragments = null;
            FragmentList MftBitmapFragments = null;

            UInt64 MftDataBytes = 0;
            UInt64 MftBitmapBytes = 0;

            Boolean Result = InterpretMftRecord(diskInfo, null, 0, 0,
                ref MftDataFragments, ref MftDataBytes, ref MftBitmapFragments, ref MftBitmapBytes,
                Helper.BinaryReader(Buffer), diskInfo.BytesPerMftRecord);

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

            UInt64 MaxMftBitmapBytes = 0;

            foreach (Fragment fragment in MftBitmapFragments)
            {
                if (fragment.IsLogical)
                    MaxMftBitmapBytes += fragment.Length;
            }

            // transform clusters into bytes
            MaxMftBitmapBytes *= diskInfo.BytesPerCluster;

            MaxMftBitmapBytes = Math.Max(MaxMftBitmapBytes, MftBitmapBytes);

            ByteArray MftBitmap = new ByteArray((Int64)MaxMftBitmapBytes);

            UInt64 RealVcn = 0;

            ShowDebug(6, "Reading $MFT::$BITMAP into memory");

            foreach (Fragment fragment in MftBitmapFragments)
            {
                if (fragment.IsLogical)
                {
                    tempLcn = fragment.Lcn * diskInfo.BytesPerCluster;

                    UInt64 numClusters = fragment.Length;
                    Int32 numBytes = (Int32)(numClusters * diskInfo.BytesPerCluster);
                    Int32 startIndex = (Int32)(RealVcn * diskInfo.BytesPerCluster);

                    m_msDefragLib.m_data.Disk.ReadFromCluster(tempLcn, MftBitmap.m_bytes,
                        startIndex, numBytes);

                    RealVcn += fragment.Length;
                }
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
            UInt64 MaxInode = Math.Max(MftBitmapBytes * 8, MftDataBytes / diskInfo.BytesPerMftRecord);

            ItemStruct[] InodeArray = new ItemStruct[MaxInode];
            InodeArray.SetValue(m_msDefragLib.m_data.ItemTree, 0);
            ItemStruct Item = null;

            UInt64 BlockStart = 0;
            UInt64 InodeNumber = 0;
            for (InodeNumber = 1; InodeNumber < MaxInode; InodeNumber++)
            {
                InodeArray.SetValue(null, (Int64)InodeNumber);
            }

            /*
                Read and process all the records in the MFT. The records are read into a
                buffer and then given one by one to the InterpretMftRecord() subroutine.
            */
            UInt64 BlockEnd = 0;
            RealVcn = 0;

            m_msDefragLib.m_data.PhaseDone = 0;
            m_msDefragLib.m_data.PhaseTodo = 0;

            DateTime Time = DateTime.Now;
            Int64 StartTime = Time.ToFileTime();

            Byte[] BitmapMasks = { 1, 2, 4, 8, 16, 32, 64, 128 };
            for (InodeNumber = 1; InodeNumber < MaxInode; InodeNumber++)
            {
                Byte val = MftBitmap.GetValue((Int64)(InodeNumber >> 3));
                Boolean mask = ((val & BitmapMasks[InodeNumber % 8]) == 0);

                if (mask == false)
                    continue;

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

                UInt64 u1 = 0;

                /* Read a block of inode's into memory. */
                if (InodeNumber >= BlockEnd)
                {
                    /* Slow the program down to the percentage that was specified on the command line. */
                    m_msDefragLib.SlowDown();

                    BlockStart = InodeNumber;
                    BlockEnd = BlockStart + MFTBUFFERSIZE / diskInfo.BytesPerMftRecord;

                    if (BlockEnd > MftBitmapBytes * 8) BlockEnd = MftBitmapBytes * 8;

                    Fragment foundFragment = null;
                    foreach (Fragment fragment in MftDataFragments)
                    {
                        /* Calculate m_iNode at the end of the fragment. */
                        u1 = diskInfo.ClusterToInode(RealVcn + fragment.Length);
                        if (u1 > InodeNumber)
                        {
                            foundFragment = fragment;
                            break;
                        }

                        do
                        {
                            ShowDebug(6, "Skipping to next extent");

                            if (fragment.IsLogical)
                                RealVcn += fragment.Length;

                            //Vcn = fragment.NextVcn;
                            //fragment = fragment.Next;

                            throw new NotImplementedException();
                            if (fragment == null)
                                break;
                        } while (fragment.IsVirtual);

                        //if (fragment != null)
                        //{
                        //    ShowDebug(6, String.Format("  Extent Lcn={0:G}, RealVcn={1:G}, Size={2:G}",
                        //          fragment.Lcn, RealVcn, fragment.NextVcn - Vcn));
                        //}
                    }

                    if (foundFragment == null)
                        break;
                    if (BlockEnd >= u1)
                        BlockEnd = u1;

                    tempLcn = (foundFragment.Lcn - RealVcn) * diskInfo.BytesPerCluster +
                        BlockStart * diskInfo.BytesPerMftRecord;

                    ShowDebug(6, String.Format("Reading block of {0:G} Inodes from MFT into memory, {1:G} bytes from LCN={2:G}",
                          BlockEnd - BlockStart, diskInfo.InodeToBytes(BlockEnd - BlockStart),
                          tempLcn / diskInfo.BytesPerCluster));

                    m_msDefragLib.m_data.Disk.ReadFromCluster(tempLcn,
                        Buffer.m_bytes, 0, (Int32)diskInfo.InodeToBytes(BlockEnd - BlockStart));
                }

                /* Fixup the raw data of this m_iNode. */
                UInt64 lengthInBytes = diskInfo.InodeToBytes(InodeNumber - BlockStart);
                if (FixupRawMftdata(diskInfo,
                        Buffer.ToByteArray((Int64)lengthInBytes, Buffer.GetLength() - (Int64)(lengthInBytes)),
                        diskInfo.BytesPerMftRecord) == false)
                {
                    ShowDebug(2, String.Format("The error occurred while processing m_iNode {0:G} (max {0:G})",
                            InodeNumber, MaxInode));
                    continue;
                }

                /* Interpret the m_iNode's attributes. */
                Result = InterpretMftRecord(diskInfo, InodeArray, InodeNumber, MaxInode,
                        ref MftDataFragments, ref MftDataBytes, ref MftBitmapFragments, ref MftBitmapBytes,
                        Helper.BinaryReader(Buffer, (Int64)diskInfo.InodeToBytes(InodeNumber - BlockStart)),
                        diskInfo.BytesPerMftRecord);

                if (m_msDefragLib.m_data.PhaseDone % 50 == 0)
                    ShowDebug(1, "Done: " + m_msDefragLib.m_data.PhaseDone + "/" + m_msDefragLib.m_data.PhaseTodo);
            }

            Time = DateTime.Now;

            Int64 EndTime = Time.ToFileTime();

            if (EndTime > StartTime)
            {
                ShowDebug(2, String.Format("  Analysis speed: {0:G} items per second",
                      (Int64)MaxInode * 1000 / (EndTime - StartTime)));
            }

            using (m_msDefragLib.m_data.Disk)
            {
                if (m_msDefragLib.m_data.Running != RunningState.RUNNING)
                {
                    m_msDefragLib.DeleteItemTree(m_msDefragLib.m_data.ItemTree);
                    m_msDefragLib.m_data.ItemTree = null;
                    return false;
                }

                /* Setup the ParentDirectory in all the items with the info in the InodeArray. */
                for (Item = ItemTree.TreeSmallest(m_msDefragLib.m_data.ItemTree); Item != null; Item = ItemTree.TreeNext(Item))
                {
                    Item.ParentDirectory = (ItemStruct)InodeArray.GetValue((Int64)Item.ParentInode);
                    if (Item.ParentInode == 5)
                        Item.ParentDirectory = null;
                }
            }
            return true;
        }
    }
}
