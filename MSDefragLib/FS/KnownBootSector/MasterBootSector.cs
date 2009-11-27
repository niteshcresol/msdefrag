using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSDefragLib.FS.KnownBootSector
{
    class MasterBootSector : BaseBootSector
    {
        public MasterBootSector(byte[] buffer)
            : base(buffer)
        {
            throw new NotImplementedException();
        }

        #region IBootSector Members

        public override Filesystem Filesystem
        {
            get { return Filesystem.MBR; }
        }

        #endregion

        public override ulong Serial
        {
            get { throw new NotImplementedException(); }
        }

        public override byte MediaType
        {
            get { throw new NotImplementedException(); }
        }
    }
}
