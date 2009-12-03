using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSDefragLib.FileSystem.Ntfs
{
    public class UInt16Array
    {
        private UInt16[] m_words;

        public ByteArray ToByteArray(Int64 index, Int64 length)
        {
            ByteArray ba = new ByteArray(length * 2 + 1);

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

}
