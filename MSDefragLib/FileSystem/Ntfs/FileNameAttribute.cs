using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSDefragLib.FileSystem.Ntfs
{
    class FileNameAttribute
    {
        public InodeReference m_parentDirectory;
        public UInt64 m_creationTime;
        public UInt64 m_changeTime;
        public UInt64 m_lastWriteTime;
        public UInt64 m_lastAccessTime;
        public UInt64 m_allocatedSize;
        public UInt64 m_dataSize;
        public UInt32 m_fileAttributes;
        public UInt32 m_alignmentOrReserved;
        public Byte m_nameLength;
        public Byte m_nameType;                   /* NTFS=0x01, DOS=0x02 */
        public String m_name/*[1]*/;

        public FileNameAttribute(ByteArray buffer, ref Int64 offset)
        {
            Parse(buffer, ref offset);
        }

        public void Parse(ByteArray buffer, ref Int64 offset)
        {
            //HACK: remove later
            m_parentDirectory = InodeReference.Parse(Helper.BinaryReader(buffer, offset));
            offset += m_parentDirectory.Size;
            m_creationTime = buffer.ToUInt64(ref offset);
            m_changeTime = buffer.ToUInt64(ref offset);
            m_lastWriteTime = buffer.ToUInt64(ref offset);
            m_lastAccessTime = buffer.ToUInt64(ref offset);
            m_allocatedSize = buffer.ToUInt64(ref offset);
            m_dataSize = buffer.ToUInt64(ref offset);
            m_fileAttributes = buffer.ToUInt32(ref offset);
            m_alignmentOrReserved = buffer.ToUInt32(ref offset);
            m_nameLength = buffer.ToByte(ref offset);
            m_nameType = buffer.ToByte(ref offset);
            m_name = "";

            for (int ii = 0; ii < m_nameLength; ii++)
            {
                m_name += (Char)buffer.ToUInt16(ref offset); // TODO: Check this
            }
        }
    }
}
