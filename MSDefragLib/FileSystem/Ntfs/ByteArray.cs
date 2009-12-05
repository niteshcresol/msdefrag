using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSDefragLib.FileSystem.Ntfs
{
    public class ByteArray
    {
        public Byte[] m_bytes;

        public ByteArray()
        {
        }

        public ByteArray(Int64 size)
        {
            m_bytes = new Byte[size];
        }

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

        public ByteArray ToByteArray(Int64 index, Int64 length)
        {
            ByteArray ba = new ByteArray(length);
            Array.Copy(m_bytes, index, ba.m_bytes, 0, length);
            return ba;
        }

        //TODO: check if this matters: offset is truncated to 32 bits
        public UInt16Array ToUInt16Array(Int64 index, Int64 length)
        {
            UInt16Array ba = new UInt16Array(length / 2);
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
    }
}
