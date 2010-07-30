using System;
using System.Linq;
using System.Text;

namespace MSDefragLib.Defragmenter
{
    internal class DiskDefragmenter : BaseDefragmenter
    {
        public DiskDefragmenter()
        {
            lib = new MSDefragLib(this);
        }

        private MSDefragLib lib;

        #region IDefragmenter Members

        public override void BeginDefragmentation(string parameter)
        {
            lib.RunJkDefrag(@"C:\*", 2, 10, null, null);
            //lib.RunJkDefrag(@"T:\*", 2, 10, null, null);
        }

        public override void FinishDefragmentation(Int32 timeoutMs)
        {
            if ((lib.Data != null) && (lib.Data.Running == RunningState.Running))
            {
                lib.StopJkDefrag(timeoutMs);
            }
        }

        public override void ReparseClusters()
        {
            lib.ParseDiskBitmap();
        }

        public override void StopReparsingClusters()
        {
            lib.StopReparsingClusters();
        }

        public override DiskMap diskMap
        {
            set { }
            get { return lib.diskMap; }
        }

        #endregion
    }
}
