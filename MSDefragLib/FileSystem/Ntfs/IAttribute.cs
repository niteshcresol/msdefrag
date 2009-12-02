using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSDefragLib.FileSystem.Ntfs
{
    public interface IAttribute
    {
        AttributeType Type { get; }

        UInt32 Length { get; }

        Byte NameLength { get; }

        UInt16 NameOffset {get;}
    }
}
