using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSDefragLib
{
    public class LogMessage
    {
        public LogMessage(Int16 level, String msg)
        {
            logLevel = level;
            message = msg;
        }


        private Int16 logLevel;
        public Int16 LogLevel { set { logLevel = value; } get { return logLevel; } }

        private String message;
        public String Message { set { message = value; } get { return message; } }

        public static String[] messages =
        {
            "This is not a valid MFT record, it does not begin with FILE (maybe trying to read past the end?).",
            "Warning: USA data indicates that data is missing, the MFT may be corrupt.",
            "Error: USA fixup word is not equal to the Update Sequence Number, the MFT may be corrupt.",
            "    Reading {0:G} bytes from offset {0:G}",
            "Sanity check failed!",
            "    Cannot read {0:G} bytes, maximum is {1:G}.",
            "    Reading {0:G} bytes from Lcn={1:G} into offset={2:G}",
            "RunData Parse issue {1} for Stream: {0}",
            "Error: infinite attribute loop",
            "      Referenced m_iNode {0:G} is not in use.",
            "      Warning: m_iNode {0:G} is an extension of m_iNode {1:G}, but thinks it's an extension of m_iNode {2:G}.", // 10

            "Error: attribute in m_iNode {0:G} is bigger than the data, the MFT may be corrupt.",
            "Inode {0:G} is not in use.",
            "Warning: Inode {0:G} contains a different MFTRecordNumber {1:G}",
            "Error: attributes in m_iNode {0:G} are outside the FILE record, the MFT may be corrupt.",
            "Error: in m_iNode {0:G} the record is bigger than the size of the buffer, the MFT may be corrupt.",
            "ProcessAttributes failed for {0} (cnt={1})",
            "This is not an NTFS disk (different cookie).",
            "  Disk cookie: {0:X}",
            "  BytesPerSector: {0:G}",
            "  TotalSectors: {0:G}", // 20

            "  SectorsPerCluster: {0:G}",
            "  SectorsPerTrack: {0:G}",
            "  NumberOfHeads: {0:G}",
            "  MftStartLcn: {0:G}",
            "  Mft2StartLcn: {0:G}",
            "  BytesPerMftRecord: {0:G}",
            "  ClustersPerIndexRecord: {0:G}",
            "  MediaType: {0:X}",
            "  VolumeSerialNumber: {0:X}",
            "MftDataBytes = {0:G}, MftBitmapBytes = {0:G}", // 30

            "  Analysis speed: {0:G} items per second"
        };

    }
}
