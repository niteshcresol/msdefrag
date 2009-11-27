using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSDefragLib.FileSystem.Ntfs
{
    /// <summary>
    /// Enumerator containing all atribute types
    /// </summary>
    enum AttributeTypeEnum
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
        AttributeAll = 0xFF,
        AttributeEndOfList = -1
    };

    class AttributeType
    {
        public AttributeTypeEnum m_attributeType;

        public AttributeType()
        {
        }

        public AttributeType(ByteArray buffer, ref Int64 offset)
        {
            Parse(buffer, ref offset);
        }

        public void Parse(ByteArray buffer, ref Int64 offset)
        {
            AttributeTypeEnum retValue = AttributeTypeEnum.AttributeInvalid;

            // http://msdn.microsoft.com/en-us/library/bb470038%28VS.85%29.aspx
            // It is a DWORD containing enumerated values
            UInt32 val = buffer.ToUInt32(ref offset);

            // the attribute type code may contain a special value -1 (or 0xFFFFFFFF) which 
            // may be present as a filler to mark the end of an attribute list. In that case,
            // the rest of the attribute should be ignored, and the attribute list should not
            // be scanned further.
            switch (val)
            {
                case 0xFFFFFFFF:
                    retValue = AttributeTypeEnum.AttributeEndOfList;
                    break;
                case 0x00: 
                    retValue = AttributeTypeEnum.AttributeInvalid;
                    break;
                case 0x10: 
                    retValue = AttributeTypeEnum.AttributeStandardInformation;
                    break;
                case 0x20: 
                    retValue = AttributeTypeEnum.AttributeAttributeList;
                    break;
                case 0x30: 
                    retValue = AttributeTypeEnum.AttributeFileName;
                    break;
                case 0x40: 
                    retValue = AttributeTypeEnum.AttributeObjectId;
                    break;
                case 0x50: 
                    retValue = AttributeTypeEnum.AttributeSecurityDescriptor;
                    break;
                case 0x60: 
                    retValue = AttributeTypeEnum.AttributeVolumeName;
                    break;
                case 0x70: 
                    retValue = AttributeTypeEnum.AttributeVolumeInformation;
                    break;
                case 0x80: 
                    retValue = AttributeTypeEnum.AttributeData;
                    break;
                case 0x90: 
                    retValue = AttributeTypeEnum.AttributeIndexRoot;
                    break;
                case 0xA0: 
                    retValue = AttributeTypeEnum.AttributeIndexAllocation;
                    break;
                case 0xB0: 
                    retValue = AttributeTypeEnum.AttributeBitmap;
                    break;
                case 0xC0: 
                    retValue = AttributeTypeEnum.AttributeReparsePoint;
                    break;
                case 0xD0: 
                    retValue = AttributeTypeEnum.AttributeEAInformation;
                    break;
                case 0xE0: 
                    retValue = AttributeTypeEnum.AttributeEA;
                    break;
                case 0xF0: 
                    retValue = AttributeTypeEnum.AttributePropertySet;
                    break;
                case 0x100: 
                    retValue = AttributeTypeEnum.AttributeLoggedUtilityStream;
                    break;
                case 0xFF: 
                    retValue = AttributeTypeEnum.AttributeAll;
                    break;
                default:
                    throw new NotSupportedException();
            }

            m_attributeType = retValue;
        }
    }
}
