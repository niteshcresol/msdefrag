using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSDefragLib.FileSystem.Ntfs
{
    public class ByteArray
    {
        public Byte[] m_bytes;

        public ByteArray(Int64 size)
        {
            Initialize(size);
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

        public void Initialize(Int64 length)
        {
            if (length != (int)length)
                throw new Exception("This implementation does not support byte arrays with a length bigger than 32 bits");
            m_bytes = new Byte[length];
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
    }
}
