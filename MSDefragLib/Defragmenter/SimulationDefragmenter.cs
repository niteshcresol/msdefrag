using System;
using System.Linq;
using System.Text;

namespace MSDefragLib.Defragmenter
{
    internal class SimulationDefragmenter : BaseDefragmenter
    {
        public SimulationDefragmenter()
        {
            lib = new SimulationLib(this);
        }

        private SimulationLib lib;

        #region IDefragmenter Members

        public override void BeginDefragmentation(string parameter)
        {
            if (lib != null)
            {
                lib.RunSimulation();
            }
        }

        public override void FinishDefragmentation(Int32 timeoutMs)
        {
            if ((lib.Data != null) && (lib.Data.Running == RunningState.Running))
            {
                lib.StopSimulation(timeoutMs);
            }
        }

        public override DiskMap diskMap
        {
            set {} 
            get { return lib.diskMap; }
        }

        #endregion
    }
}
